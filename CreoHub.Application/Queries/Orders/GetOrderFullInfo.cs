using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Exceptions;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Queries.Orders;

public record GetOrderFullInfoQuery(Guid userId, Guid orderId) : IRequest<BaseResponse<OrderFullInfoDTO>>
{
    
}

public class GetOrderFullInfoQueryHandler : IRequestHandler<GetOrderFullInfoQuery, BaseResponse<OrderFullInfoDTO>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IAccountRepository _accountRepository;

    public GetOrderFullInfoQueryHandler(IOrderRepository orderRepository,  IAccountRepository accountRepository)
    {
        _orderRepository = orderRepository;
        _accountRepository = accountRepository;
    }
    
    public async Task<BaseResponse<OrderFullInfoDTO>> Handle(GetOrderFullInfoQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orderRepository.GetOrderInfoById(request.orderId);
            if (order.CustomerId != request.userId)
            {
                throw new AccessOrderException(request.orderId);
            }
            return BaseResponse<OrderFullInfoDTO>.Success(order);
        }
        catch(Exception ex)
        {
            return BaseResponse<OrderFullInfoDTO>.Fail(ex.Message);
        }
    }
}