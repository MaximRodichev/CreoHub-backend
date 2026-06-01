using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class OrderItemFileConfiguration : IEntityTypeConfiguration<OrderItemFile>
{
    public void Configure(EntityTypeBuilder<OrderItemFile> builder)
    {
        // Составной ключ: OrderItemId (shadow) + ContentFileId
        builder.HasKey("OrderItemId", nameof(OrderItemFile.ContentFileId));

        builder.HasOne(x => x.ContentFile)
            .WithMany()
            .HasForeignKey(nameof(OrderItemFile.ContentFileId))
            .OnDelete(DeleteBehavior.Restrict);
    }
}
