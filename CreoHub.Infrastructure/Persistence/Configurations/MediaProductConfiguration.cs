using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class MediaProductConfiguration : IEntityTypeConfiguration<MediaProduct>
{
    public void Configure(EntityTypeBuilder<MediaProduct> builder)
    {
        builder.HasKey(x => new { x.ProductId, x.StorageObjectId });
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.StorageObjectId).IsRequired();
        builder.Property(x=>x.SortOrder).IsRequired();

        builder.HasOne(x => x.Thumbnail)
            .WithMany()
            .HasForeignKey(x => x.ThumbnailId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Product).WithMany(x=>x.MediaProducts).HasForeignKey(x => x.ProductId);
        // OnDelete.Restrict: запрещает удаление StorageObject пока к нему привязан MediaProduct.
        // Сама связь (One-to-One) полностью настроена в StorageObjectConfiguration.
        builder.HasOne(x => x.StorageObject)
            .WithOne(x => x.MediaProduct)
            .HasForeignKey<MediaProduct>(x => x.StorageObjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}