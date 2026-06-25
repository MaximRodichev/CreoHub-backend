using System.Text.Json;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Admin;

public record FlowItemDto(DateTime At, string EventType, int? ProductId, string? ProductName, string? Detail);

/// <summary>Карточка субъекта (только для зарегистрированного юзера; у гостя null).</summary>
public record SubjectInfoDto(
    Guid Id, string Name, string? Email, long? TelegramId, string? TelegramUsername,
    string Role, decimal LifetimeSpent, bool HasShop);

public record SubjectFlowDto(Guid? UserId, string? SessionId, SubjectInfoDto? Subject, IReadOnlyList<FlowItemDto> Items);

/// <summary>Хронология пути субъекта (юзер по UserId или гость по SessionId) для админ-таймлайна.</summary>
public record GetSubjectFlowQuery(Guid? UserId, string? SessionId, int Days = 30)
    : IRequest<BaseResponse<SubjectFlowDto>>;

public class GetSubjectFlowHandler : IRequestHandler<GetSubjectFlowQuery, BaseResponse<SubjectFlowDto>>
{
    private readonly IUserEventRepository _events;
    private readonly IAdminRepository     _admin;

    public GetSubjectFlowHandler(IUserEventRepository events, IAdminRepository admin)
    {
        _events = events;
        _admin  = admin;
    }

    public async Task<BaseResponse<SubjectFlowDto>> Handle(GetSubjectFlowQuery request, CancellationToken ct)
    {
        try
        {
            if (request.UserId is null && string.IsNullOrWhiteSpace(request.SessionId))
                return BaseResponse<SubjectFlowDto>.Fail("Нужен userId или sessionId.");

            var to   = DateTime.UtcNow;
            var from = to.AddDays(-Math.Clamp(request.Days, 1, 365));

            var raw = await _events.GetSubjectFlowAsync(request.UserId, request.SessionId, from, to, 500, ct);

            var productIds = raw.Where(e => e.ProductId.HasValue).Select(e => e.ProductId!.Value).Distinct().ToList();
            var pnames     = productIds.Count > 0
                ? await _admin.GetProductNamesAsync(productIds, ct)
                : new Dictionary<int, string>();

            static string? Detail(FlowEventRaw e)
            {
                if (string.IsNullOrEmpty(e.Payload)) return null;
                try
                {
                    using var doc = JsonDocument.Parse(e.Payload);
                    if (doc.RootElement.TryGetProperty("q", out var q))    return q.GetString();
                    if (doc.RootElement.TryGetProperty("path", out var p)) return p.GetString();
                }
                catch { /* битый payload */ }
                return null;
            }

            var items = raw.Select(e => new FlowItemDto(
                e.At, e.EventType, e.ProductId,
                e.ProductId.HasValue ? pnames.GetValueOrDefault(e.ProductId.Value) : null,
                Detail(e))).ToList();

            // Профиль субъекта — имя/TG/email/спенд для шапки (только если это зарегистрированный юзер).
            SubjectInfoDto? subject = null;
            if (request.UserId is Guid uid)
            {
                var u = await _admin.GetMergeUserAsync(uid, ct);
                if (u is not null)
                    subject = new SubjectInfoDto(
                        u.Id, u.Name, u.Email, u.TelegramId, u.TelegramUsername,
                        u.Role, u.LifetimeSpent, u.HasShop);
            }

            return BaseResponse<SubjectFlowDto>.Success(
                new SubjectFlowDto(request.UserId, request.SessionId, subject, items));
        }
        catch (Exception ex)
        {
            return BaseResponse<SubjectFlowDto>.Fail(ex.Message);
        }
    }
}
