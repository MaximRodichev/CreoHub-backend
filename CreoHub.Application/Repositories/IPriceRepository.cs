using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IPriceRepository : IRepository<Price, int>
{
    public Task<Price> GetPriceByProductId(int id);
}