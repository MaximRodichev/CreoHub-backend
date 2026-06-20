using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.AdminCommands;

public record ApproveContentReplacementCommand(Guid ReplacementId, Guid AdminId)
    : IRequest<BaseResponse<bool>>;

public record RejectContentReplacementCommand(Guid ReplacementId, Guid AdminId, string? Reason = null)
    : IRequest<BaseResponse<bool>>;

// ── Approve ───────────────────────────────────────────────────────────────────

public class ApproveContentReplacementHandler
    : IRequestHandler<ApproveContentReplacementCommand, BaseResponse<bool>>
{
    private readonly IContentFileReplacementRepository _replacements;
    private readonly IContentFileRepository            _contentFiles;
    private readonly IStorageObjectRepository          _storageObjects;
    private readonly IStorageService                   _storage;
    private readonly IUnitOfWork                       _unitOfWork;

    public ApproveContentReplacementHandler(
        IContentFileReplacementRepository replacements,
        IContentFileRepository contentFiles,
        IStorageObjectRepository storageObjects,
        IStorageService storage,
        IUnitOfWork unitOfWork)
    {
        _replacements   = replacements;
        _contentFiles   = contentFiles;
        _storageObjects = storageObjects;
        _storage        = storage;
        _unitOfWork     = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(ApproveContentReplacementCommand request, CancellationToken ct)
    {
        try
        {
            var repl = await _replacements.GetByIdAsync(request.ReplacementId, ct);
            if (repl is null)                              return BaseResponse<bool>.Fail("Заявка не найдена.");
            if (repl.Status != ReplacementStatus.Pending)  return BaseResponse<bool>.Fail("Заявка уже обработана.");

            var contentFile = await _contentFiles.GetByIdWithStorageAsync(repl.ContentFileId);
            if (contentFile?.StorageObject is null) return BaseResponse<bool>.Fail("Контент-файл или его storage не найдены.");
            var oldSo = contentFile.StorageObject;

            var newSo = await _storageObjects.GetByIdAsync(repl.NewStorageObjectId);
            if (newSo is null) return BaseResponse<bool>.Fail("Новый файл не найден.");

            var oldKey = oldSo.Key;

            // Подменяем байты на ТОМ ЖЕ StorageObject → ContentFile/ContentAccess не трогаются,
            // покупатели получают исправленный файл по тем же доступам.
            oldSo.ReplaceFile(newSo.Key, newSo.FileName, newSo.FileSize, newSo.MimeType);
            _storageObjects.Update(oldSo);

            // Staging-строку нового файла удаляем (её R2-объект теперь принадлежит oldSo).
            _storageObjects.Remove(newSo);

            repl.Approve(request.AdminId);

            await _unitOfWork.SaveChangesAsync(ct);

            // Старый R2-объект больше не нужен — удаляем после успешного commit.
            if (!string.Equals(oldKey, newSo.Key, StringComparison.Ordinal))
                _ = _storage.DeleteFileAsync(oldKey);

            // NB: повторную FFmpeg-оптимизацию НЕ запускаем автоматически (риск нагрузки — см. инцидент).
            // При необходимости владелец запустит вручную через /s3/optimize.

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}

// ── Reject ────────────────────────────────────────────────────────────────────

public class RejectContentReplacementHandler
    : IRequestHandler<RejectContentReplacementCommand, BaseResponse<bool>>
{
    private readonly IContentFileReplacementRepository _replacements;
    private readonly IStorageObjectRepository          _storageObjects;
    private readonly IStorageService                   _storage;
    private readonly IAccountRepository                _accounts;
    private readonly INotificationService              _notifications;
    private readonly IUnitOfWork                       _unitOfWork;

    public RejectContentReplacementHandler(
        IContentFileReplacementRepository replacements,
        IStorageObjectRepository storageObjects,
        IStorageService storage,
        IAccountRepository accounts,
        INotificationService notifications,
        IUnitOfWork unitOfWork)
    {
        _replacements   = replacements;
        _storageObjects = storageObjects;
        _storage        = storage;
        _accounts       = accounts;
        _notifications  = notifications;
        _unitOfWork     = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(RejectContentReplacementCommand request, CancellationToken ct)
    {
        try
        {
            var repl = await _replacements.GetByIdAsync(request.ReplacementId, ct);
            if (repl is null)                              return BaseResponse<bool>.Fail("Заявка не найдена.");
            if (repl.Status != ReplacementStatus.Pending)  return BaseResponse<bool>.Fail("Заявка уже обработана.");

            repl.Reject(request.AdminId, request.Reason);

            // Отклонённый staging-файл удаляем (он никогда не использовался).
            var newSo  = await _storageObjects.GetByIdAsync(repl.NewStorageObjectId);
            var newKey = newSo?.Key;
            if (newSo is not null) _storageObjects.Remove(newSo);

            await _unitOfWork.SaveChangesAsync(ct);

            if (newKey is not null) _ = _storage.DeleteFileAsync(newKey);

            // In-app уведомление владельцу (fire-and-forget, не влияет на основной поток).
            try
            {
                var owner = await _accounts.GetUserByShopIdAsync(repl.ShopId, ct);
                if (owner is not null)
                {
                    var msg = string.IsNullOrWhiteSpace(request.Reason)
                        ? "Замена файла отклонена модератором."
                        : $"Замена файла отклонена модератором. Причина: {request.Reason}";
                    await _notifications.NotifyAsync(owner.Id, NotificationType.Moderation,
                        msg, actionUrl: null, null, null, CancellationToken.None);
                }
            }
            catch { /* notifications must never affect the main flow */ }

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
