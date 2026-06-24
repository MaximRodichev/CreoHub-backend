using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AdminDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Commands.AdminCommands;

/// <summary>
/// Гарды объединения аккаунтов. Возвращает список блокеров (пусто = можно мерджить).
/// keep — остаётся, merge — удаляется.
/// </summary>
public static class AccountMergeGuards
{
    public static List<string> Evaluate(MergeUserSummaryDto keep, MergeUserSummaryDto merge)
    {
        var b = new List<string>();
        if (keep.Id == merge.Id)
            b.Add("Нельзя объединить аккаунт сам с собой.");
        if (keep.HasShop)
            b.Add("Остающийся аккаунт владеет магазином — мердж запрещён (осиротит Shop).");
        if (merge.HasShop)
            b.Add("Удаляемый аккаунт владеет магазином — мердж запрещён (осиротит Shop).");
        if (string.Equals(keep.Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(merge.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            b.Add("Админ-аккаунты объединять нельзя.");
        if (keep.TelegramId is not null && merge.TelegramId is not null)
            b.Add("У обоих аккаунтов привязан Telegram — конфликт (потеряли бы один).");
        if (!string.IsNullOrWhiteSpace(keep.Email) && !string.IsNullOrWhiteSpace(merge.Email))
            b.Add("У обоих аккаунтов привязан e-mail — конфликт (потеряли бы один).");
        return b;
    }
}

public record MergeAccountsCommand(Guid KeepId, Guid MergeId, Guid AdminId)
    : IRequest<BaseResponse<bool>>;

public class MergeAccountsHandler : IRequestHandler<MergeAccountsCommand, BaseResponse<bool>>
{
    private readonly IAdminRepository _admin;

    public MergeAccountsHandler(IAdminRepository admin) => _admin = admin;

    public async Task<BaseResponse<bool>> Handle(MergeAccountsCommand request, CancellationToken ct)
    {
        try
        {
            var keep  = await _admin.GetMergeUserAsync(request.KeepId, ct);
            var merge = await _admin.GetMergeUserAsync(request.MergeId, ct);
            if (keep is null)  return BaseResponse<bool>.Fail("Остающийся аккаунт не найден.");
            if (merge is null) return BaseResponse<bool>.Fail("Удаляемый аккаунт не найден.");

            var blockers = AccountMergeGuards.Evaluate(keep, merge);
            if (blockers.Count > 0)
                return BaseResponse<bool>.Fail("Мердж невозможен: " + string.Join(" ", blockers));

            await _admin.MergeAccountsAsync(request.KeepId, request.MergeId, request.AdminId, ct);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
