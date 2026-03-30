using CreoHub.Application.Repositories;

namespace CreoHub.Application.DTO.StorageDTOs;

public class DetachContentFileDTO
{
    public int ProductId { get; set; }
    public Guid ContentFileId { get; set; }
}