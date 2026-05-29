using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class ProductStatusLog
{
    public int           Id          { get; private set; }
    public int           ProductId   { get; private set; }
    public ProductStatus OldStatus   { get; private set; }
    public ProductStatus NewStatus   { get; private set; }
    public string?       Reason      { get; private set; }
    public Guid?         ChangedById { get; private set; }   // null = система
    public DateTime      CreatedAt   { get; private set; }

    public Product Product { get; private set; } = null!;

    private ProductStatusLog() { }

    public ProductStatusLog(
        int productId,
        ProductStatus oldStatus,
        ProductStatus newStatus,
        string? reason = null,
        Guid? changedById = null)
    {
        ProductId   = productId;
        OldStatus   = oldStatus;
        NewStatus   = newStatus;
        Reason      = reason;
        ChangedById = changedById;
        CreatedAt   = DateTime.UtcNow;
    }
}
