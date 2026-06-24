using CreoHub.Application.Commands.AdminCommands;
using CreoHub.Application.DTO.AdminDTOs;

namespace CreoHub.Tests.MergeTests;

/// <summary>Гарды объединения аккаунтов: что блокирует мердж, что пропускает.</summary>
public class AccountMergeGuardsTests
{
    private static MergeUserSummaryDto U(
        Guid? id = null, string role = "User", bool hasShop = false,
        long? tg = null, string? email = null)
        => new(
            id ?? Guid.NewGuid(),
            "Name", email, tg, tg is null ? null : "tguser",
            hasShop, role, 0m, 0m, 0m);

    [Fact]
    public void CleanMerge_NoBlockers()
    {
        // keep: только email, merge: только telegram, без шопов, обычные юзеры
        var keep  = U(email: "a@a.com");
        var merge = U(tg: 123);
        Assert.Empty(AccountMergeGuards.Evaluate(keep, merge));
    }

    [Fact]
    public void SameAccount_Blocked()
    {
        var id = Guid.NewGuid();
        var blockers = AccountMergeGuards.Evaluate(U(id), U(id));
        Assert.Contains(blockers, b => b.Contains("сам с собой"));
    }

    [Fact]
    public void KeepOwnsShop_Blocked()
    {
        var blockers = AccountMergeGuards.Evaluate(U(hasShop: true, email: "a@a.com"), U(tg: 1));
        Assert.Contains(blockers, b => b.Contains("магазином"));
    }

    [Fact]
    public void MergeOwnsShop_Blocked()
    {
        var blockers = AccountMergeGuards.Evaluate(U(email: "a@a.com"), U(hasShop: true, tg: 1));
        Assert.Contains(blockers, b => b.Contains("магазином"));
    }

    [Fact]
    public void BothHaveTelegram_Blocked()
    {
        var blockers = AccountMergeGuards.Evaluate(U(tg: 111), U(tg: 222));
        Assert.Contains(blockers, b => b.Contains("Telegram"));
    }

    [Fact]
    public void BothHaveEmail_Blocked()
    {
        var blockers = AccountMergeGuards.Evaluate(U(email: "a@a.com"), U(email: "b@b.com"));
        Assert.Contains(blockers, b => b.Contains("e-mail"));
    }

    [Fact]
    public void AdminAccount_Blocked()
    {
        var blockers = AccountMergeGuards.Evaluate(U(role: "Admin", email: "a@a.com"), U(tg: 1));
        Assert.Contains(blockers, b => b.Contains("Админ"));
    }
}
