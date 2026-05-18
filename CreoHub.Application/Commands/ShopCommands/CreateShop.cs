using AutoMapper;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ShopDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using MediatR;

namespace CreoHub.Application.Commands.ShopCommands;

public record CreateShopCommand(Guid UserId, CreateShopDTO dto) : IRequest<BaseResponse<Guid>>
{
    
}

public class CreateShopHandler : IRequestHandler<CreateShopCommand, BaseResponse<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShopRepository _shopRepository;
    private readonly IAccountRepository _accountRepository;

    public CreateShopHandler(IMapper mapper, IUnitOfWork unitOfWork, IShopRepository shopRepository, IAccountRepository accountRepository)
    {
        _unitOfWork = unitOfWork;
        _shopRepository = shopRepository;
        _accountRepository = accountRepository;
    }
    
    public async Task<BaseResponse<Guid>> Handle(CreateShopCommand request, CancellationToken cancellationToken)
    {
        return BaseResponse<Guid>.Fail("Создание магазина временно недоступно");
        /* LOCKED FOR DEVELOPMENT
        try
        {
            var shop = new Shop(
                    request.dto.Name,
                    request.dto.Description,
                    request.UserId
                );
            await _shopRepository.AddAsync(shop);
            
            User? user = await _accountRepository.GetByIdAsync(request.UserId);
            if (user == null)
                throw new Exception("User not found");
            user.AssignShop(shop);

            _accountRepository.Update(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BaseResponse<Guid>.Success(shop.Id);
        }
        catch (Exception ex)
        {
            return BaseResponse<Guid>.Fail(ex.Message);
        }
        */
    }
}