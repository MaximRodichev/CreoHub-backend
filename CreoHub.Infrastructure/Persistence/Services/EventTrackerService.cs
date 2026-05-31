using System.Text.Json;
using System.Threading.Channels;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;

namespace CreoHub.Infrastructure.Persistence.Services;

/// <summary>
/// Singleton implementation of IEventTracker backed by an unbounded Channel.
/// Track() is synchronous and never blocks — it just TryWrites to the channel.
/// The EventTrackerBackgroundService drains the channel in batches.
/// </summary>
public class EventTrackerService : IEventTracker
{
    private readonly Channel<UserEvent> _channel;

    public EventTrackerService(Channel<UserEvent> channel) => _channel = channel;

    public void Track(
        string  eventType,
        int?    productId = null,
        Guid?   userId    = null,
        string? sessionId = null,
        object? payload   = null)
    {
        try
        {
            string? payloadJson = payload is not null
                ? JsonSerializer.Serialize(payload)
                : null;

            var ev = UserEvent.Create(eventType, productId, userId, sessionId, payloadJson);
            _channel.Writer.TryWrite(ev);   // fire-and-forget, never throws
        }
        catch
        {
            // Swallow all errors — analytics must never affect the main request path
        }
    }
}
