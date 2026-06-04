using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class ShopFollowConfiguration : IEntityTypeConfiguration<ShopFollow>
{
    public void Configure(EntityTypeBuilder<ShopFollow> builder)
    {
        // Композитный ключ = уникальность подписки (нельзя подписаться дважды)
        builder.HasKey(x => new { x.UserId, x.ShopId });

        builder.Property(x => x.CreatedAt)
            .HasConversion(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // FK без навигаций, каскад при удалении магазина/пользователя
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Shop>()
            .WithMany()
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        // Быстрый список подписчиков магазина (рассылка + счётчик)
        builder.HasIndex(x => x.ShopId);

        builder.ToTable("ShopFollows");
    }
}
