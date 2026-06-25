using CreoHub.Application.Commands.AccountCommands;
using CreoHub.Application.DTO;
using CreoHub.Application.Queries.Admin;
using CreoHub.Application.Repositories;
using NSubstitute;

namespace CreoHub.Tests.RetentionTests;

/// <summary>Стичинг сессии + история поиска + flow субъекта (хендлеры).</summary>
public class RetentionAnalyticsTests
{
    // ── LinkSession ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LinkSession_CallsRepoWithArgs()
    {
        var events = Substitute.For<IUserEventRepository>();
        var uid = Guid.NewGuid();

        var res = await new LinkSessionHandler(events)
            .Handle(new LinkSessionCommand(uid, "sid-123"), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, res.Status);
        await events.Received(1).AttachSessionToUserAsync(uid, "sid-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinkSession_EmptySession_DoesNotCall()
    {
        var events = Substitute.For<IUserEventRepository>();
        await new LinkSessionHandler(events)
            .Handle(new LinkSessionCommand(Guid.NewGuid(), ""), CancellationToken.None);
        await events.DidNotReceive()
            .AttachSessionToUserAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── GetSearchHistory ─────────────────────────────────────────────────────

    [Fact]
    public async Task SearchHistory_ShapesItems_ResolvesNames()
    {
        var events = Substitute.For<IUserEventRepository>();
        var admin  = Substitute.For<IAdminRepository>();
        var uid = Guid.NewGuid();
        var items = new List<SearchHistoryItem>
        {
            new(DateTime.UtcNow, "sweet bonanza", true,  uid,  "sid1"),
            new(DateTime.UtcNow, "joker",         false, null, "sid2"),
        };
        events.GetSearchHistoryAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<bool>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
              .Returns((items, 2));
        admin.GetUserNamesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
             .Returns(new Dictionary<Guid, string> { [uid] = "Maxim" });

        var res = await new GetSearchHistoryHandler(events, admin)
            .Handle(new GetSearchHistoryQuery(30, true, 0, 50), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, res.Status);
        Assert.Equal(2, res.Data!.Total);
        Assert.Equal("Maxim", res.Data.Items[0].UserName);
        Assert.True(res.Data.Items[0].NoResults);
        Assert.Null(res.Data.Items[1].UserName);
    }

    // ── GetSubjectFlow ───────────────────────────────────────────────────────

    [Fact]
    public async Task Flow_NoSubject_Fails()
    {
        var res = await new GetSubjectFlowHandler(
                Substitute.For<IUserEventRepository>(), Substitute.For<IAdminRepository>())
            .Handle(new GetSubjectFlowQuery(null, null, 30), CancellationToken.None);
        Assert.Equal(ResponseStatus.Error, res.Status);
    }

    [Fact]
    public async Task Flow_ParsesDetail_AndProductNames()
    {
        var events = Substitute.For<IUserEventRepository>();
        var admin  = Substitute.For<IAdminRepository>();
        var raw = new List<FlowEventRaw>
        {
            new(DateTime.UtcNow, "search",       null, "{\"q\":\"tiger\"}",      "sid", null),
            new(DateTime.UtcNow, "page_view",    null, "{\"path\":\"/store\"}",  "sid", null),
            new(DateTime.UtcNow, "product_view", 7,    null,                     "sid", null),
        };
        events.GetSubjectFlowAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<DateTime>(),
                Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
              .Returns(raw);
        admin.GetProductNamesAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
             .Returns(new Dictionary<int, string> { [7] = "Tiger Gems" });

        var res = await new GetSubjectFlowHandler(events, admin)
            .Handle(new GetSubjectFlowQuery(null, "sid", 30), CancellationToken.None);

        Assert.Equal(ResponseStatus.Success, res.Status);
        var items = res.Data!.Items;
        Assert.Equal("tiger",      items[0].Detail);
        Assert.Equal("/store",     items[1].Detail);
        Assert.Equal("Tiger Gems", items[2].ProductName);
    }
}
