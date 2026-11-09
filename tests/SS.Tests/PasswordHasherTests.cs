using SS.Services;

namespace SS.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void DefaultAdminPassword_Verifies()
    {
        var hash = PasswordHasher.HashPassword("admin123");

        Assert.True(PasswordHasher.VerifyPassword("admin123", hash));
    }

    [Fact]
    public void WrongPassword_DoesNotVerify()
    {
        var hash = PasswordHasher.HashPassword("admin123");

        Assert.False(PasswordHasher.VerifyPassword("admin1234", hash));
        Assert.False(PasswordHasher.VerifyPassword("", hash));
    }

    [Fact]
    public void DifferentSalts_ProduceDifferentHashes()
    {
        Assert.NotEqual(PasswordHasher.HashPassword("admin123"), PasswordHasher.HashPassword("admin123"));
    }

    [Fact]
    public void MalformedHash_DoesNotVerify()
    {
        Assert.False(PasswordHasher.VerifyPassword("admin123", "not-a-valid-hash"));
    }
}
