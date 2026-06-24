using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreoHub.Infrastructure.Persistence.Configurations;

public class AccountMergeLogConfiguration : IEntityTypeConfiguration<AccountMergeLog>
{
    public void Configure(EntityTypeBuilder<AccountMergeLog> b)
    {
        b.ToTable("AccountMergeLogs");
        b.HasKey(x => x.Id);

        b.Property(x => x.AddedBalance).HasPrecision(18, 2);
        b.Property(x => x.AddedSpent).HasPrecision(18, 2);

        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.KeepUserId);
    }
}
