using CreoHub.Application.Commands.AdminCommands;
using CreoHub.Application.Commands.OrderCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.DTO.PaymentDTOs;
using CreoHub.Application.Pricing;
using CreoHub.Application.Repositories;
using CreoHub.Application.Services;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.ModerationTests;

/// <summary>
/// Тесты для полного moderation flow:
///   1. Domain — Product.Ban / Unban / Approve / Reject / AdminHide / AdminSendToModeration
///   2. Handlers — Ban, Unban, Approve, Reject, AdminHide, AdminSendToModeration
///   3. Checkout — статус товара проверяется перед оплатой
///   4. Admin manual order — кастомная цена применяется через priceOverrides
/// </summary>
public class ModerationFlowTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Общие данные
    // ═══════════════════════════════════════════════════════════════════════════

    private static readonly Guid AdminId = Guid.Parse("aaaa0000-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ShopId  = Guid.Parse("bbbb0000-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserId  = Guid.Parse("cccc0000-cccc-cccc-cccc-cccccccccccc");
    private const int ProductId = 42;

    private readonly ITestOutputHelper _output;

    public ModerationFlowTests(ITestOutputHelper output) => _output = output;

    // ── Хелперы ──────────────────────────────────────────────────────────────

    /// <summary>Присваивает ID товару (имитация DB-присвоения).</summary>
    private static void SetId(Product product, int id) =>
        typeof(Product).GetProperty("Id")!.SetValue(product, id);

    /// <summary>
    /// Создаёт Product в заданном статусе через доменные методы (без рефлексии).
    /// Дефолтный статус новых продуктов — OnModerating.
    /// </summary>
    private static Product MakeProduct(ProductStatus status = ProductStatus.OnModerating, decimal price = 18m)
    {
        var product = new Product("Fire Pack Vol.3", "Test description", ShopId);
        // дефолт: OnModerating
        switch (status)
        {
            case ProductStatus.Active:
                product.ApproveModeration();
                break;
            case ProductStatus.Hidden:
                product.ApproveModeration();
                product.Hide();
                break;
            case ProductStatus.ModerationFailed:
                product.RejectModeration();
                break;
            case ProductStatus.Banned:
                product.ApproveModeration();
                product.Ban("Нарушение правил");
                break;
            case ProductStatus.Archived:
                product.Archive();
                break;
            case ProductStatus.OnModerating:
            default:
                break;
        }
        if (price > 0)
            product.AddPrice(price);
        return product;
    }

    private static (
        IProductRepository         productRepo,
        IProductStatusLogRepository statusLogRepo,
        IUnitOfWork                 unitOfWork)
        MakeModerationDeps()
    {
        return (
            Substitute.For<IProductRepository>(),
            Substitute.For<IProductStatusLogRepository>(),
            Substitute.For<IUnitOfWork>()
        );
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 1. DOMAIN — Product entity
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Дефолтный статус ─────────────────────────────────────────────────────

    [Fact]
    public void Product_NewProduct_DefaultStatusIsOnModerating()
    {
        var product = new Product("Brand New", "Desc", ShopId);
        Assert.Equal(ProductStatus.OnModerating, product.ProductStatus);
    }

    // ── Approve ──────────────────────────────────────────────────────────────

    [Fact]
    public void Product_ApproveModeration_FromOnModerating_SetsActive()
    {
        var product = MakeProduct(ProductStatus.OnModerating);

        product.ApproveModeration();

        Assert.Equal(ProductStatus.Active, product.ProductStatus);
    }

    [Fact]
    public void Product_ApproveModeration_NotOnModerating_Throws()
    {
        var product = MakeProduct(ProductStatus.Active);
        Assert.Throws<InvalidOperationException>(() => product.ApproveModeration());
    }

    [Theory]
    [InlineData(ProductStatus.Hidden)]
    [InlineData(ProductStatus.ModerationFailed)]
    [InlineData(ProductStatus.Banned)]
    [InlineData(ProductStatus.Archived)]
    public void Product_ApproveModeration_WrongStatus_Throws(ProductStatus status)
    {
        var product = MakeProduct(status);
        var ex = Assert.Throws<InvalidOperationException>(() => product.ApproveModeration());
        _output.WriteLine($"[ApproveModeration from {status}] → {ex.Message}");
    }

    // ── Reject ───────────────────────────────────────────────────────────────

    [Fact]
    public void Product_RejectModeration_FromOnModerating_SetsModerationFailed()
    {
        var product = MakeProduct(ProductStatus.OnModerating);

        product.RejectModeration();

        Assert.Equal(ProductStatus.ModerationFailed, product.ProductStatus);
    }

    [Fact]
    public void Product_RejectModeration_NotOnModerating_Throws()
    {
        var product = MakeProduct(ProductStatus.Active);
        Assert.Throws<InvalidOperationException>(() => product.RejectModeration());
    }

    // ── SendToModeration (owner) ─────────────────────────────────────────────

    [Theory]
    [InlineData(ProductStatus.Hidden)]
    [InlineData(ProductStatus.ModerationFailed)]
    public void Product_SendToModeration_AllowedStatuses_SetsOnModerating(ProductStatus from)
    {
        var product = MakeProduct(from);

        product.SendToModeration();

        Assert.Equal(ProductStatus.OnModerating, product.ProductStatus);
    }

    [Fact]
    public void Product_SendToModeration_FromBanned_Throws()
    {
        var product = MakeProduct(ProductStatus.Banned);

        var ex = Assert.Throws<InvalidOperationException>(() => product.SendToModeration());
        _output.WriteLine($"SendToModeration(Banned) → {ex.Message}");
    }

    [Fact]
    public void Product_SendToModeration_FromArchived_Throws()
    {
        var product = MakeProduct(ProductStatus.Archived);

        Assert.Throws<InvalidOperationException>(() => product.SendToModeration());
    }

    // ── Ban / Unban ───────────────────────────────────────────────────────────

    [Fact]
    public void Product_Ban_ActiveProduct_SetsBannedAndSavesReason()
    {
        var product = MakeProduct(ProductStatus.Active);
        const string reason = "Продажа нелицензионного контента";

        product.Ban(reason);

        Assert.Equal(ProductStatus.Banned, product.ProductStatus);
        Assert.Equal(reason, product.BanReason);
    }

    [Fact]
    public void Product_Ban_EmptyReason_Throws()
    {
        var product = MakeProduct(ProductStatus.Active);
        Assert.Throws<ArgumentException>(() => product.Ban("   "));
    }

    [Fact]
    public void Product_Ban_NullReason_Throws()
    {
        var product = MakeProduct(ProductStatus.Active);
        Assert.Throws<ArgumentException>(() => product.Ban(null!));
    }

    [Fact]
    public void Product_Ban_ArchivedProduct_Throws()
    {
        var product = MakeProduct(ProductStatus.Archived);
        Assert.Throws<InvalidOperationException>(() => product.Ban("reason"));
    }

    [Fact]
    public void Product_Unban_BannedProduct_SetsHiddenAndClearsBanReason()
    {
        var product = MakeProduct(ProductStatus.Banned);
        Assert.NotNull(product.BanReason);

        product.Unban();

        Assert.Equal(ProductStatus.Hidden, product.ProductStatus);
        Assert.Null(product.BanReason);
    }

    [Fact]
    public void Product_Unban_NotBanned_Throws()
    {
        var product = MakeProduct(ProductStatus.Active);
        Assert.Throws<InvalidOperationException>(() => product.Unban());
    }

    // ── AdminHide ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ProductStatus.Active)]
    [InlineData(ProductStatus.OnModerating)]
    [InlineData(ProductStatus.ModerationFailed)]
    public void Product_AdminHide_AllowedStatuses_SetsHidden(ProductStatus from)
    {
        var product = MakeProduct(from);

        product.AdminHide();

        Assert.Equal(ProductStatus.Hidden, product.ProductStatus);
        _output.WriteLine($"AdminHide({from}) → {product.ProductStatus}");
    }

    [Fact]
    public void Product_AdminHide_BannedProduct_Throws()
    {
        var product = MakeProduct(ProductStatus.Banned);
        var ex = Assert.Throws<InvalidOperationException>(() => product.AdminHide());
        _output.WriteLine($"AdminHide(Banned) → {ex.Message}");
    }

    [Fact]
    public void Product_AdminHide_ArchivedProduct_Throws()
    {
        var product = MakeProduct(ProductStatus.Archived);
        Assert.Throws<InvalidOperationException>(() => product.AdminHide());
    }

    // ── AdminSendToModeration ─────────────────────────────────────────────────

    [Theory]
    [InlineData(ProductStatus.Active)]
    [InlineData(ProductStatus.Hidden)]
    [InlineData(ProductStatus.ModerationFailed)]
    public void Product_AdminSendToModeration_AllowedStatuses_SetsOnModerating(ProductStatus from)
    {
        var product = MakeProduct(from);

        product.AdminSendToModeration();

        Assert.Equal(ProductStatus.OnModerating, product.ProductStatus);
    }

    [Fact]
    public void Product_AdminSendToModeration_BannedProduct_Throws()
    {
        var product = MakeProduct(ProductStatus.Banned);
        Assert.Throws<InvalidOperationException>(() => product.AdminSendToModeration());
    }

    [Fact]
    public void Product_AdminSendToModeration_ArchivedProduct_Throws()
    {
        var product = MakeProduct(ProductStatus.Archived);
        Assert.Throws<InvalidOperationException>(() => product.AdminSendToModeration());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 2. HANDLERS — Ban / Unban
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BanProduct_ActiveProduct_SetsBannedAndLogsStatus()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Active);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new BanProductHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new BanProductCommand(ProductId, "Нарушение ToS", AdminId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, ProductStatus: {product.ProductStatus}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(ProductStatus.Banned, product.ProductStatus);
        Assert.Equal("Нарушение ToS", product.BanReason);
        productRepo.Received(1).Update(product);
        await statusLogRepo.Received(1).AddAsync(
            Arg.Is<ProductStatusLog>(l =>
                l.NewStatus == ProductStatus.Banned &&
                l.ChangedById == AdminId),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BanProduct_AlreadyBanned_ReturnsErrorWithoutDoubleLog()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Banned);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new BanProductHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new BanProductCommand(ProductId, "Повторная блокировка", AdminId), CancellationToken.None);

        _output.WriteLine($"Error: {result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("уже заблокирован", result.ErrorMessage);
        await statusLogRepo.DidNotReceive().AddAsync(Arg.Any<ProductStatusLog>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BanProduct_ProductNotFound_ReturnsError()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        productRepo.GetProductById(ProductId).ReturnsNull();

        var handler = new BanProductHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new BanProductCommand(ProductId, "reason", AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BanProduct_EmptyReason_ReturnsError()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Active);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new BanProductHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new BanProductCommand(ProductId, "   ", AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnbanProduct_BannedProduct_SetsHiddenAndLogs()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Banned);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new UnbanProductHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new UnbanProductCommand(ProductId, AdminId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, ProductStatus: {product.ProductStatus}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(ProductStatus.Hidden, product.ProductStatus);
        Assert.Null(product.BanReason);
        await statusLogRepo.Received(1).AddAsync(
            Arg.Is<ProductStatusLog>(l =>
                l.OldStatus == ProductStatus.Banned &&
                l.NewStatus == ProductStatus.Hidden),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnbanProduct_NotBanned_ReturnsError()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Active);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new UnbanProductHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new UnbanProductCommand(ProductId, AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 3. HANDLERS — Approve / Reject
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ApproveModeration_OnModeratingProduct_SetsActiveAndLogs()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.OnModerating);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new ApproveModerationHandler(productRepo, statusLogRepo,
            Substitute.For<IAccountRepository>(), Substitute.For<INotificationService>(),
            Substitute.For<IServiceScopeFactory>(), unitOfWork);
        var result  = await handler.Handle(
            new ApproveModerationCommand(ProductId, AdminId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, ProductStatus: {product.ProductStatus}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(ProductStatus.Active, product.ProductStatus);
        await statusLogRepo.Received(1).AddAsync(
            Arg.Is<ProductStatusLog>(l =>
                l.OldStatus == ProductStatus.OnModerating &&
                l.NewStatus == ProductStatus.Active &&
                l.ChangedById == AdminId),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(ProductStatus.Active)]
    [InlineData(ProductStatus.Hidden)]
    [InlineData(ProductStatus.Banned)]
    public async Task ApproveModeration_NotOnModeration_ReturnsError(ProductStatus wrongStatus)
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(wrongStatus);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new ApproveModerationHandler(productRepo, statusLogRepo,
            Substitute.For<IAccountRepository>(), Substitute.For<INotificationService>(),
            Substitute.For<IServiceScopeFactory>(), unitOfWork);
        var result  = await handler.Handle(
            new ApproveModerationCommand(ProductId, AdminId), CancellationToken.None);

        _output.WriteLine($"ApproveModeration({wrongStatus}) → {result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveModeration_ProductNotFound_ReturnsError()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        productRepo.GetProductById(ProductId).ReturnsNull();

        var handler = new ApproveModerationHandler(productRepo, statusLogRepo,
            Substitute.For<IAccountRepository>(), Substitute.For<INotificationService>(),
            Substitute.For<IServiceScopeFactory>(), unitOfWork);
        var result  = await handler.Handle(
            new ApproveModerationCommand(ProductId, AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    [Fact]
    public async Task ApproveModeration_Republish_RunsPriceBranchAndSucceeds()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.OnModerating, price: 25m);
        productRepo.GetProductById(ProductId).Returns(product);

        // Товар уже публиковался раньше → срабатывает ветка повторной публикации (сравнение цены).
        statusLogRepo.GetByProductIdAsync(ProductId, Arg.Any<CancellationToken>())
            .Returns(new List<ProductStatusLog>
            {
                new(ProductId, ProductStatus.OnModerating, ProductStatus.Active, "первый аппрув"),
            });

        var handler = new ApproveModerationHandler(productRepo, statusLogRepo,
            Substitute.For<IAccountRepository>(), Substitute.For<INotificationService>(),
            Substitute.For<IServiceScopeFactory>(), unitOfWork);
        var result  = await handler.Handle(
            new ApproveModerationCommand(ProductId, AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(ProductStatus.Active, product.ProductStatus);
    }

    [Fact]
    public async Task RejectModeration_WithCustomReason_UsesCustomReason()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.OnModerating);
        productRepo.GetProductById(ProductId).Returns(product);

        const string reason = "Плагиат чужого контента";
        var handler = new RejectModerationHandler(productRepo, statusLogRepo,
            Substitute.For<IAccountRepository>(), Substitute.For<INotificationService>(), unitOfWork);
        var result  = await handler.Handle(
            new RejectModerationCommand(ProductId, AdminId, reason), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, ProductStatus: {product.ProductStatus}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(ProductStatus.ModerationFailed, product.ProductStatus);
        await statusLogRepo.Received(1).AddAsync(
            Arg.Is<ProductStatusLog>(l => l.Reason == reason),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectModeration_WithoutReason_UsesDefaultReason()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.OnModerating);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new RejectModerationHandler(productRepo, statusLogRepo,
            Substitute.For<IAccountRepository>(), Substitute.For<INotificationService>(), unitOfWork);
        var result  = await handler.Handle(
            new RejectModerationCommand(ProductId, AdminId, null), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        await statusLogRepo.Received(1).AddAsync(
            Arg.Is<ProductStatusLog>(l => l.Reason != null && l.Reason.Contains("администратором")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectModeration_NotOnModeration_ReturnsError()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Active);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new RejectModerationHandler(productRepo, statusLogRepo,
            Substitute.For<IAccountRepository>(), Substitute.For<INotificationService>(), unitOfWork);
        var result  = await handler.Handle(
            new RejectModerationCommand(ProductId, AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 4. HANDLERS — AdminHide / AdminSendToModeration
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AdminHide_ActiveProduct_SetsHiddenAndLogs()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Active);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new AdminHideProductHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new AdminHideProductCommand(ProductId, AdminId, "Спам"), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(ProductStatus.Hidden, product.ProductStatus);
        await statusLogRepo.Received(1).AddAsync(
            Arg.Is<ProductStatusLog>(l =>
                l.NewStatus == ProductStatus.Hidden &&
                l.Reason == "Спам"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminHide_AlreadyHidden_ReturnsError()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Hidden);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new AdminHideProductHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new AdminHideProductCommand(ProductId, AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("уже скрыт", result.ErrorMessage);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminHide_BannedProduct_ReturnsError()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Banned);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new AdminHideProductHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new AdminHideProductCommand(ProductId, AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminHide_ArchivedProduct_ReturnsError()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Archived);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new AdminHideProductHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new AdminHideProductCommand(ProductId, AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    [Fact]
    public async Task AdminHide_ProductNotFound_ReturnsError()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        productRepo.GetProductById(ProductId).ReturnsNull();

        var handler = new AdminHideProductHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new AdminHideProductCommand(ProductId, AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    [Fact]
    public async Task AdminSendToModeration_ActiveProduct_SetsOnModeratingAndLogs()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Active);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new AdminSendToModerationHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new AdminSendToModerationCommand(ProductId, AdminId, "Повторная проверка"), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(ProductStatus.OnModerating, product.ProductStatus);
        await statusLogRepo.Received(1).AddAsync(
            Arg.Is<ProductStatusLog>(l =>
                l.OldStatus == ProductStatus.Active &&
                l.NewStatus == ProductStatus.OnModerating &&
                l.Reason == "Повторная проверка"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminSendToModeration_AlreadyOnModeration_ReturnsError()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.OnModerating);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new AdminSendToModerationHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new AdminSendToModerationCommand(ProductId, AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("уже находится на модерации", result.ErrorMessage);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdminSendToModeration_BannedProduct_ReturnsError()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Banned);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new AdminSendToModerationHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new AdminSendToModerationCommand(ProductId, AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    [Fact]
    public async Task AdminSendToModeration_ArchivedProduct_ReturnsError()
    {
        var (productRepo, statusLogRepo, unitOfWork) = MakeModerationDeps();
        var product = MakeProduct(ProductStatus.Archived);
        productRepo.GetProductById(ProductId).Returns(product);

        var handler = new AdminSendToModerationHandler(productRepo, statusLogRepo, unitOfWork);
        var result  = await handler.Handle(
            new AdminSendToModerationCommand(ProductId, AdminId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 5. CHECKOUT — проверка статуса товаров перед оплатой
    // ═══════════════════════════════════════════════════════════════════════════

    private CreateCheckoutHandler MakeCheckoutHandler(IProductRepository productRepo)
    {
        var unitOfWork      = Substitute.For<IUnitOfWork>();
        var orderRepo       = Substitute.For<IOrderRepository>();
        var transactionRepo = Substitute.For<IUserTransactionRepository>();
        var paymentSvc      = Substitute.For<IPaymentGatewayService>();
        var accountRepo     = Substitute.For<IAccountRepository>();
        var contentFileRepo = Substitute.For<IContentFileRepository>();
        var accessRepo      = Substitute.For<IContentAccessRepository>();

        // PricingConfig — стандартные настройки
        var pricingOpts = Options.Create(new PricingConfig
        {
            CapN = 30, MinOvershoot = 1.2, MaxOvershoot = 2.0,
        });

        return new CreateCheckoutHandler(
            unitOfWork, orderRepo, productRepo,
            transactionRepo, paymentSvc, accountRepo,
            contentFileRepo, accessRepo, pricingOpts,
            Substitute.For<IEventTracker>());
    }

    [Fact]
    public async Task Checkout_BannedProduct_ReturnsErrorBeforePayment()
    {
        var productRepo = Substitute.For<IProductRepository>();
        var product     = MakeProduct(ProductStatus.Banned);
        productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });

        var handler = MakeCheckoutHandler(productRepo);
        var result  = await handler.Handle(
            new CreateCheckoutCommand(UserId, new List<CheckoutItemDTO>
            {
                new() { ProductId = product.Id, FileIds = new List<Guid>() }
            }), CancellationToken.None);

        _output.WriteLine($"Error: {result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("недоступны для покупки", result.ErrorMessage);
        Assert.Contains("Fire Pack Vol.3", result.ErrorMessage);
    }

    [Fact]
    public async Task Checkout_HiddenProduct_ReturnsError()
    {
        var productRepo = Substitute.For<IProductRepository>();
        var product     = MakeProduct(ProductStatus.Hidden);
        productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });

        var handler = MakeCheckoutHandler(productRepo);
        var result  = await handler.Handle(
            new CreateCheckoutCommand(UserId, new List<CheckoutItemDTO>
            {
                new() { ProductId = product.Id, FileIds = new List<Guid>() }
            }), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("недоступны для покупки", result.ErrorMessage);
    }

    [Fact]
    public async Task Checkout_OnModeratingProduct_ReturnsError()
    {
        var productRepo = Substitute.For<IProductRepository>();
        var product     = MakeProduct(ProductStatus.OnModerating);
        productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });

        var handler = MakeCheckoutHandler(productRepo);
        var result  = await handler.Handle(
            new CreateCheckoutCommand(UserId, new List<CheckoutItemDTO>
            {
                new() { ProductId = product.Id, FileIds = new List<Guid>() }
            }), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    [Fact]
    public async Task Checkout_MixOfActiveAndBanned_ReturnsErrorWithBannedProductName()
    {
        var productRepo = Substitute.For<IProductRepository>();
        var active      = MakeProduct(ProductStatus.Active, price: 20m);
        var banned      = MakeProduct(ProductStatus.Banned, price: 30m);
        SetId(active, 1);
        SetId(banned, 2);
        productRepo.GetProductsByIds(Arg.Any<List<int>>())
            .Returns(new List<Product> { active, banned });

        var handler = MakeCheckoutHandler(productRepo);
        var result  = await handler.Handle(
            new CreateCheckoutCommand(UserId, new List<CheckoutItemDTO>
            {
                new() { ProductId = active.Id, FileIds = new List<Guid>() },
                new() { ProductId = banned.Id, FileIds = new List<Guid>() },
            }), CancellationToken.None);

        _output.WriteLine($"Error: {result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        // Сообщение должно содержать имя заблокированного товара
        Assert.Contains("Fire Pack Vol.3", result.ErrorMessage);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 6. ADMIN ORDER — кастомная цена через priceOverrides
    // ═══════════════════════════════════════════════════════════════════════════

    private static CreateOrderDevHandler MakeOrderDevHandler(
        IProductRepository productRepo,
        IAccountRepository accountRepo,
        IOrderRepository orderRepo,
        IContentFileRepository contentFileRepo,
        IContentAccessRepository accessRepo,
        IUnitOfWork unitOfWork) =>
        new(unitOfWork, orderRepo, productRepo, accountRepo, contentFileRepo, accessRepo);

    private static User MakeCustomer() => User.Create("Test Customer", "customer@test.com");

    [Fact]
    public async Task AdminOrder_WithCustomPrice_OrderPriceMatchesCustomPrice()
    {
        // Arrange
        var product = MakeProduct(ProductStatus.Active, price: 18m); // реальная цена $18
        var customer = MakeCustomer();

        var productRepo     = Substitute.For<IProductRepository>();
        var accountRepo     = Substitute.For<IAccountRepository>();
        var orderRepo       = Substitute.For<IOrderRepository>();
        var contentFileRepo = Substitute.For<IContentFileRepository>();
        var accessRepo      = Substitute.For<IContentAccessRepository>();
        var unitOfWork      = Substitute.For<IUnitOfWork>();

        productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });
        accountRepo.GetByIdAsync(customer.Id).Returns(Task.FromResult<User?>(customer));
        contentFileRepo.GetByProductIdAsync(product.Id).Returns(new List<ContentFile>());

        Order? capturedOrder = null;
        orderRepo.AddAsync(Arg.Do<Order>(o => capturedOrder = o));

        var handler = MakeOrderDevHandler(productRepo, accountRepo, orderRepo, contentFileRepo, accessRepo, unitOfWork);

        // Act — кастомная цена $108 вместо $18
        var dto = new CreateOrderDevDTO
        {
            ClientId    = customer.Id,
            ProductsIds = new List<int> { product.Id },
            Price       = 108m,
        };
        var result = await handler.Handle(new CreateOrderDevCommand(dto), CancellationToken.None);

        // Assert
        _output.WriteLine($"Status: {result.Status}, OrderPrice: {capturedOrder?.Price}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.NotNull(capturedOrder);
        Assert.Equal(108m, capturedOrder!.Price);  // кастомная цена попала в заказ
    }

    [Fact]
    public async Task AdminOrder_ZeroCustomPrice_UsesProductStoredPrice()
    {
        var product  = MakeProduct(ProductStatus.Active, price: 25m);
        var customer = MakeCustomer();

        var productRepo     = Substitute.For<IProductRepository>();
        var accountRepo     = Substitute.For<IAccountRepository>();
        var orderRepo       = Substitute.For<IOrderRepository>();
        var contentFileRepo = Substitute.For<IContentFileRepository>();
        var accessRepo      = Substitute.For<IContentAccessRepository>();
        var unitOfWork      = Substitute.For<IUnitOfWork>();

        productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });
        accountRepo.GetByIdAsync(customer.Id).Returns(Task.FromResult<User?>(customer));
        contentFileRepo.GetByProductIdAsync(product.Id).Returns(new List<ContentFile>());

        Order? capturedOrder = null;
        orderRepo.AddAsync(Arg.Do<Order>(o => capturedOrder = o));

        var handler = MakeOrderDevHandler(productRepo, accountRepo, orderRepo, contentFileRepo, accessRepo, unitOfWork);

        var dto = new CreateOrderDevDTO
        {
            ClientId    = customer.Id,
            ProductsIds = new List<int> { product.Id },
            Price       = 0m,   // нет кастомной цены
        };
        var result = await handler.Handle(new CreateOrderDevCommand(dto), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, OrderPrice: {capturedOrder?.Price}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.NotNull(capturedOrder);
        Assert.Equal(25m, capturedOrder!.Price);  // берётся цена из прайса
    }

    [Fact]
    public async Task AdminOrder_MultipleProducts_CustomPriceDistributedProportionally()
    {
        // Product A: $20, Product B: $80 → normalTotal = $100
        // CustomPrice = $50 → A получает $10, B получает $40
        var productA = MakeProduct(ProductStatus.Active, price: 20m);
        var productB = MakeProduct(ProductStatus.Active, price: 80m);
        SetId(productA, 10);
        SetId(productB, 11);
        var customer = MakeCustomer();

        var productRepo     = Substitute.For<IProductRepository>();
        var accountRepo     = Substitute.For<IAccountRepository>();
        var orderRepo       = Substitute.For<IOrderRepository>();
        var contentFileRepo = Substitute.For<IContentFileRepository>();
        var accessRepo      = Substitute.For<IContentAccessRepository>();
        var unitOfWork      = Substitute.For<IUnitOfWork>();

        productRepo.GetProductsByIds(Arg.Any<List<int>>())
            .Returns(new List<Product> { productA, productB });
        accountRepo.GetByIdAsync(customer.Id).Returns(Task.FromResult<User?>(customer));
        contentFileRepo.GetByProductIdAsync(Arg.Any<int>()).Returns(new List<ContentFile>());

        Order? capturedOrder = null;
        orderRepo.AddAsync(Arg.Do<Order>(o => capturedOrder = o));

        var handler = MakeOrderDevHandler(productRepo, accountRepo, orderRepo, contentFileRepo, accessRepo, unitOfWork);

        var dto = new CreateOrderDevDTO
        {
            ClientId    = customer.Id,
            ProductsIds = new List<int> { productA.Id, productB.Id },
            Price       = 50m,
        };
        var result = await handler.Handle(new CreateOrderDevCommand(dto), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, OrderPrice: {capturedOrder?.Price}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.NotNull(capturedOrder);
        // Итоговая сумма заказа = $50 (кастомная цена, распределённая без потерь округления)
        Assert.Equal(50m, capturedOrder!.Price);
    }

    [Fact]
    public async Task AdminOrder_CustomPrice_AddSpendCalledWithCustomPrice()
    {
        var product  = MakeProduct(ProductStatus.Active, price: 18m);
        var customer = MakeCustomer();

        var productRepo     = Substitute.For<IProductRepository>();
        var accountRepo     = Substitute.For<IAccountRepository>();
        var orderRepo       = Substitute.For<IOrderRepository>();
        var contentFileRepo = Substitute.For<IContentFileRepository>();
        var accessRepo      = Substitute.For<IContentAccessRepository>();
        var unitOfWork      = Substitute.For<IUnitOfWork>();

        productRepo.GetProductsByIds(Arg.Any<List<int>>()).Returns(new List<Product> { product });
        accountRepo.GetByIdAsync(customer.Id).Returns(Task.FromResult<User?>(customer));
        contentFileRepo.GetByProductIdAsync(product.Id).Returns(new List<ContentFile>());

        User? capturedCustomer = null;
        accountRepo.When(r => r.Update(Arg.Any<User>()))
            .Do(ci => capturedCustomer = ci.Arg<User>());

        var handler = MakeOrderDevHandler(productRepo, accountRepo, orderRepo, contentFileRepo, accessRepo, unitOfWork);

        var dto = new CreateOrderDevDTO
        {
            ClientId    = customer.Id,
            ProductsIds = new List<int> { product.Id },
            Price       = 108m,
        };
        await handler.Handle(new CreateOrderDevCommand(dto), CancellationToken.None);

        _output.WriteLine($"Customer LifetimeSpent: {capturedCustomer?.LifetimeSpent}");
        Assert.NotNull(capturedCustomer);
        // LifetimeSpent должен учитывать кастомную цену $108, а не $18
        Assert.Equal(108m, capturedCustomer!.LifetimeSpent);
        accountRepo.Received(1).Update(Arg.Any<User>());
    }
}
