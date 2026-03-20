using CreoHub.Application.DTO.StorageDTOs;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Interfaces;
using MediatR;

namespace CreoHub.Application.Repositories;

public interface IStorageObjectRepository : IRepository<StorageObject, Guid>
{   
    public Task<List<StorageObjectResponseDTO>> GetAllByShopId(Guid shopId);
}