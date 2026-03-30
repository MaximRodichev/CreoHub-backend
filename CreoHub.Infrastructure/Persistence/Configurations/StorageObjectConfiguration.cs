using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class StorageObjectConfiguration : IEntityTypeConfiguration<StorageObject>
{
    public void Configure(EntityTypeBuilder<StorageObject> builder)
    {
        builder.HasKey(x=> x.Id);
        
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Key).IsRequired();
        
        builder.Property(x => x.FileSize)
            .IsRequired();
        
        builder.Property(x=>x.MimeType).IsRequired().HasMaxLength(50);
        
        builder.Property(x=>x.FileType).IsRequired();
        builder.Property(x => x.FileType)
            .HasConversion<string>();
            
        builder.Property(x => x.UploadedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );
        
        builder.HasOne(x=>x.MediaProduct)
            .WithOne(x=>x.StorageObject)
            .HasForeignKey<MediaProduct>(x=>x.StorageObjectId);
        
        builder.HasOne(x=>x.Owner).WithMany(x=>x.Files).HasForeignKey(f=>f.OwnerId).OnDelete(DeleteBehavior.Restrict);
    }
}