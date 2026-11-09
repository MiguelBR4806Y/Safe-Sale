using SS.Services;

namespace SS.Tests;

public class InputValidatorTests
{
    [Theory]
    [InlineData("10.98", 10.98)]
    [InlineData("10", 10)]
    [InlineData("1098", 1098)]
    [InlineData(" 12.5 ", 12.5)]
    [InlineData("0.01", 0.01)]
    public void TryParsePrice_AcceptsDotDecimals(string input, double expected)
    {
        var ok = InputValidator.TryParsePrice(input, out var price, out var error);

        Assert.True(ok, error);
        Assert.Equal((decimal)expected, price);
    }

    [Theory]
    [InlineData("10,98")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("-0.5")]
    [InlineData("10.989")]
    [InlineData("10.")]
    [InlineData(".5")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParsePrice_RejectsInvalidValues(string input)
    {
        Assert.False(InputValidator.TryParsePrice(input, out _, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryParsePrice_Comma_UsesDotHint()
    {
        Assert.False(InputValidator.TryParsePrice("10,98", out _, out var error));
        Assert.Contains("punto", error);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("10", 10)]
    [InlineData("999", 999)]
    public void TryParseStock_AcceptsPositiveIntegers(string input, int expected)
    {
        var ok = InputValidator.TryParseStock(input, out var stock, out var error);

        Assert.True(ok, error);
        Assert.Equal(expected, stock);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("1,5")]
    [InlineData("")]
    [InlineData("abc")]
    public void TryParseStock_RejectsInvalidValues(string input)
    {
        Assert.False(InputValidator.TryParseStock(input, out _, out _));
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("5", 5)]
    public void TryParseMinStock_AcceptsNonNegativeIntegers(string input, int expected)
    {
        var ok = InputValidator.TryParseMinStock(input, out var minStock, out var error);

        Assert.True(ok, error);
        Assert.Equal(expected, minStock);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("")]
    [InlineData("abc")]
    public void TryParseMinStock_RejectsInvalidValues(string input)
    {
        Assert.False(InputValidator.TryParseMinStock(input, out _, out _));
    }
}
