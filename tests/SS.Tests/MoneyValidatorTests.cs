using SS.Services;

namespace SS.Tests;

public class MoneyValidatorTests
{
    [Theory]
    [InlineData("0", 0)]
    [InlineData("100", 100)]
    [InlineData("100.50", 100.50)]
    [InlineData("0.01", 0.01)]
    public void TryParseMoney_AcceptsValid(string input, double expected)
    {
        var ok = InputValidator.TryParseMoney(input, out var amount, out var error);

        Assert.True(ok, error);
        Assert.Equal((decimal)expected, amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("10,50")]
    [InlineData("-1")]
    [InlineData("1.234")]
    [InlineData("abc")]
    [InlineData(".")]
    public void TryParseMoney_RejectsInvalid(string input)
    {
        Assert.False(InputValidator.TryParseMoney(input, out _, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
