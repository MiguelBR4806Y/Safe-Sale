using SS.Services;

namespace SS.Tests;

public class UserValidatorTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("juan.perez")]
    [InlineData("ana_1")]
    [InlineData("user-name")]
    [InlineData("  lucas  ")]
    public void TryValidateUsername_AcceptsValid(string input)
    {
        var ok = UserValidator.TryValidateUsername(input, out var username, out var error);

        Assert.True(ok, error);
        Assert.Equal(input.Trim(), username);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("con espacio")]
    [InlineData("josé")]
    [InlineData("user@dominio")]
    [InlineData("123456789012345678901234567890123")]
    public void TryValidateUsername_RejectsInvalid(string input)
    {
        Assert.False(UserValidator.TryValidateUsername(input, out _, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("admin123", "admin123")]
    [InlineData("clave segura", "clave segura")]
    [InlineData("123456", "123456")]
    public void TryValidatePassword_AcceptsValid(string password, string confirm)
    {
        Assert.True(UserValidator.TryValidatePassword(password, confirm, out var error), error);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("12345", "12345")]
    [InlineData("admin123", "admin124")]
    [InlineData("admin123", "")]
    [InlineData(" admin123", " admin123")]
    [InlineData("admin123 ", "admin123 ")]
    public void TryValidatePassword_RejectsInvalid(string password, string confirm)
    {
        Assert.False(UserValidator.TryValidatePassword(password, confirm, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryValidatePassword_Mismatch_HasClearMessage()
    {
        Assert.False(UserValidator.TryValidatePassword("admin123", "otra123", out var error));
        Assert.Contains("coinciden", error);
    }
}
