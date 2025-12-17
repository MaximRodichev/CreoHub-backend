using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Queries.Orders;

public record GetOrdersQuery() : IRequest<BaseResponse<List<Order>>>
{
    
}

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, BaseResponse<List<Order>>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
    
    public async Task<BaseResponse<List<Order>>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _orderRepository.GetAllAsync();
            return BaseResponse<List<Order>>.Success(orders);
        }
        catch(Exception ex)
        {
            return BaseResponse<List<Order>>.Fail(ex.Message);
        }
    }
}