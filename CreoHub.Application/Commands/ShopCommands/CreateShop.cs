using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.ShopCommands;

public record CreateShopCommand(Guid UserId, CreateShopDTO dto) : IRequest<BaseResponse<Guid>>
{
    
}

public class CreateShopHandler : IRequestHandler<CreateShopCommand, BaseResponse<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShopRepository _shopRepository;

    public CreateShopHandler(IMapper mapper, IUnitOfWork unitOfWork, IShopRepository shopRepository)
    {
        _unitOfWork = unitOfWork;
        _shopRepository = shopRepository;
    }
    
    public async Task<BaseResponse<Guid>> Handle(CreateShopCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var shop = new Shop(
                    request.dto.Name,
                    request.dto.Description,
                    request.UserId
                );
            await _shopRepository.AddAsync(shop);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<Guid>.Success(shop.Id);
        }
        catch (Exception ex)
        {
            return BaseResponse<Guid>.Fail(ex.Message);
        }
    }
}