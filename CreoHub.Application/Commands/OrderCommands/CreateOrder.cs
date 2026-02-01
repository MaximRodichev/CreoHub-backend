using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Application.Commands.OrderCommands;

public record CreateOrderCommand(Guid userId, CreateOrderDTO dto) : IRequest<BaseResponse<bool>>
{
    
}

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPriceRepository _priceRepository;
    private readonly IAccountRepository _accountRepository;

    public CreateOrderHandler(IUnitOfWork unitOfWork, IOrderRepository orderRepository,  IProductRepository productRepository, IAccountRepository accountRepository,  IPriceRepository priceRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _accountRepository = accountRepository;
        _priceRepository = priceRepository;
        _unitOfWork = unitOfWork;
        _orderRepository = orderRepository;
    }

    public async Task<BaseResponse<bool>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            User? customer = await _accountRepository.GetByIdAsync(request.userId);
            List<Product> products = await _productRepository.GetProductsByIds(request.dto.ProductsIds);
            decimal price = products.Sum(x=>x.Prices.Last().Value);

            Order order = Order.Open(price, String.Empty, products, customer.Id);
            order.InjectOrderDate(request.dto
                .Date); //TODO: дата не должна инжекститься, это условность чтобы восстановить истори работы

            await _orderRepository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}