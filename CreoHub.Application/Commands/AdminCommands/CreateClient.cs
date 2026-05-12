using CreoHub.Application.DTO;
using CreoHub.Application.DTO.AdminDTOs;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.AdminCommands;

public record CreateClientCommand(string Name, string? Email) : IRequest<BaseResponse<Guid>>;

public class CreateClientHandler : IRequestHandler<CreateClientCommand, BaseResponse<Guid>>
{
    private readonly IAccountRepository _accounts;
    private readonly IUnitOfWork        _unitOfWork;

    public CreateClientHandler(IAccountRepository accounts, IUnitOfWork unitOfWork)
    {
        _accounts   = accounts;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<Guid>> Handle(CreateClientCommand request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BaseResponse<Guid>.Fail("Имя клиента обязательно.");

            // User.Create() внутри создаёт Cart и UserBalance
            var user = User.Create(
                name:     request.Name.Trim(),
                email:    request.Email?.Trim()
            );

            await _accounts.AddAsync(user);
            await _unitOfWork.SaveChangesAsync(ct);

            return BaseResponse<Guid>.Success(user.Id);
        }
        catch (Exception ex)
        {
            return BaseResponse<Guid>.Fail(ex.Message);
        }
    }
}
