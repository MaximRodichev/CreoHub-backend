using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Queries.Orders;

public record GetOrdersShortInfoByShopIdQuery(Guid shopId, DateTime? From = null, DateTime? To = null) : IRequest<BaseResponse<List<OrderShortInfoDTO>>>;

public class GetOrdersShortInfoByShopIdHandler : IRequestHandler<GetOrdersShortInfoByShopIdQuery, BaseResponse<List<OrderShortInfoDTO>>>
{
    private readonly IOrderRepository  _orderRepository;

    public GetOrdersShortInfoByShopIdHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
    
    public async Task<BaseResponse<List<OrderShortInfoDTO>>> Handle(GetOrdersShortInfoByShopIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _orderRepository.GetOrdersShortInfoByShopIdAsync(request.shopId, request.From, request.To);
            
            
            return BaseResponse<List<OrderShortInfoDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<OrderShortInfoDTO>>.Fail(ex.Message);
        }
    }
}