using Xunit;
using Calcvik.Models;
using Calcvik.ViewModels;

public class PolishTests
{
    [Fact]
    public void FormatValue_RendersAllFourBases()
    {
        var e = new CalculatorEngine();

        Assert.Equal("255", e.FormatValue(255m, BaseMode.Decimal));
        Assert.Equal("FF", e.FormatValue(255m, BaseMode.Hexadecimal));
        Assert.Equal("377", e.FormatValue(255m, BaseMode.Octal));
        Assert.Equal("11111111", e.FormatValue(255m, BaseMode.Binary));
    }

    [Fact]
    public void FormatValue_NegativeUsesTwosComplement()
    {
        var e = new CalculatorEngine();

        // 64-bit two's complement, independent of the active base mode.
        Assert.Equal("FFFFFFFFFFFFFFFF", e.FormatValue(-1m, BaseMode.Hexadecimal));
        Assert.Equal("1777777777777777777777", e.FormatValue(-1m, BaseMode.Octal));
        Assert.Equal("1111111111111111111111111111111111111111111111111111111111111111",
            e.FormatValue(-1m, BaseMode.Binary));
    }

    [Fact]
    public void FormatValue_DoesNotDependOnActiveBase()
    {
        var e = new CalculatorEngine();
        e.SetBaseMode(BaseMode.Binary);

        // The active base changes how the engine displays input, but FormatValue keeps its argument's base.
        Assert.Equal("FF", e.FormatValue(255m, BaseMode.Hexadecimal));
        Assert.Equal("11111111", e.ConvertToBase(255m));
    }

    [Fact]
    public void CurrentValue_TracksEnteredDigits()
    {
        var e = new CalculatorEngine();
        e.AppendDigit(4);
        e.AppendDigit(2);

        Assert.Equal(42m, e.CurrentValue);
        Assert.False(e.HasError);
    }

    [Fact]
    public void ViewModel_ExposesAllFourBaseValues()
    {
        var vm = new CalculatorViewModel();
        vm.DigitCommand.Execute("2");
        vm.DigitCommand.Execute("5");
        vm.DigitCommand.Execute("5");

        Assert.Equal("255", vm.DecValue);
        Assert.Equal("FF", vm.HexValue);
        Assert.Equal("377", vm.OctValue);
        Assert.Equal("11111111", vm.BinValue);
    }

    [Fact]
    public void ViewModel_BaseTileSwitchKeepsTheValue()
    {
        var vm = new CalculatorViewModel();
        vm.DigitCommand.Execute("2");
        vm.DigitCommand.Execute("5");
        vm.DigitCommand.Execute("5");
        vm.SetBaseCommand.Execute("HEX");

        Assert.True(vm.IsHexBase);
        Assert.Equal("FF", vm.DisplayText);
        // The other bases keep showing the same numeric value.
        Assert.Equal("255", vm.DecValue);
    }

    [Fact]
    public void SwitchingToProgrammer_TruncatesFractionalValue()
    {
        var vm = new CalculatorViewModel();
        vm.DigitCommand.Execute("1");
        vm.DigitCommand.Execute("2");
        vm.DecimalCommand.Execute(null);
        vm.DigitCommand.Execute("5");
        Assert.Equal("12.5", vm.DisplayText);

        vm.SelectedMode = CalculatorMode.Programmer;

        // Programmer is integer-only: the stored value is retyped, not just re-rendered.
        Assert.Equal("12", vm.DisplayText);
    }

    [Fact]
    public void SwitchingFromProgrammerHexBackToStandard_ShowsDecimal()
    {
        var vm = new CalculatorViewModel();
        vm.SelectedMode = CalculatorMode.Programmer;
        vm.SetBaseCommand.Execute("HEX");
        vm.DigitCommand.Execute("2");
        vm.DigitCommand.Execute("5");   // 0x25 = 37
        Assert.Equal("25", vm.DisplayText);

        vm.SelectedMode = CalculatorMode.Standard;

        // Base switching is a Programmer concept: leaving it must not leave a hex string on screen.
        Assert.True(vm.IsDecimalBase);
        Assert.Equal("37", vm.DisplayText);
    }

    [Fact]
    public void SwitchingBackToProgrammer_KeepsIntegerValue()
    {
        var vm = new CalculatorViewModel();
        vm.SelectedMode = CalculatorMode.Programmer;
        vm.DigitCommand.Execute("4");
        vm.DigitCommand.Execute("2");

        vm.SelectedMode = CalculatorMode.Scientific;
        vm.SelectedMode = CalculatorMode.Programmer;

        Assert.Equal("42", vm.DisplayText);
    }

    [Fact]
    public void Parser_OverflowIsReportedNotSwallowed()
    {
        var vm = new CalculatorViewModel();
        vm.SelectedMode = CalculatorMode.Scientific;

        vm.ExpressionInput = "2 ^ 10000";
        vm.EvaluateExpressionCommand.Execute(null);
        Assert.Equal("Overflow", vm.DisplayText);
        Assert.True(vm.IsError);

        vm.ExpressionInput = "10 ^ 30";
        vm.EvaluateExpressionCommand.Execute(null);
        Assert.Equal("Overflow", vm.DisplayText);
    }

