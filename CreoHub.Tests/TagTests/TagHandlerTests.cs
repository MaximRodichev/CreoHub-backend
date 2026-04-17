using AutoMapper;
using CreoHub.Application.Commands.AdminCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.DTO.StatsDTOs;
using CreoHub.Application.Queries.Tag;
using CreoHub.Application.Repositories;
using CreoHub.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit.Abstractions;

namespace CreoHub.Tests.TagTests;

public class TagHandlerTests
{
    private readonly ITestOutputHelper _output;
    private readonly ITagRepository _tagRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private static readonly Guid ShopId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public TagHandlerTests(ITestOutputHelper output)
    {
        _output = output;
        _tagRepo = Substitute.For<ITagRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _mapper = Substitute.For<IMapper>();
    }

    // ── CreateTag ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTag_ValidName_ReturnsSuccess()
    {
        _tagRepo.AddAsync(Arg.Any<Tag>()).Returns(ci => Task.FromResult(ci.Arg<Tag>()));
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var handler = new CreateTagHandler(_unitOfWork, _tagRepo);
        var result = await handler.Handle(new CreateTagCommand("pragmatic play"), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.True(result.Data);
        await _tagRepo.Received(1).AddAsync(Arg.Any<Tag>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTag_RepositoryThrows_ReturnsError()
    {
        _tagRepo.AddAsync(Arg.Any<Tag>()).Throws(new Exception("Duplicate key"));

        var handler = new CreateTagHandler(_unitOfWork, _tagRepo);
        var result = await handler.Handle(new CreateTagCommand("netent"), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
        Assert.Contains("Duplicate key", result.ErrorMessage);
    }

    // ── GetTagsList ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTagsList_ReturnsMappedTagNames()
    {
        var tags = new List<Tag> { new Tag("pragmatic play"), new Tag("netent"), new Tag("evolution") };
        _tagRepo.GetAllAsync().Returns(Task.FromResult(tags));

        var handler = new GetTagsListHandler(_mapper, _tagRepo);
        var result = await handler.Handle(new GetTagsListCommand(), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Tags: {string.Join(", ", result.Data ?? [])}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(3, result.Data.Count);
        Assert.Contains("pragmatic play", result.Data);
        Assert.Contains("netent", result.Data);
    }

    [Fact]
    public async Task GetTagsList_EmptyRepository_ReturnsEmptyList()
    {
        _tagRepo.GetAllAsync().Returns(Task.FromResult(new List<Tag>()));

        var handler = new GetTagsListHandler(_mapper, _tagRepo);
        var result = await handler.Handle(new GetTagsListCommand(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetTagsList_RepositoryThrows_ReturnsError()
    {
        _tagRepo.GetAllAsync().Throws(new Exception("DB error"));

        var handler = new GetTagsListHandler(_mapper, _tagRepo);
        var result = await handler.Handle(new GetTagsListCommand(), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }

    // ── GetTagStatsByShop ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetTagStatsByShop_ReturnsStats()
    {
        var stats = new List<TagStatsDTO>
        {
            new TagStatsDTO { TagId = 1, TagName = "pragmatic play", TagRevenue = 500m, TagItemsSold = 20 },
            new TagStatsDTO { TagId = 2, TagName = "netent", TagRevenue = 300m, TagItemsSold = 12 },
        };
        _tagRepo.GetTagStatsByShopAsync(ShopId).Returns(Task.FromResult(stats));

        var handler = new GetTagStatsHandler(_tagRepo);
        var result = await handler.Handle(new GetTagStatsByShopQuery(ShopId), CancellationToken.None);

        _output.WriteLine($"Status: {result.Status}, Count: {result.Data?.Count}");

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("pragmatic play", result.Data[0].TagName);
        Assert.Equal(500m, result.Data[0].TagRevenue);
    }

    [Fact]
    public async Task GetTagStatsByShop_EmptyShop_ReturnsEmptyList()
    {
        _tagRepo.GetTagStatsByShopAsync(ShopId).Returns(Task.FromResult(new List<TagStatsDTO>()));

        var handler = new GetTagStatsHandler(_tagRepo);
        var result = await handler.Handle(new GetTagStatsByShopQuery(ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, result.Status);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetTagStatsByShop_RepositoryThrows_ReturnsError()
    {
        _tagRepo.GetTagStatsByShopAsync(ShopId).Throws(new Exception("Query failed"));

        var handler = new GetTagStatsHandler(_tagRepo);
        var result = await handler.Handle(new GetTagStatsByShopQuery(ShopId), CancellationToken.None);

        Assert.Equal(ResponseStatus.Error, result.Status);
    }
}
