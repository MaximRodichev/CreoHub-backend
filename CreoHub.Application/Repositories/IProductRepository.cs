using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.DTO.StatsDTOs;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IProductRepository : IRepository<Product, int>
{
    public Task<(List<ProductViewDTO>, int)> GetProductsByFilters(FiltersDto filters);
    public Task<ProductInfoDTO> GetProductInfoById(int id);
    public Task<Product> GetProductById(int id);

    public Task<Guid> GetShopIdByProductId(int id);
    public Task<ProductInfoDTO> GetProductByName(string name);
    public Task<ProductAnalyticsDTO> GetProductAnalyticsById(int id);
    public Task<List<ProductViewExtendedDTO>> GetProductsExtendedInfo(Guid shopId);
    public Task<List<ProductShortInfoDTO>> GetProductsNamesByShopId(Guid shopId);
    public Task<List<Product>> GetProductsByIds(List <int> ids);
    public Task<int> GetProductsCount();
    public Task<List<ProductStatsDTO>> GetProductsStatsByShopIdAsync(Guid shopId, DateTime? from = null, DateTime? to = null, int? limit = null);
}