    [Fact]
    public void Parser_DivideByZeroKeepsTheWording()
    {
        var vm = new CalculatorViewModel();
        vm.SelectedMode = CalculatorMode.Scientific;
        vm.ExpressionInput = "1 / 0";
        vm.EvaluateExpressionCommand.Execute(null);

        Assert.Equal("Cannot divide by zero", vm.DisplayText);
    }

    [Fact]
    public void ArithmeticOverflow_ShowsOverflow()
    {
        var vm = new CalculatorViewModel();
        foreach (var ch in "9999999999999999") vm.DigitCommand.Execute(ch.ToString());
        vm.MultiplyCommand.Execute(null);
        foreach (var ch in "9999999999999999") vm.DigitCommand.Execute(ch.ToString());
        vm.EqualsCommand.Execute(null);

        Assert.Equal("Overflow", vm.DisplayText);
        Assert.True(vm.IsError);
    }

    [Fact]
    public void SquareOfHugeValue_ShowsOverflowInsteadOfCrashing()
    {
        var vm = new CalculatorViewModel();
        foreach (var ch in "9999999999999999") vm.DigitCommand.Execute(ch.ToString());
        vm.SquareCommand.Execute(null);

        Assert.Equal("Overflow", vm.DisplayText);
        Assert.True(vm.IsError);
    }

    [Fact]
    public void ProgrammerDisplay_OverflowsLikeARealCalculator()
    {
        var vm = new CalculatorViewModel();
        vm.SelectedMode = CalculatorMode.Scientific;
        vm.ExpressionInput = "10 ^ 25";                 // fits decimal, far beyond 64-bit
        vm.EvaluateExpressionCommand.Execute(null);
        Assert.False(vm.IsError);

        vm.SelectedMode = CalculatorMode.Programmer;    // Programmer views are 64-bit
        Assert.Equal("Overflow", vm.DisplayText);
        // While the display is in an error state the base tiles show a dash instead of a bogus value.
        Assert.Equal("-", vm.HexValue);
    }

    [Fact]
    public void EngineErrorState_ShowsDashesInBaseTiles()
    {
        var vm = new CalculatorViewModel();
        vm.SelectedMode = CalculatorMode.Scientific;
        vm.ExpressionInput = "2 ^ 10000";
        vm.EvaluateExpressionCommand.Execute(null);

        vm.SelectedMode = CalculatorMode.Programmer;
        Assert.Equal("Overflow", vm.DisplayText);
        Assert.Equal("-", vm.HexValue);
    }

    [Fact]
    public void TypingAfterOverflow_StartsFresh()
    {
        var vm = new CalculatorViewModel();
        vm.SelectedMode = CalculatorMode.Scientific;
        vm.ExpressionInput = "2 ^ 10000";
        vm.EvaluateExpressionCommand.Execute(null);
        Assert.Equal("Overflow", vm.DisplayText);

        vm.DigitCommand.Execute("7");
        Assert.Equal("7", vm.DisplayText);
        Assert.False(vm.IsError);
    }

    [Fact]
    public void TypingAfterExpressionError_DiscardsPreviousValue()
    {
        var vm = new CalculatorViewModel();
        vm.DigitCommand.Execute("1");
        vm.DigitCommand.Execute("2");
        vm.SelectedMode = CalculatorMode.Scientific;
        vm.ExpressionInput = "2 ^ 10000";
        vm.EvaluateExpressionCommand.Execute(null);

        vm.DigitCommand.Execute("7");

        Assert.Equal("7", vm.DisplayText);
        Assert.False(vm.IsError);
    }

    [Fact]
    public void Clear_RemovesScientificExpression()
    {
        var vm = new CalculatorViewModel
        {
            SelectedMode = CalculatorMode.Scientific,
            ExpressionInput = "2 + 3"
        };

        vm.ClearCommand.Execute(null);

        Assert.Empty(vm.ExpressionInput);
        Assert.Equal("0", vm.DisplayText);
    }

    [Fact]
    public void MemoryOverflow_ShowsErrorInsteadOfThrowing()
    {
        var vm = new CalculatorViewModel
        {
            SelectedMode = CalculatorMode.Scientific,
            ExpressionInput = "70000000000000000000000000000"
        };
        vm.EvaluateExpressionCommand.Execute(null);
        vm.MemoryStoreCommand.Execute(null);

        vm.MemoryAddCommand.Execute(null);

        Assert.Equal("Overflow", vm.DisplayText);
        Assert.True(vm.IsError);
    }

    [Fact]
    public void ViewModel_InverseToggleFlipsAndResets()
    {
        var vm = new CalculatorViewModel();
        vm.SelectModeCommand.Execute("Scientific");

        Assert.False(vm.IsInverseMode);
        vm.ToggleInverseCommand.Execute(null);
        Assert.True(vm.IsInverseMode);

        vm.SelectModeCommand.Execute("Standard");
        Assert.False(vm.IsInverseMode);
    }

    [Fact]
    public void ViewModel_InversePageInsertsInverseFunctionText()
    {
        var vm = new CalculatorViewModel();
        vm.SelectModeCommand.Execute("Scientific");
        vm.ToggleInverseCommand.Execute(null);

        vm.InsertTextCommand.Execute("asin(");

        Assert.Equal("asin(", vm.ExpressionInput);
    }
}
