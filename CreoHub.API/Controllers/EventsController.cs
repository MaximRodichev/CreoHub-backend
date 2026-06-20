using System.Security.Claims;
using CreoHub.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreoHub.API.Controllers;

public record TrackPageViewDto(string Path);

/// <summary>
/// Клиентский beacon аналитики. Фронт шлёт сюда просмотры страниц (page_view).
/// Anonymous + fire-and-forget: трекер пишет в bounded-канал и никогда не блокирует/бросает.
/// </summary>
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventTracker _tracker;

    public EventsController(IEventTracker tracker) => _tracker = tracker;

    [AllowAnonymous]
    [HttpPost("page-view")]
    public IActionResult TrackPageView([FromBody] TrackPageViewDto dto)
    {
        var path = dto?.Path;
        if (string.IsNullOrWhiteSpace(path) || path.Length > 512 || !path.StartsWith('/'))
            return NoContent();   // молча игнорируем мусор — это аналитика, не контракт

        // Нормализуем: только path, без query/fragment, c ограничением длины.
        var clean = path.Split('?', '#')[0];
        if (clean.Length > 128) clean = clean[..128];

        Guid? uid = null;
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(sub, out var g) && g != Guid.Empty) uid = g;

        var sid = Request.Headers.TryGetValue("X-Session-Id", out var sv) ? sv.ToString() : null;

        _tracker.Track(EventTypes.PageView, userId: uid, sessionId: sid, payload: new { path = clean });
        return NoContent();
    }
}
