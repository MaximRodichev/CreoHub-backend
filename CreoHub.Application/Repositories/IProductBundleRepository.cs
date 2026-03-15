using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IProductBundleRepository : IRepository<ProductBundle, int>
{

    public Task<bool> AddRangeAsync(List<ProductBundle> entities);

}