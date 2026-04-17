using CreoHub.Application.DTO;
using CreoHub.Application.DTO.BalanceDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Balance;

public record GetBalanceQuery(Guid UserId) : IRequest<BaseResponse<BalanceDTO>>;

public class GetBalanceHandler : IRequestHandler<GetBalanceQuery, BaseResponse<BalanceDTO>>
{
    private readonly IUserBalanceRepository _balanceRepository;

    public GetBalanceHandler(IUserBalanceRepository balanceRepository)
    {
        _balanceRepository = balanceRepository;
    }

    public async Task<BaseResponse<BalanceDTO>> Handle(
        GetBalanceQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var balance = await _balanceRepository.GetByUserIdAsync(request.UserId);

            // Баланс создаётся лениво — если ещё нет, возвращаем нули
            return BaseResponse<BalanceDTO>.Success(new BalanceDTO
            {
                AvailableAmount = balance?.AvailableAmount ?? 0,
                PendingAmount   = balance?.PendingAmount   ?? 0,
            });
        }
        catch (Exception ex)
        {
            return BaseResponse<BalanceDTO>.Fail(ex.Message);
        }
    }
}
