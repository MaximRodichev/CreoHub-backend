using CreoHub.Application.Commands.OrderCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.OrderDTOs;
using CreoHub.Application.DTO.ProductDTOs;
using CreoHub.Application.Queries.Orders;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using CreoHub.Domain.Types;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.OrderTests;

public class OrderHandlerTests
{
    private readonly ITestOutputHelper _output;

    private readonly IOrderRepository _orderRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IProductRepository _productRepo;
    private readonly IPriceRepository _priceRepo;
    private readonly IContentFileRepository _contentFileRepo;
    private readonly IContentAccessRepository _contentAccessRepo;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ShopId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public OrderHandlerTests(ITestOutputHelper output)
    {
        _output           = output;
        _orderRepo        = Substitute.For<IOrderRepository>();
        _accountRepo      = Substitute.For<IAccountRepository>();
        _productRepo      = Substitute.For<IProductRepository>();
        _priceRepo        = Substitute.For<IPriceRepository>();
        _contentFileRepo  = Substitute.For<IContentFileRepository>();
        _contentAccessRepo= Substitute.For<IContentAccessRepository>();
        _unitOfWork       = Substitute.For<IUnitOfWork>();

        // Default: every product has no content files (prevents NRE in GetByProductIdAsync)
        _contentFileRepo.GetByProductIdAsync(Arg.Any<int>())
            .Returns(Task.FromResult(new List<ContentFile>()));
    }

