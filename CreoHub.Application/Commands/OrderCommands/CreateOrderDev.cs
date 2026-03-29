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
            var customer = await _accountRepository.GetByIdAsync(request.dto.ClientId)
                           ?? throw new InvalidOperationException("Customer not found.");

            var products = await _productRepository.GetProductsByIds(request.dto.ProductsIds);
    
            if (products.Count != request.dto.ProductsIds.Count)
                throw new InvalidOperationException("Some products were not found.");

            var items = products
                .Select(p => (p, p.ContentFiles.ToList()))
                .ToList();

            var order = Order.Open(
                description: "",
                items: items,
                customerId: customer.Id
            );

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