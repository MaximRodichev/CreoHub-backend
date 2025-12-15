using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;


public class UserConfiguration :  IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x=>x.EmailAddress).IsUnique();
        builder.HasIndex(x => x.TelegramId).IsUnique();
        builder.Property(x => x.Discount).IsRequired();
           
        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_User_Discount",
                "\"Discount\" >= 0 AND \"Discount\" <= 15"
            );
        });
        
        builder.Property(x => x.RegistrationDate)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );

        builder.Property(x => x.Role)
            .HasConversion<string>();

        builder.HasOne(x => x.Shop).WithOne(x => x.Owner);
        builder.HasMany(x => x.Orders).WithOne(x => x.Customer).HasForeignKey(x => x.CustomerId);

    }
}