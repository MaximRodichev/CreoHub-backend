using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;

namespace CreoHub.Application.Repositories;

public interface IMediaProductRepository : IRepository<MediaProduct, (Guid product, Guid storageObject)>
{
    
}