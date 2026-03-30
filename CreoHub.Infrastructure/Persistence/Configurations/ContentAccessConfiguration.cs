using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class ContentAccessConfiguration : IEntityTypeConfiguration<ContentAccess>
{
    public void Configure(EntityTypeBuilder<ContentAccess> builder)
    {
        builder.HasKey(ca => ca.Id);
        
        builder.HasOne(ca => ca.User)
            .WithMany(p => p.ContentAccesses) 
            .HasForeignKey(ca => ca.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(ca => ca.ContentFile)
            .WithMany()
            .HasForeignKey(ca => ca.ContentFileId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(ca => ca.Order)
            .WithMany()
            .HasForeignKey(ca => ca.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(ca => ca.GrantedAt)
            .IsRequired();

        builder.HasIndex(ca => new { ca.UserId, ca.ContentFileId })
            .IsUnique();
        
        builder.HasIndex(ca => ca.OrderId);
    }
}