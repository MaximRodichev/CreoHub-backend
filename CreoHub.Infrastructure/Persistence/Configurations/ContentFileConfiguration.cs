using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class ContentFileConfiguration : IEntityTypeConfiguration<ContentFile>
{
    public void Configure(EntityTypeBuilder<ContentFile> builder)
    {
        builder.HasKey(c => c.Id);
        
        builder.ToTable(t => t.HasCheckConstraint("CK_ContentFile_PriceWeight", "\"PriceWeight\" >= 1 AND \"PriceWeight\" <= 10"));
        builder.Property(c => c.PriceWeight)
            .IsRequired();

        builder.Property(c => c.PreviewName)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(c => c.StorageObject)
            .WithMany()
            .HasForeignKey(c => c.StorageObjectId)
            .OnDelete(DeleteBehavior.Cascade); 

        builder.HasOne(c => c.Product)
            .WithMany(p => p.ContentFiles)
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.ProductId);
        builder.HasIndex(c => c.StorageObjectId);
    }
}