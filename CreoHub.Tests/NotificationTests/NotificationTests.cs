using CreoHub.Application.Commands.AccountCommands;
using CreoHub.Application.Commands.AdminCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.Queries.Admin;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Infrastructure.Persistence.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.NotificationTests;

/// <summary>
/// Tests for Block 24 — Notification system.
/// Covers: BroadcastJob domain, CreateBroadcastJobHandler, GetBroadcastJobsHandler,
/// UpdateNotificationSettingsHandler, TelegramNotificationService (unconfigured),
/// SmtpNotificationService (unconfigured), and handler-level notification triggers.
/// </summary>
public class NotificationTests
{
    private static readonly Guid AdminId = Guid.Parse("aaaa0000-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId  = Guid.Parse("bbbb0000-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly ITestOutputHelper _output;

    public NotificationTests(ITestOutputHelper output) => _output = output;

    // ═══════════════════════════════════════════════════════════════════════════
    // 1. BroadcastJob — Domain entity state machine
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BroadcastJob_Create_DefaultStatusIsPending()
    {
        var job = BroadcastJob.Create("Hello everyone!", AdminId);

        Assert.Equal(BroadcastStatus.Pending, job.Status);
        Assert.Equal(AdminId, job.CreatedBy);
        Assert.Equal("Hello everyone!", job.Message);
        Assert.Equal(0, job.TotalUsers);
        Assert.Equal(0, job.SentCount);
        Assert.Equal(0, job.FailedCount);
        Assert.Null(job.StartedAt);
        Assert.Null(job.CompletedAt);
        Assert.Null(job.ErrorLog);
    }

    [Fact]
    public void BroadcastJob_Create_EmptyMessage_Throws()
    {
        Assert.Throws<ArgumentException>(() => BroadcastJob.Create("", AdminId));
        Assert.Throws<ArgumentException>(() => BroadcastJob.Create("   ", AdminId));
    }

    [Fact]
    public void BroadcastJob_Start_SetsRunningAndTotalUsers()
    {
        var job = BroadcastJob.Create("Test", AdminId);
        job.Start(150);

        Assert.Equal(BroadcastStatus.Running, job.Status);
        Assert.Equal(150, job.TotalUsers);
        Assert.NotNull(job.StartedAt);
    }

    [Fact]
    public void BroadcastJob_RecordSentAndFailed_IncrementCounters()
    {
        var job = BroadcastJob.Create("Test", AdminId);
        job.Start(10);

        job.RecordSent();
        job.RecordSent();
        job.RecordSent();
        job.RecordFailed();

        Assert.Equal(3, job.SentCount);
        Assert.Equal(1, job.FailedCount);
    }

    [Fact]
    public void BroadcastJob_Complete_SetsDoneAndCompletedAt()
    {
        var job = BroadcastJob.Create("Test", AdminId);
        job.Start(5);
        job.RecordSent();
        job.Complete();

        Assert.Equal(BroadcastStatus.Done, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.Null(job.ErrorLog);
    }

    [Fact]
    public void BroadcastJob_Fail_SetsFailedStatusAndErrorLog()
    {
        var job = BroadcastJob.Create("Test", AdminId);
        job.Start(5);
        job.Fail("Connection timeout");

        Assert.Equal(BroadcastStatus.Failed, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.Equal("Connection timeout", job.ErrorLog);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 2. CreateBroadcastJobHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateBroadcastJob_ValidMessage_SavesAndReturnsId()
    {
        var broadcastRepo = Substitute.For<IBroadcastJobRepository>();
        var unitOfWork    = Substitute.For<IUnitOfWork>();

        // Simulate DB assigning Id = 7
        broadcastRepo.AddAsync(Arg.Any<BroadcastJob>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var job = ci.Arg<BroadcastJob>();
                typeof(BroadcastJob).GetProperty("Id")!.SetValue(job, 7);
                return Task.FromResult(job);
            });
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new CreateBroadcastJobHandler(broadcastRepo, unitOfWork);
        var result  = await handler.Handle(
            new CreateBroadcastJobCommand("Привет всем!", AdminId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Id: {result.Data}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(7, result.Data);
        await broadcastRepo.Received(1).AddAsync(
            Arg.Is<BroadcastJob>(j => j.Message == "Привет всем!" && j.CreatedBy == AdminId),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBroadcastJob_EmptyMessage_ReturnsError()
    {
        var broadcastRepo = Substitute.For<IBroadcastJobRepository>();
        var unitOfWork    = Substitute.For<IUnitOfWork>();
        var handler = new CreateBroadcastJobHandler(broadcastRepo, unitOfWork);

        var result = await handler.Handle(
            new CreateBroadcastJobCommand("   ", AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        await broadcastRepo.DidNotReceive().AddAsync(Arg.Any<BroadcastJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBroadcastJob_RepositoryThrows_ReturnsError()
    {
        var broadcastRepo = Substitute.For<IBroadcastJobRepository>();
        var unitOfWork    = Substitute.For<IUnitOfWork>();
        broadcastRepo.AddAsync(Arg.Any<BroadcastJob>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var handler = new CreateBroadcastJobHandler(broadcastRepo, unitOfWork);
        var result  = await handler.Handle(
            new CreateBroadcastJobCommand("Test msg", AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("DB down", result.ErrorMessage);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 3. GetBroadcastJobsHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetBroadcastJobs_ReturnsAllJobsMappedToDTOs()
    {
        var broadcastRepo = Substitute.For<IBroadcastJobRepository>();

        var job1 = BroadcastJob.Create("Message 1", AdminId);
        var job2 = BroadcastJob.Create("Message 2", AdminId);
        job2.Start(50);
        job2.RecordSent();
        job2.Complete();

        broadcastRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BroadcastJob> { job1, job2 }));

        var handler = new GetBroadcastJobsHandler(broadcastRepo);
        var result  = await handler.Handle(new GetBroadcastJobsQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(2, result.Data!.Count);

        var dto1 = result.Data.First(d => d.Message == "Message 1");
        Assert.Equal(BroadcastStatus.Pending, dto1.Status);
        Assert.Equal(0, dto1.SentCount);

        var dto2 = result.Data.First(d => d.Message == "Message 2");
        Assert.Equal(BroadcastStatus.Done, dto2.Status);
        Assert.Equal(1, dto2.SentCount);
        Assert.Equal(50, dto2.TotalUsers);
    }

    [Fact]
    public async Task GetBroadcastJobs_EmptyList_ReturnsEmptyCollection()
    {
        var broadcastRepo = Substitute.For<IBroadcastJobRepository>();
        broadcastRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<BroadcastJob>()));

        var result = await new GetBroadcastJobsHandler(broadcastRepo)
            .Handle(new GetBroadcastJobsQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetBroadcastJobs_RepositoryThrows_ReturnsError()
    {
        var broadcastRepo = Substitute.For<IBroadcastJobRepository>();
        broadcastRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Storage unavailable"));

        var result = await new GetBroadcastJobsHandler(broadcastRepo)
            .Handle(new GetBroadcastJobsQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 4. UpdateNotificationSettingsHandler
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateNotificationSettings_ValidUser_UpdatesAndSaves()
    {
        var accountRepo = Substitute.For<IAccountRepository>();
        var unitOfWork  = Substitute.For<IUnitOfWork>();
        var user = User.Create("TestUser", "test@example.com");
        accountRepo.GetByIdWithSettingsAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new UpdateNotificationSettingsHandler(accountRepo, unitOfWork, Substitute.For<INotificationService>());
        var result  = await handler.Handle(
            new UpdateNotificationSettingsCommand(UserId,
                TelegramEnabled: true, EmailEnabled: true,
                NotifyOnPurchase: false, NotifyOnModeration: true,
                NotifyOnBalance: true, NotifyOnBroadcast: true, NotifyOnNewProduct: true),
            CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.False(user.NotificationSettings!.NotifyOnPurchase);
        Assert.True(user.NotificationSettings!.NotifyOnModeration);
        accountRepo.Received(1).Update(user);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateNotificationSettings_BothTrue_UpdatesCorrectly()
    {
        var accountRepo = Substitute.For<IAccountRepository>();
        var unitOfWork  = Substitute.For<IUnitOfWork>();
        var user = User.Create("TestUser", "test@example.com");
        accountRepo.GetByIdWithSettingsAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await new UpdateNotificationSettingsHandler(accountRepo, unitOfWork, Substitute.For<INotificationService>())
            .Handle(new UpdateNotificationSettingsCommand(UserId, true, true, true, true, true, true, true), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.True(user.NotificationSettings!.NotifyOnPurchase);
        Assert.True(user.NotificationSettings!.NotifyOnModeration);
    }

    [Fact]
    public async Task UpdateNotificationSettings_BothFalse_UpdatesCorrectly()
    {
        var accountRepo = Substitute.For<IAccountRepository>();
        var unitOfWork  = Substitute.For<IUnitOfWork>();
        var user = User.Create("TestUser", "test@example.com");
        accountRepo.GetByIdWithSettingsAsync(UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await new UpdateNotificationSettingsHandler(accountRepo, unitOfWork, Substitute.For<INotificationService>())
            .Handle(new UpdateNotificationSettingsCommand(UserId, false, false, false, false, false, false, false), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.False(user.NotificationSettings!.NotifyOnPurchase);
        Assert.False(user.NotificationSettings!.NotifyOnModeration);
    }

    [Fact]
    public async Task UpdateNotificationSettings_UserNotFound_ReturnsError()
    {
        var accountRepo = Substitute.For<IAccountRepository>();
        var unitOfWork  = Substitute.For<IUnitOfWork>();
        accountRepo.GetByIdWithSettingsAsync(UserId, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await new UpdateNotificationSettingsHandler(accountRepo, unitOfWork, Substitute.For<INotificationService>())
            .Handle(new UpdateNotificationSettingsCommand(UserId, false, false, false, false, false, false, false), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 5. TelegramNotificationService — unconfigured (no token)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TelegramNotificationService_NoToken_ReturnsFalse()
    {
        var config = Substitute.For<IConfiguration>();
        config["Telegram:BotToken"].Returns((string?)null);

        var service = new TelegramNotificationService(
            new HttpClient(),
            config,
            NullLogger<TelegramNotificationService>.Instance);

        var result = await service.TrySendAsync(12345L, "hello");

        Assert.False(result);
    }

    [Fact]
    public async Task TelegramNotificationService_EmptyToken_ReturnsFalse()
    {
        var config = Substitute.For<IConfiguration>();
        config["Telegram:BotToken"].Returns(string.Empty);

        var service = new TelegramNotificationService(
            new HttpClient(),
            config,
            NullLogger<TelegramNotificationService>.Instance);

        var result = await service.TrySendAsync(99999L, "test message");

        Assert.False(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 6. SmtpNotificationService — unconfigured (no host)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SmtpNotificationService_NoHost_ReturnsFalse()
    {
        var config = Substitute.For<IConfiguration>();
        config["Smtp:Host"].Returns((string?)null);

        var service = new SmtpNotificationService(
            config,
            NullLogger<SmtpNotificationService>.Instance);

        var result = await service.TrySendAsync("user@example.com", "Subject", "Body");

        Assert.False(result);
    }

    [Fact]
    public async Task SmtpNotificationService_EmptyHost_ReturnsFalse()
    {
        var config = Substitute.For<IConfiguration>();
        config["Smtp:Host"].Returns("  ");

        var service = new SmtpNotificationService(
            config,
            NullLogger<SmtpNotificationService>.Instance);

        var result = await service.TrySendAsync("user@example.com", "Subject", "Body");

        Assert.False(result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 7. INotificationService — handler-level trigger verification
    //    (INotificationService is mocked; we verify SendAsync is called with
    //     correct args from ApproveModerationHandler / RejectModerationHandler)
    // ═══════════════════════════════════════════════════════════════════════════

    private static readonly Guid ShopId = Guid.Parse("cccc0000-cccc-cccc-cccc-cccccccccccc");
    private const int ProductId = 99;

    [Fact]
    public async Task ApproveModeration_WhenSellerHasNotifyOn_CallsSendAsync()
    {
        var productRepo  = Substitute.For<IProductRepository>();
        var statusLogRepo = Substitute.For<IProductStatusLogRepository>();
        var accountRepo  = Substitute.For<IAccountRepository>();
        var notifications = Substitute.For<INotificationService>();
        var unitOfWork   = Substitute.For<IUnitOfWork>();

        // Product in OnModerating state
        var product = new Product("Fire Pack", "desc", ShopId);
        typeof(Product).GetProperty("Id")!.SetValue(product, ProductId);
        productRepo.GetProductById(ProductId).Returns(product);

        // Seller with notifications enabled (moderation=true)
        var seller = User.Create("SellerName", "seller@example.com");
        seller.NotificationSettings!.Update(true, true, false, true, true, true, true);
        accountRepo.GetUserByShopIdAsync(ShopId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(seller));

        statusLogRepo.AddAsync(Arg.Any<ProductStatusLog>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<ProductStatusLog>()));
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        notifications.NotifyAsync(
            Arg.Any<Guid>(), Arg.Any<CreoHub.Domain.Types.NotificationType>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<long?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var handler = new ApproveModerationHandler(productRepo, statusLogRepo, accountRepo,
            notifications, Substitute.For<IServiceScopeFactory>(), unitOfWork);
        var result  = await handler.Handle(new ApproveModerationCommand(ProductId, AdminId), CancellationToken.None);

        _output.WriteLine($"ApproveModeration result: {result.Status}");
        Assert.Equal(ResponseStatus.Success, result.Status);

        // Allow the fire-and-forget task a moment to complete
        await Task.Delay(50);
        await notifications.Received(1).NotifyAsync(
            seller.Id,
            CreoHub.Domain.Types.NotificationType.Moderation,
            Arg.Is<string>(m => m.Contains("Fire Pack")),
            Arg.Any<string?>(), Arg.Any<long?>(),
            Arg.Is<string?>(e => e == "seller@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveModeration_WhenSellerHasNotifyOff_DoesNotCallSendAsync()
    {
        var productRepo   = Substitute.For<IProductRepository>();
        var statusLogRepo = Substitute.For<IProductStatusLogRepository>();
        var accountRepo   = Substitute.For<IAccountRepository>();
        var notifications = Substitute.For<INotificationService>();
        var unitOfWork    = Substitute.For<IUnitOfWork>();

        var product = new Product("Ice Pack", "desc", ShopId);
        productRepo.GetProductById(ProductId).Returns(product);

        var seller = User.Create("SellerName", "seller@example.com");
        seller.NotificationSettings!.Update(true, true, false, false, true, true, true); // moderation=OFF
        accountRepo.GetUserByShopIdAsync(ShopId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(seller));

        statusLogRepo.AddAsync(Arg.Any<ProductStatusLog>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<ProductStatusLog>()));
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ApproveModerationHandler(productRepo, statusLogRepo, accountRepo,
            notifications, Substitute.For<IServiceScopeFactory>(), unitOfWork);
        await handler.Handle(new ApproveModerationCommand(ProductId, AdminId), CancellationToken.None);

        await Task.Delay(50);
        await notifications.DidNotReceive().SendAsync(
            Arg.Any<long?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectModeration_WhenSellerHasNotifyOn_CallsSendAsyncWithReason()
    {
        var productRepo   = Substitute.For<IProductRepository>();
        var statusLogRepo = Substitute.For<IProductStatusLogRepository>();
        var accountRepo   = Substitute.For<IAccountRepository>();
        var notifications = Substitute.For<INotificationService>();
        var unitOfWork    = Substitute.For<IUnitOfWork>();

        var product = new Product("Fraud Pack", "desc", ShopId);
        productRepo.GetProductById(ProductId).Returns(product);

        var seller = User.Create("BadSeller", "bad@example.com");
        seller.NotificationSettings!.Update(true, true, true, true, true, true, true);
        accountRepo.GetUserByShopIdAsync(ShopId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(seller));

        statusLogRepo.AddAsync(Arg.Any<ProductStatusLog>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<ProductStatusLog>()));
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        notifications.NotifyAsync(
            Arg.Any<Guid>(), Arg.Any<CreoHub.Domain.Types.NotificationType>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<long?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        const string reason = "Авторские права нарушены";
        var handler = new RejectModerationHandler(productRepo, statusLogRepo, accountRepo, notifications, unitOfWork);
        var result  = await handler.Handle(
            new RejectModerationCommand(ProductId, AdminId, reason), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);

        await Task.Delay(50);
        await notifications.Received(1).NotifyAsync(
            seller.Id,
            CreoHub.Domain.Types.NotificationType.Moderation,
            Arg.Is<string>(m => m.Contains("Fraud Pack") && m.Contains(reason)),
            Arg.Any<string?>(), Arg.Any<long?>(),
            Arg.Is<string?>(e => e == "bad@example.com"),
            Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 8. UserNotificationSettings — defaults and update
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void User_DefaultNotificationSettings_AreBothTrue()
    {
        var user = User.Create("Alice", "alice@example.com");

        Assert.NotNull(user.NotificationSettings);
        Assert.True(user.NotificationSettings!.NotifyOnPurchase);
        Assert.True(user.NotificationSettings!.NotifyOnModeration);
    }

    [Fact]
    public void User_UpdateNotificationSettings_ChangesValues()
    {
        var user = User.Create("Bob", "bob@example.com");

        user.NotificationSettings!.Update(true, true, false, false, true, true, true);

        Assert.False(user.NotificationSettings!.NotifyOnPurchase);
        Assert.False(user.NotificationSettings!.NotifyOnModeration);
    }

    [Fact]
    public void User_UpdateNotificationSettings_CanBeCalledMultipleTimes()
    {
        var user = User.Create("Carol", "carol@example.com");

        user.NotificationSettings!.Update(true, true, false, false, true, true, true);
        user.NotificationSettings!.Update(true, true, true, false, true, true, true);

        Assert.True(user.NotificationSettings!.NotifyOnPurchase);
        Assert.False(user.NotificationSettings!.NotifyOnModeration);
    }
}
