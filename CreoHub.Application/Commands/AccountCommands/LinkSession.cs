using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Commands.AccountCommands;

/// <summary>
/// Стичинг гость→юзер: при логине привязывает анонимную историю событий (по SessionId) к аккаунту.
/// Чтобы видеть весь путь «гость → зарегался → купил», а не два обрубка.
/// </summary>
public record LinkSessionCommand(Guid UserId, string SessionId) : IRequest<BaseResponse<bool>>;

public class LinkSessionHandler : IRequestHandler<LinkSessionCommand, BaseResponse<bool>>
{
    private readonly IUserEventRepository _events;

    public LinkSessionHandler(IUserEventRepository events) => _events = events;

    public async Task<BaseResponse<bool>> Handle(LinkSessionCommand request, CancellationToken ct)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(request.SessionId))
                await _events.AttachSessionToUserAsync(request.UserId, request.SessionId, ct);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
