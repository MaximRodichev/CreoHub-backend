using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Queries.Orders;

public record GetUserOrdersQuery(Guid UserId, int pageSize, int page) : IRequest<BaseResponse<List<OrderUserInfoDTO>>>;


public class GetUserOrdersHandler : IRequestHandler<GetUserOrdersQuery, BaseResponse<List<OrderUserInfoDTO>>>
{

    private readonly IOrderRepository _orderRepository;

    public GetUserOrdersHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
    
    public async Task<BaseResponse<List<OrderUserInfoDTO>>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            List<OrderUserInfoDTO> response = await _orderRepository.GetUserOrders(request.UserId, request.page, request.pageSize);

            return BaseResponse<List<OrderUserInfoDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<OrderUserInfoDTO>>.Fail(ex.Message);
        }
    }
}