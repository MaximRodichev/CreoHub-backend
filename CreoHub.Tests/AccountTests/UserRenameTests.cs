using CreoHub.Domain.Entities;

namespace CreoHub.Tests.AccountTests;

/// <summary>
/// Тесты смены отображаемого имени (User.Rename). Имена НЕ уникальны.
/// </summary>
public class UserRenameTests
{
    private static User MakeUser() => User.Create("Old", "u@u.com");

    [Fact]
    public void Rename_ValidName_Changes()
    {
        var u = MakeUser();
        u.Rename("Максим");
        Assert.Equal("Максим", u.Name);
    }

    [Fact]
    public void Rename_TrimsWhitespace()
    {
        var u = MakeUser();
        u.Rename("  Максим  ");
        Assert.Equal("Максим", u.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_EmptyOrWhitespace_Throws(string name)
    {
        var u = MakeUser();
        Assert.Throws<ArgumentException>(() => u.Rename(name));
    }

    [Fact]
    public void Rename_TooLong_Throws()
    {
        var u = MakeUser();
        Assert.Throws<ArgumentException>(() => u.Rename(new string('x', 51)));
    }

    [Fact]
    public void Rename_DuplicateValueAcrossUsers_Allowed()
    {
        var a = User.Create("X", "a@a.com");
        var b = User.Create("Y", "b@b.com");
        a.Rename("Максим");
        b.Rename("Максим");
        Assert.Equal(a.Name, b.Name);   // дубли имён разрешены
    }
}
