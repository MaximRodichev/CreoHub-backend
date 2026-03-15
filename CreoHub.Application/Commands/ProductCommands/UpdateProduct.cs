using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Repositories;
using MediatR;

namespace CreoHub.Application.Commands.ProductCommands;

public record UpdateProductCommand(Guid shopId, UpdateProductInfoDTO dto) : IRequest<BaseResponse<bool>>;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, BaseResponse<bool>>
{
    IProductRepository _productRepository;
    IUnitOfWork _unitOfWork;
    ITagRepository _tagRepository;
    IPriceRepository _priceRepository;
    
    public UpdateProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<BaseResponse<bool>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _productRepository.GetProductById(request.dto.Id);
            if (response == null)
            {
                return BaseResponse<bool>.Fail($"Product with id {request.dto.Id} not found.");
            }

            if (response.Name != request.dto.Name)
            {
                response.Name = request.dto.Name;
            }

            if (response.Description != request.dto.Description)
            {
                response.Description = request.dto.Description;
            }
            
            
            return BaseResponse<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.Fail(ex.Message);
        }
    }
}