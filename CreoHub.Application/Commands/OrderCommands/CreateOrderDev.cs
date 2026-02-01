using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Application.Commands.OrderCommands;

public record CreateOrderDevCommand(CreateOrderDevDTO dto) : IRequest<BaseResponse<bool>>
{
    
}

public class CreateOrderDevHandler : IRequestHandler<CreateOrderDevCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPriceRepository _priceRepository;
    private readonly IAccountRepository _accountRepository;

    public CreateOrderDevHandler(IUnitOfWork unitOfWork, IOrderRepository orderRepository,  IProductRepository productRepository, IAccountRepository accountRepository,  IPriceRepository priceRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _accountRepository = accountRepository;
        _priceRepository = priceRepository;
        _unitOfWork = unitOfWork;
        _orderRepository = orderRepository;
    }

    public async Task<BaseResponse<bool>> Handle(CreateOrderDevCommand request, CancellationToken cancellationToken)
    {
        try
        {
            User? customer = await _accountRepository.GetByIdAsync(request.dto.ClientId);
            List<Product> products = await _productRepository.GetProductsByIds(request.dto.ProductsIds);
            
            foreach (Product product in products)
            {   
                Price _ = await _priceRepository.AddAsync(new Price()
                {
                    Date = request.dto.PurchaseDate,
                    ProductId = product.Id,
                    Value = products.Count > 1 ? product.Prices.Last().Value : request.dto.Price
                });
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            //decimal price = products.Sum(x=>x.Prices.Last().Value);
            
            Order order = Order.Open(request.dto.Price, String.Empty, products, customer.Id);
            order.InjectOrderDate(request.dto
                .PurchaseDate); //TODO: дата не должна инжекститься, это условность чтобы восстановить истори работы

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