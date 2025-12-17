using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Exceptions;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.ProductCommands;

public record ChangePriceCommand(Guid userId, ChangePriceDTO dto) : IRequest<BaseResponse<bool>>
{
    
}

public class ChangePriceHandler : IRequestHandler<ChangePriceCommand, BaseResponse<bool>>
{
    private readonly IPriceRepository _priceRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountRepository _accountRepository;

    public ChangePriceHandler(IPriceRepository priceRepository, IAccountRepository accountRepository, IUnitOfWork unitOfWork,  IProductRepository productRepository)
    {   
        _productRepository = productRepository;
        _priceRepository = priceRepository;
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(ChangePriceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Guid shopUserVerification = await _accountRepository.GetShopByUserId(request.userId);
            Guid shopProductVerification = await _productRepository.GetShopIdByProductId(request.dto.ProductId);
            if (shopUserVerification != shopProductVerification)
                throw new ProductAccessDeniedException();

            Price newPrice = new Price(request.dto.Price, request.dto.ProductId);
            await _priceRepository.AddAsync(newPrice);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}
