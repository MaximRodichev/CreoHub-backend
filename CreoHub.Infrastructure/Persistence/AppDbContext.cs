using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreoHub.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Price> Prices { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ProductBundle> ProductBundles { get; set; }
    public DbSet<Shop> Shops { get; set; }
    public DbSet<Tag> Tags { get; set; }
    
    //update s3 logic layer
    public DbSet<StorageObject> StorageObjects  {get; set;}
    public DbSet<ContentFile> ContentFiles { get; set; }
    public DbSet<MediaProduct> MediaProducts { get; set; }
    public DbSet<ContentAccess> ContentAccesses { get; set; }
    public DbSet<UserTransaction> UserTransactions { get; set; }
    public DbSet<ShopTransaction> ShopTransactions { get; set; }
    public DbSet<UserBalance> UserBalances { get; set; }
    public DbSet<ShopBalance> ShopBalances { get; set; }
    
    // Если есть конфигурации сущностей через Fluent API
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}