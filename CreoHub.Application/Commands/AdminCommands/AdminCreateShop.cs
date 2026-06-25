using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using MediatR;

namespace CreoHub.Application.Commands.AdminCommands;

/// <summary>
/// Менеджер создаёт магазин для пользователя (само-создание отключено).
/// Гарды: пользователь существует и ещё не владеет магазином; имя 3–50 / описание ≤1000; имя уникально.
/// </summary>
public record AdminCreateShopCommand(Guid OwnerUserId, string Name, string Description, Guid AdminId)
    : IRequest<BaseResponse<Guid>>;

public class AdminCreateShopHandler : IRequestHandler<AdminCreateShopCommand, BaseResponse<Guid>>
{
    private readonly IShopRepository    _shops;
    private readonly IAccountRepository _accounts;
    private readonly IUnitOfWork        _unitOfWork;

    public AdminCreateShopHandler(IShopRepository shops, IAccountRepository accounts, IUnitOfWork unitOfWork)
    {
        _shops      = shops;
        _accounts   = accounts;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<Guid>> Handle(AdminCreateShopCommand request, CancellationToken ct)
    {
        try
        {
            var name = (request.Name ?? string.Empty).Trim();
            var desc = (request.Description ?? string.Empty).Trim();

            if (name.Length is < 3 or > 50)
                return BaseResponse<Guid>.Fail("Название магазина: 3–50 символов.");
            if (desc.Length > 1000)
                return BaseResponse<Guid>.Fail("Описание: до 1000 символов.");

            var user = await _accounts.GetByIdAsync(request.OwnerUserId);
            if (user is null)
                return BaseResponse<Guid>.Fail("Пользователь не найден.");

            var existing = await _shops.GetShopIdByOwnerIdAsync(request.OwnerUserId);
            if (existing is not null)
                return BaseResponse<Guid>.Fail("У пользователя уже есть магазин.");

            var shop = new Shop(name, desc, request.OwnerUserId);
            await _shops.AddAsync(shop);
            user.AssignShop(shop);
            _accounts.Update(user);

            await _unitOfWork.SaveChangesAsync(ct);
            return BaseResponse<Guid>.Success(shop.Id);
        }
        catch (Exception ex)
        {
            // Уникальность Shop.Name на уровне БД и пр.
            var msg = (ex.InnerException?.Message ?? ex.Message);
            return BaseResponse<Guid>.Fail(
                msg.Contains("Name", StringComparison.OrdinalIgnoreCase) || msg.Contains("unique", StringComparison.OrdinalIgnoreCase)
                    ? "Магазин с таким именем уже существует."
                    : ex.Message);
        }
    }
}
