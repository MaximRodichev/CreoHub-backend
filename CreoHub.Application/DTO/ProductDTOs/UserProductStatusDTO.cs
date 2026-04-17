using CreoHub.Domain.Entities;

namespace CreoHub.Application.DTO.ProductDTOs;

public class UserProductStatusDTO
{
    public UserProductStatus Status { get; init; }
    public List<Guid> ContentFilesIds { get; init; }
}

public enum UserProductStatus
{
    NotPurchased,
    PartiallyPurchased,
    Purchased
}