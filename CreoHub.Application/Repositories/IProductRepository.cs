using CreoHub.Application.DTO;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IProductRepository : IRepository<Product, int>
{
    public Task<List<ProductViewDTO>> GetProductsByFilters(FiltersDto filters);
    public Task<ProductInfoDTO> GetProductInfoById(int id);
    public Task<Product> GetProductById(int id);

    public Task<Guid> GetShopIdByProductId(int id);
    public Task<List<ProductInfoDTO>> GetProductsInfoByShopId(Guid shopId);
    public Task<List<ProductNameDTO>> GetProductsNamesByShopId(Guid shopId);
}