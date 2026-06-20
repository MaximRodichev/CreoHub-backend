using CreoHub.Application.Commands.ProductCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Types;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CreoHub.Application.Commands.AdminCommands;

/// <summary>
/// Backfill: перегенерировать og:image карточки всех активных товаров.
/// Запускается в фоне последовательно с троттлингом (не нагружает R2/CPU).
/// Возвращает количество товаров, поставленных в обработку.
/// </summary>
public record RegenerateAllProductOgCommand() : IRequest<BaseResponse<int>>;

public class RegenerateAllProductOgHandler
    : IRequestHandler<RegenerateAllProductOgCommand, BaseResponse<int>>
{
    private readonly IProductRepository   _products;
    private readonly IServiceScopeFactory _scopeFactory;

    public RegenerateAllProductOgHandler(IProductRepository products, IServiceScopeFactory scopeFactory)
    {
        _products     = products;
        _scopeFactory = scopeFactory;
    }

    public async Task<BaseResponse<int>> Handle(RegenerateAllProductOgCommand request, CancellationToken ct)
    {
        var items = await _products.GetProductsByStatusAsync(ProductStatus.Active, ct);
        var ids   = items.Select(i => i.ProductId).Distinct().ToList();

        var factory = _scopeFactory;
        _ = Task.Run(async () =>
        {
            foreach (var id in ids)
            {
                await ProductOgGenerator.GenerateInScopeAsync(factory, id);
                await Task.Delay(250);   // троттлинг между товарами
            }
        });

        return BaseResponse<int>.Success(ids.Count);
    }
}
