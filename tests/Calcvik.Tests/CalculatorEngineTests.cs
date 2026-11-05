using Calcvik.Models;
using Xunit;

public class CalculatorEngineTests
{
    [Fact]
    public void AddsTwoNumbers()
    {
        var e = new CalculatorEngine();
        e.AppendDigit(2); e.AppendOperator('+'); e.AppendDigit(3);
        Assert.Equal("5", e.Equals());
    }

    [Fact]
    public void DividesByZeroWithoutCrash()
    {
        var e = new CalculatorEngine();
        e.AppendDigit(5); e.AppendOperator('/'); e.AppendDigit(0);
        Assert.Equal("Cannot divide by zero", e.Equals());
    }

    [Fact]
    public void StartsFreshCalculationAfterEquals()
    {
        var e = new CalculatorEngine();
        e.AppendDigit(2); e.AppendOperator('+'); e.AppendDigit(3);
        Assert.Equal("5", e.Equals());

        e.AppendDigit(4); e.AppendOperator('+'); e.AppendDigit(5);
        Assert.Equal("9", e.Equals());
    }
}
