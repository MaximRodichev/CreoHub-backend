using System.ComponentModel.DataAnnotations;

namespace CreoHub.Application.DTO.StorageDTOs;

public class AttachContentFileDTO
{
    [Required]
    public int ProductId { get; set; }
    [Required]
    public Guid StorageObjectId { get; set; }
    [Required]
    public string PreviewName { get; set; }
    [Range(1,10)]
    public int PriceWeight { get; set; }
}