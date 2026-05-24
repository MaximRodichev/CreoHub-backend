using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.OrderCommands;

public record CreateOrderDevCommand(CreateOrderDevDTO dto) : IRequest<BaseResponse<bool>>;

public class CreateOrderDevHandler : IRequestHandler<CreateOrderDevCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IContentFileRepository _contentFileRepository;
    private readonly IContentAccessRepository _accessRepository;

    public CreateOrderDevHandler(
        IUnitOfWork unitOfWork,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IAccountRepository accountRepository,
        IContentFileRepository contentFileRepository,
        IContentAccessRepository accessRepository)
    {
        _unitOfWork            = unitOfWork;
        _orderRepository       = orderRepository;
        _productRepository     = productRepository;
        _accountRepository     = accountRepository;
        _contentFileRepository = contentFileRepository;
        _accessRepository      = accessRepository;
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

            // ── Завершить заказ и выдать доступ к файлам ─────────────
            order.Complete();

            foreach (var item in order.Items)
            {
                var files = await _contentFileRepository.GetByProductIdAsync(item.ProductId);
                foreach (var file in files)
                    await _accessRepository.AddAsync(
                        new ContentAccess(order.CustomerId, file.Id, order.Id));
            }

            // ── LifetimeSpent ─────────────────────────────────────────
            if (request.dto.Price > 0)
            {
                customer.AddSpend(request.dto.Price);
                _accountRepository.Update(customer);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}