    // ── GetUserOrders ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserOrders_WithOrders_ReturnsOrderList()
    {
        var orders = new List<OrderUserInfoDTO>
        {
            new OrderUserInfoDTO
            {
                OrderId = Guid.NewGuid(),
                OrderDate = DateTime.UtcNow.AddDays(-3),
                Status = OrderStatus.Completed,
                TotalPrice = 37m,
                Items = new List<OrderItemDTO>
                {
                    new OrderItemDTO { ProductId = 10, ProductName = "InOut ChickenRoad", PriceAtPurchase = 25m },
                    new OrderItemDTO { ProductId = 11, ProductName = "InOut TwistXmas",   PriceAtPurchase = 12m },
                }
            }
        };
        _orderRepo.GetUserOrders(UserId, 0, 20).Returns(orders);

        var handler = new GetUserOrdersHandler(_orderRepo);
        var result = await handler.Handle(new GetUserOrdersQuery(UserId, 20, 0), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Count: {result.Data?.Count}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Single(result.Data);
        Assert.Equal(37m, result.Data[0].TotalPrice);
        Assert.Equal(2, result.Data[0].Items.Count);
    }

    [Fact]
    public async Task GetUserOrders_WhenNoOrders_ReturnsEmptyList()
    {
        _orderRepo.GetUserOrders(UserId, 0, 20).Returns(new List<OrderUserInfoDTO>());

        var handler = new GetUserOrdersHandler(_orderRepo);
        var result = await handler.Handle(new GetUserOrdersQuery(UserId, 20, 0), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetUserOrders_RepositoryThrows_ReturnsError()
    {
        _orderRepo.GetUserOrders(UserId, Arg.Any<int>(), Arg.Any<int>())
            .Throws(new Exception("Query timeout"));

        var handler = new GetUserOrdersHandler(_orderRepo);
        var result = await handler.Handle(new GetUserOrdersQuery(UserId, 20, 0), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Query timeout", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 10)]
    [InlineData(2, 5)]
    public async Task GetUserOrders_Pagination_PassesCorrectParams(int page, int pageSize)
    {
        _orderRepo.GetUserOrders(UserId, page, pageSize).Returns(new List<OrderUserInfoDTO>());

        var handler = new GetUserOrdersHandler(_orderRepo);
        await handler.Handle(new GetUserOrdersQuery(UserId, pageSize, page), CancellationToken.None);

        await _orderRepo.Received(1).GetUserOrders(UserId, page, pageSize);
    }

    // ── GetOrderFullInfo ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderFullInfo_OwnerAccess_ReturnsFullInfo()
    {
        var orderId = Guid.NewGuid();
        var dto = new OrderFullInfoDTO
        {
            Id = orderId,
            CustomerId = UserId,
            CustomerName = "MaxG",
            Date = DateTime.UtcNow,
            Status = "Created",
            ProductItems = new List<ProductOrderInfoDTO>()
        };
        _orderRepo.GetOrderInfoById(orderId).Returns(Task.FromResult(dto));

        var handler = new GetOrderFullInfoQueryHandler(_orderRepo, _accountRepo);
        var result = await handler.Handle(new GetOrderFullInfoQuery(UserId, orderId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Customer: {result.Data?.CustomerName}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal("MaxG", result.Data.CustomerName);
    }

    [Fact]
    public async Task GetOrderFullInfo_WrongUser_ReturnsError()
    {
        var orderId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var dto = new OrderFullInfoDTO
        {
            Id = orderId,
            CustomerId = UserId,           // belongs to UserId
            CustomerName = "MaxG",
            Status = "Created",
            ProductItems = new List<ProductOrderInfoDTO>()
        };
        _orderRepo.GetOrderInfoById(orderId).Returns(Task.FromResult(dto));

        var handler = new GetOrderFullInfoQueryHandler(_orderRepo, _accountRepo);
        var result = await handler.Handle(
            new GetOrderFullInfoQuery(otherUserId, orderId),   // wrong user
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Access denied", result.ErrorMessage);
    }

    [Fact]
    public async Task GetOrderFullInfo_RepositoryThrows_ReturnsError()
    {
        _orderRepo.GetOrderInfoById(Arg.Any<Guid>()).Throws(new Exception("Order not found"));

        var handler = new GetOrderFullInfoQueryHandler(_orderRepo, _accountRepo);
        var result = await handler.Handle(
            new GetOrderFullInfoQuery(UserId, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ── GetOrdersShortInfoByShopId ────────────────────────────────────────────

    [Fact]
    public async Task GetOrdersShortInfoByShopId_ReturnsOrders()
    {
        var deals = new List<OrderShortInfoDTO>
        {
            new OrderShortInfoDTO
            {
                Id = Guid.NewGuid(), CustomerName = "Bob", Price = 25m,
                ProductNames = new List<string> { "ChickenRoad" }, Status = "Completed"
            }
        };
        _orderRepo.GetOrdersShortInfoByShopIdAsync(ShopId).Returns(Task.FromResult(deals));

        var handler = new GetOrdersShortInfoByShopIdHandler(_orderRepo);
        var result = await handler.Handle(new GetOrdersShortInfoByShopIdQuery(ShopId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Count: {result.Data?.Count}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Single(result.Data);
        Assert.Equal(25m, result.Data[0].Price);
    }

    [Fact]
    public async Task GetOrdersShortInfoByShopId_EmptyShop_ReturnsEmpty()
    {
        _orderRepo.GetOrdersShortInfoByShopIdAsync(ShopId).Returns(Task.FromResult(new List<OrderShortInfoDTO>()));

        var handler = new GetOrdersShortInfoByShopIdHandler(_orderRepo);
        var result = await handler.Handle(new GetOrdersShortInfoByShopIdQuery(ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetOrdersShortInfoByShopId_RepositoryThrows_ReturnsError()
    {
        _orderRepo.GetOrdersShortInfoByShopIdAsync(ShopId).Throws(new Exception("Query failed"));

        var handler = new GetOrdersShortInfoByShopIdHandler(_orderRepo);
        var result = await handler.Handle(new GetOrdersShortInfoByShopIdQuery(ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ── GetOrders (admin) ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrders_ReturnsAllOrders()
    {
        var orders = new List<Order>();   // empty but non-null
        _orderRepo.GetAllAsync().Returns(Task.FromResult(orders));

        var handler = new GetOrdersQueryHandler(_orderRepo);
        var result = await handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetOrders_RepositoryThrows_ReturnsError()
    {
        _orderRepo.GetAllAsync().Throws(new Exception("DB error"));

        var handler = new GetOrdersQueryHandler(_orderRepo);
        var result = await handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ── CreateOrderDev (admin: manual order without payment) ──────────────────

    [Fact]
    public async Task CreateOrderDev_ValidData_CreatesOrderAndReturnsSuccess()
    {
        var customer = User.Create("MaxG", "max@gmail.com");

        // Build a real product with a price so Order.Open() works
        var product = new Product("InOut ChickenRoad", "Full pack", ShopId);
        product.AddPrice(25m);

        var dto = new CreateOrderDevDTO
        {
            ClientId = customer.Id,
            ProductsIds = new List<int> { product.Id }
        };

        _accountRepo.GetByIdAsync(customer.Id).Returns(Task.FromResult<User?>(customer));
        _productRepo.GetProductsByIds(dto.ProductsIds).Returns(Task.FromResult(new List<Product> { product }));
        _orderRepo.AddAsync(Arg.Any<Order>()).Returns(ci => Task.FromResult(ci.Arg<Order>()));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new CreateOrderDevHandler(_unitOfWork, _orderRepo, _productRepo, _accountRepo, _contentFileRepo, _contentAccessRepo);
        var result = await handler.Handle(new CreateOrderDevCommand(dto), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");
        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.True(result.Data);
        await _orderRepo.Received(1).AddAsync(Arg.Any<Order>());
        // Handler calls SaveChanges twice: once after AddAsync, once after granting access + LifetimeSpent
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOrderDev_CustomerNotFound_ReturnsError()
    {
        _accountRepo.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        var handler = new CreateOrderDevHandler(_unitOfWork, _orderRepo, _productRepo, _accountRepo, _contentFileRepo, _contentAccessRepo);
        var result = await handler.Handle(
            new CreateOrderDevCommand(new CreateOrderDevDTO
            {
                ClientId = Guid.NewGuid(),
                ProductsIds = new List<int> { 1 }
            }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Customer not found", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateOrderDev_SomeProductsNotFound_ReturnsError()
    {
        var customer = User.Create("MaxG", "max@gmail.com");
        // Request 2 products but only 1 found
        _accountRepo.GetByIdAsync(customer.Id).Returns(Task.FromResult<User?>(customer));
        _productRepo.GetProductsByIds(Arg.Any<List<int>>())
            .Returns(Task.FromResult(new List<Product>()));   // 0 returned, 2 requested

        var handler = new CreateOrderDevHandler(_unitOfWork, _orderRepo, _productRepo, _accountRepo, _contentFileRepo, _contentAccessRepo);
        var result = await handler.Handle(
            new CreateOrderDevCommand(new CreateOrderDevDTO
            {
                ClientId = customer.Id,
                ProductsIds = new List<int> { 1, 2 }
            }),
            CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Error: {result.ErrorMessage}");
        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("not found", result.ErrorMessage);
    }
}
