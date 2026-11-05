using Xunit;
using Calcvik.Models;

public class FeaturesTests
{
    [Fact]
    public void HexDigits_Append()
    {
        var e = new CalculatorEngine();
        e.SetBaseMode(BaseMode.Hexadecimal);
        e.AppendDigit(1);
        e.AppendHexDigit('A');
        e.AppendHexDigit('f');
        Assert.Equal("1AF", e.GetDisplayValue());
    }

    [Fact]
    public void HexDigit_RejectedInDecimal()
    {
        var e = new CalculatorEngine();
        e.SetBaseMode(BaseMode.Decimal);
        e.AppendHexDigit('A');
        Assert.Equal("0", e.ToString());
    }

    [Fact]
    public void ConvertToBase_Negative_IsTwosComplement()
    {
        var e = new CalculatorEngine();
        e.SetBaseMode(BaseMode.Hexadecimal);
        Assert.Equal("FFFFFFFFFFFFFFFF", e.ConvertToBase(-1m));
    }

    [Fact]
    public void ConvertToBase_ClampsLargeValue()
    {
        var e = new CalculatorEngine();
        e.SetBaseMode(BaseMode.Decimal);
        Assert.Equal("9223372036854775807", e.ConvertToBase(decimal.MaxValue));
    }

    [Fact]
    public void ShiftOutOfRange_ShowsError()
    {
        var e = new CalculatorEngine();
        e.SetInput("5");
        e.LeftShift();
        e.SetInput("64");
        Assert.Equal("Shift out of range", e.Equals());
    }

    [Fact]
    public void Asin_ReturnsDegrees()
    {
        var e = new CalculatorEngine();
        e.SetAngleMode(AngleMode.Degrees);
        var res = e.Evaluate("asin(1)");
        Assert.InRange(res, 89.999m, 90.001m);
    }

    [Theory]
    [InlineData("asin(2)")]
    [InlineData("acos(-2)")]
    [InlineData("sqrt(-4)")]
    [InlineData("log(0)")]
    [InlineData("ln(0)")]
    [InlineData("3.5!")]
    [InlineData("tan(90)")]
    public void InvalidScientificDomain_ThrowsFormatException(string expression)
    {
        var e = new CalculatorEngine();
        Assert.Throws<FormatException>(() => e.Evaluate(expression));
    }
}
