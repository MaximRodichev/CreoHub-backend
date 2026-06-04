using Creohub.Domain.Entities;
using CreoHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CreoHub.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderItemFile> OrderItemFiles { get; set; }
    public DbSet<Price> Prices { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ProductBundle> ProductBundles { get; set; }
    public DbSet<Shop> Shops { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<CartItemFile> CartItemFiles { get; set; }
    
    //update s3 logic layer
    public DbSet<StorageObject> StorageObjects  {get; set;}
    public DbSet<ContentFile> ContentFiles { get; set; }
    public DbSet<MediaProduct> MediaProducts { get; set; }
    public DbSet<ContentAccess> ContentAccesses { get; set; }
    public DbSet<UserTransaction> UserTransactions { get; set; }
    public DbSet<ShopTransaction> ShopTransactions { get; set; }
    public DbSet<UserBalance> UserBalances { get; set; }
    public DbSet<ShopBalance> ShopBalances { get; set; }
    
    // subscription flow
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<PanelSession> PanelSessions { get; set; }
    public DbSet<SubscriptionPromoCode> SubscriptionPromoCodes { get; set; }

    // product status log
    public DbSet<ProductStatusLog> ProductStatusLogs { get; set; }

    // product edit history (audit snapshots)
    public DbSet<ProductEditHistory> ProductEditHistories { get; set; }

    // behavioural analytics events
    public DbSet<UserEvent> UserEvents { get; set; }

    // admin broadcast jobs
    public DbSet<BroadcastJob> BroadcastJobs { get; set; }

    // notification system (Block 29)
    public DbSet<UserNotificationSettings> UserNotificationSettings { get; set; }
    public DbSet<InAppNotification>        InAppNotifications        { get; set; }

    // pending uploads (security audit)
    public DbSet<PendingUpload> PendingUploads { get; set; }

    // shop requests («Оставить предложение»)
    public DbSet<ShopRequest> ShopRequests { get; set; }

    // shop follows (подписка на магазин)
    public DbSet<ShopFollow> ShopFollows { get; set; }
    
    // Если есть конфигурации сущностей через Fluent API
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    //     base.OnConfiguring(optionsBuilder);
    // }
}