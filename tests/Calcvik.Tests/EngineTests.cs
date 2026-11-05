using Xunit;
using Calcvik.Models;

public class EngineTests
{
    [Fact]
    public void ApplyPercent_ReturnsValueDividedBy100()
    {
        var e = new CalculatorEngine();
        e.AppendDigit(2); e.AppendDigit(0); e.AppendDigit(0);
        e.ApplyPercent();
        Assert.Equal("2", e.ToString());
    }

    [Fact]
    public void SquareRoot_ReturnsPositiveRoot()
    {
        var e = new CalculatorEngine();
        e.AppendDigit(9);
        e.SquareRoot();
        Assert.Equal("3", e.ToString());
    }

    [Fact]
    public void Reciprocal_ReturnsOneOverValue()
    {
        var e = new CalculatorEngine();
        e.AppendDigit(4);
        e.Reciprocal();
        Assert.Equal("0.25", e.ToString());
    }

    [Fact]
    public void BaseModeBinary_ReturnsBinaryString()
    {
        var e = new CalculatorEngine();
        e.SetBaseMode(BaseMode.Binary);
        e.AppendDigit(1); e.AppendDigit(0); e.AppendDigit(1);
        // typed digits are binary: "101" is five, displayed back as "101"
        Assert.Equal("101", e.GetInputInBase());
    }

    [Fact]
    public void EvaluateSimpleExpression_ReturnsCorrectResult()
    {
        var e = new CalculatorEngine();
        var res = e.Evaluate("2+3*4");
        Assert.Equal(14m, res);
    }

    [Fact]
    public void EvaluateSinDegrees_ReturnsOne()
    {
        var e = new CalculatorEngine();
        e.SetAngleMode(AngleMode.Degrees);
        var res = e.Evaluate("sin(90)");
        Assert.Equal(1m, res);
    }

    [Theory]
    [InlineData("-2^2", -4)]
    [InlineData("(-2)^2", 4)]
    [InlineData("2^-2", 0.25)]
    public void EvaluatePower_UsesStandardUnaryPrecedence(string expression, double expected)
    {
        var e = new CalculatorEngine();
        Assert.Equal((decimal)expected, e.Evaluate(expression));
    }

    [Fact]
    public void EvaluateUnknownCharacter_ThrowsFormatException()
    {
        var e = new CalculatorEngine();
        Assert.Throws<FormatException>(() => e.Evaluate("2$+3"));
    }

    [Fact]
    public void EvaluateOversizedNumber_ThrowsOverflowException()
    {
        var e = new CalculatorEngine();
        Assert.Throws<OverflowException>(() =>
            e.Evaluate("999999999999999999999999999999999999999999"));
    }
}
