using System.ComponentModel.DataAnnotations;

namespace CreoHub.Domain.Entities;

public class ContentFile
{
    public Guid Id { get; set; }
    [Range(1,10)]
    public int PriceWeight { get; set; }
    public string PreviewName { get; set; }
    public StorageObject StorageObject { get; set; }
    public Guid StorageObjectId { get; set; }
    public Product Product { get; set; }
    public int ProductId { get; set; }
}