using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Input;
using Calcvik.Models;
using Calcvik.Services;

namespace Calcvik.ViewModels;

public enum CalculatorMode { Standard, Scientific, Programmer }

public class CalculatorViewModel : INotifyPropertyChanged
{
    private readonly CalculatorEngine _engine;
    private string _displayText = "";
    private bool _isError;
    private string _expression = "";
    private readonly ThemeService _themeService = new();
    private CalculatorMode _selectedMode = CalculatorMode.Standard;
    private AppTheme _selectedTheme;

    private bool _isNavOpen;

    public bool IsNavOpen
    {
        get => _isNavOpen;
        set
        {
            if (_isNavOpen == value) return;
            _isNavOpen = value;
            OnPropertyChanged();
        }
    }
    private decimal _memory;
    public bool HasMemory => _memory != 0m;
    public RelayCommand MemoryStoreCommand { get; }
    public RelayCommand MemoryRecallCommand { get; }
    public RelayCommand MemoryAddCommand { get; }
    public RelayCommand MemorySubtractCommand { get; }
    public RelayCommand MemoryClearCommand { get; }

    public RelayCommand ToggleNavCommand { get; }

    public RelayCommand SelectModeCommand { get; }
    public RelayCommand SquareRootCommand { get; }
    public RelayCommand SquareCommand { get; }
    public RelayCommand ReciprocalCommand { get; }
    public RelayCommand PercentCommand { get; }
    public RelayCommand BitwiseAndCommand { get; }
    public RelayCommand BitwiseOrCommand { get; }
    public RelayCommand BitwiseXorCommand { get; }
    public RelayCommand BitwiseNotCommand { get; }
    public RelayCommand LeftShiftCommand { get; }
    public RelayCommand RightShiftCommand { get; }
    public RelayCommand ToggleAngleModeCommand { get; }
    public RelayCommand EvaluateExpressionCommand { get; }
    public RelayCommand SetBaseCommand { get; }
    public RelayCommand InsertTextCommand { get; }
    private string _expressionInput = string.Empty;
    private AngleMode _currentAngleMode = AngleMode.Degrees;
    private BaseMode _currentBaseMode = BaseMode.Decimal;
    public string ExpressionInput { get => _expressionInput; set { if (_expressionInput == value) return; _expressionInput = value; OnPropertyChanged(); } }
    public AngleMode CurrentAngleMode { get => _currentAngleMode; set { if (_currentAngleMode == value) return; _currentAngleMode = value; _engine.SetAngleMode(value); OnPropertyChanged(); OnPropertyChanged(nameof(AngleModeText)); } }

    public string AngleModeText => _currentAngleMode == AngleMode.Degrees ? "DEG" : "RAD";
    public BaseMode CurrentBaseMode
    {
        get => _currentBaseMode; set
        {
            if (_currentBaseMode == value) return; _currentBaseMode = value; _engine.SetBaseMode(value); OnPropertyChanged(); DisplayText = _engine.GetInputInBase();
            OnPropertyChanged(nameof(IsHexBase));
            OnPropertyChanged(nameof(IsBinaryBase));
            OnPropertyChanged(nameof(IsOctalBase));
            OnPropertyChanged(nameof(IsDecimalBase));
            OnPropertyChanged(nameof(IsTwoToSevenEnabled));
            OnPropertyChanged(nameof(IsEightNineEnabled));
            RaiseBaseValues();
        }
    }

    // ---- Programmer: live value in all four bases ----
    private string BaseText(BaseMode mode)
    {
        if (IsError || _engine.HasError) return "-";
        var value = _engine.CurrentValue;
        if (value > long.MaxValue || value < long.MinValue) return "Overflow";
        return _engine.FormatValue(value, mode);
    }

    public string HexValue => BaseText(BaseMode.Hexadecimal);
    public string DecValue => BaseText(BaseMode.Decimal);
    public string OctValue => BaseText(BaseMode.Octal);
    public string BinValue => BaseText(BaseMode.Binary);

    // ---- Scientific: 2nd page ----
    private bool _isInverseMode;
    public bool IsInverseMode
    {
        get => _isInverseMode;
        set
        {
            if (_isInverseMode == value) return;
            _isInverseMode = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand ToggleInverseCommand { get; }

    public bool IsHexBase => _currentBaseMode == BaseMode.Hexadecimal;
    public bool IsBinaryBase => _currentBaseMode == BaseMode.Binary;
    public bool IsOctalBase => _currentBaseMode == BaseMode.Octal;
    public bool IsDecimalBase => !IsHexBase && !IsBinaryBase && !IsOctalBase;
    public bool IsTwoToSevenEnabled => !IsBinaryBase;
    public bool IsEightNineEnabled => !IsBinaryBase && !IsOctalBase;

    public string DisplayText
    {
        get => _displayText;
        set
        {
            if (_displayText != value)
            {
                _displayText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>True while the display shows an invalid-expression error, so the view can paint the error surface.</summary>
    public bool IsError
    {
        get => _isError;
        private set
        {
            if (_isError != value)
            {
                _isError = value;
                OnPropertyChanged();
            }
        }
    }

    public string Expression
    {
        get => _expression;
        set
        {
            if (_expression != value)
            {
                _expression = value;
                OnPropertyChanged();
            }
        }
    }

    // Commands
    public RelayCommand ClearCommand { get; }
    public RelayCommand BackspaceCommand { get; }
    public RelayCommand NegateCommand { get; }
    public RelayCommand DivideCommand { get; }
    public RelayCommand MultiplyCommand { get; }
    public RelayCommand SubtractCommand { get; }
    public RelayCommand AddCommand { get; }
    public RelayCommand EqualsCommand { get; }
    public RelayCommand DecimalCommand { get; }
    public RelayCommand DigitCommand { get; }
    // Deterministic window geometry per mode: the window must not be sized by its content.
    public double WindowWidth => IsProgrammer ? 592 : IsScientific ? 540 : 404;
    public double WindowHeight => IsProgrammer ? 640 : IsScientific ? 632 : 584;

    public CalculatorMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (_selectedMode == value) return;
            _selectedMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsScientific));
            OnPropertyChanged(nameof(IsProgrammer));
            OnPropertyChanged(nameof(IsStandardMode));
            OnPropertyChanged(nameof(IsScientificMode));
            OnPropertyChanged(nameof(IsProgrammerMode));
            if (!IsScientific) IsInverseMode = false;
            RetypeForMode();
            RaiseBaseValues();
            OnPropertyChanged(nameof(WindowWidth));
            OnPropertyChanged(nameof(WindowHeight));
        }
    }
    public bool IsScientific => _selectedMode == CalculatorMode.Scientific;
    public bool IsProgrammer => _selectedMode == CalculatorMode.Programmer;

    public bool IsStandardMode => _selectedMode == CalculatorMode.Standard;
    public bool IsScientificMode => _selectedMode == CalculatorMode.Scientific;
    public bool IsProgrammerMode => _selectedMode == CalculatorMode.Programmer;
    public bool IsLightTheme => _selectedTheme == AppTheme.Light;
    public bool IsDarkTheme => _selectedTheme == AppTheme.Dark;
    public bool IsSystemTheme => _selectedTheme == AppTheme.System;

    public RelayCommand SetThemeCommand { get; }

    public AppTheme SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme == value) return;
            _selectedTheme = value;
            OnPropertyChanged();
            _themeService.Save(value);
            _themeService.Apply(value);
            OnPropertyChanged(nameof(IsLightTheme));
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(IsSystemTheme));
        }
    }

    public CalculatorViewModel()
    {
        _engine = new CalculatorEngine();
        ClearCommand = new RelayCommand(Clear);
        BackspaceCommand = new RelayCommand(Backspace);
        NegateCommand = new RelayCommand(Negate);
        DivideCommand = new RelayCommand(() => AppendOperator('/'));
        MultiplyCommand = new RelayCommand(() => AppendOperator('*'));
        SubtractCommand = new RelayCommand(() => AppendOperator('-'));
        AddCommand = new RelayCommand(() => AppendOperator('+'));
        EqualsCommand = new RelayCommand(Equals);
        MemoryStoreCommand = new RelayCommand(MemoryStore);
        MemoryRecallCommand = new RelayCommand(MemoryRecall);
        MemoryAddCommand = new RelayCommand(MemoryAdd);
        MemorySubtractCommand = new RelayCommand(MemorySubtract);
        MemoryClearCommand = new RelayCommand(MemoryClear);
        SquareRootCommand = new RelayCommand(() => { _engine.SquareRoot(); UpdateDisplay(); });
        SquareCommand = new RelayCommand(() =>
        {
            if (!decimal.TryParse(DisplayText, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var v)) return;
            try
            {
                _engine.SetInput((v * v).ToString(System.Globalization.CultureInfo.InvariantCulture));
                UpdateDisplay();
            }
            catch (System.OverflowException)
            {
                ShowError("Overflow");
            }
        });
        ReciprocalCommand = new RelayCommand(() => { _engine.Reciprocal(); UpdateDisplay(); });
        PercentCommand = new RelayCommand(() => { _engine.ApplyPercent(); UpdateDisplay(); });
        BitwiseAndCommand = new RelayCommand(() => { _engine.BitwiseAnd(); UpdateDisplay(); });
        BitwiseOrCommand = new RelayCommand(() => { _engine.BitwiseOr(); UpdateDisplay(); });
        BitwiseXorCommand = new RelayCommand(() => { _engine.BitwiseXor(); UpdateDisplay(); });
        BitwiseNotCommand = new RelayCommand(() => { _engine.BitwiseNot(); UpdateDisplay(); });
        LeftShiftCommand = new RelayCommand(() => { _engine.LeftShift(); UpdateDisplay(); });
        RightShiftCommand = new RelayCommand(() => { _engine.RightShift(); UpdateDisplay(); });
        ToggleAngleModeCommand = new RelayCommand(() => { CurrentAngleMode = CurrentAngleMode == AngleMode.Degrees ? AngleMode.Radians : AngleMode.Degrees; });
        EvaluateExpressionCommand = new RelayCommand(EvaluateExpression);
        InsertTextCommand = new RelayCommand(p => ExpressionInput = (ExpressionInput ?? string.Empty) + (p?.ToString() ?? string.Empty));
        ToggleInverseCommand = new RelayCommand(() => IsInverseMode = !IsInverseMode);
        SetThemeCommand = new RelayCommand(p => SelectedTheme = (p?.ToString()) switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.System
        });
        SetBaseCommand = new RelayCommand(p =>
        {
            var s = p?.ToString();
            CurrentBaseMode = s switch
            {
                "BIN" => BaseMode.Binary,
                "OCT" => BaseMode.Octal,
                "HEX" => BaseMode.Hexadecimal,
                _ => BaseMode.Decimal
            };
        });
        DecimalCommand = new RelayCommand(AppendDecimalSeparator);
        DigitCommand = new RelayCommand(p => Digit(p?.ToString()));

        ToggleNavCommand = new RelayCommand(() => IsNavOpen = !IsNavOpen);
        SelectModeCommand = new RelayCommand(p => { if (System.Enum.TryParse<CalculatorMode>(p?.ToString(), true, out var m)) { SelectedMode = m; } IsNavOpen = false; });

        _selectedTheme = _themeService.Load();
        _themeService.Apply(_selectedTheme);
        UpdateDisplay();
    }

    private void Clear()
    {
        _engine.Clear();
        ExpressionInput = string.Empty;
        UpdateDisplay();
    }

    private void Backspace()
    {
        _engine.BackSpace();
        UpdateDisplay();
    }

    private void Negate()
    {
        _engine.Negate();
        UpdateDisplay();
    }

    private void AppendOperator(char op)
    {
        _engine.AppendOperator(op);
        UpdateDisplay();
    }

    private void Equals()
    {
        if (IsScientific && !string.IsNullOrWhiteSpace(ExpressionInput))
        {
            EvaluateExpression();
            return;
        }
        var result = _engine.Equals();
        UpdateDisplay();
    }

    private void EvaluateExpression()
    {
        try
        {
            var r = _engine.Evaluate(ExpressionInput ?? string.Empty);
            _engine.SetInput(r.ToString(System.Globalization.CultureInfo.InvariantCulture));
            UpdateDisplay();
            ExpressionInput = string.Empty;
        }
        catch (System.DivideByZeroException)
        {
            ShowError("Cannot divide by zero");
        }
        catch (System.OverflowException)
        {
            ShowError("Overflow");
        }
        catch
        {
            ShowError("Error");
        }
    }

    private void AppendDecimalSeparator()
    {
        _engine.AppendDecimalSeparator();
        UpdateDisplay();
    }

    private void Digit(string? digit)
    {
        if (digit is { Length: 1 } && char.IsLetter(digit[0]))
        {
            _engine.AppendHexDigit(digit[0]);
            UpdateDisplay();
            return;
        }
        if (int.TryParse(digit, out var value) && value is >= 0 and <= 9)
        {
            _engine.AppendDigit(value);
            UpdateDisplay();
        }
    }

    /// <summary>The stored value follows the calculator type: Programmer is integer-only and base-aware,
    /// every other mode is decimal.</summary>
    private void RetypeForMode()
    {
        // An error on screen stays until the user clears or types a new digit.
        if (IsError) return;

        if (IsProgrammer)
        {
            var value = _engine.CurrentValue;
            if (value > long.MaxValue || value < long.MinValue)
            {
                _engine.ReportOverflow();
                DisplayText = "Overflow";
                IsError = true;
                return;
            }
            _engine.TruncateToInteger();
        }
        else if (_currentBaseMode != BaseMode.Decimal)
        {
            // Base switching belongs to Programmer; coming back must show plain decimal digits.
            CurrentBaseMode = BaseMode.Decimal;
            return;
        }

        DisplayText = _engine.GetDisplayValue();
        Expression = _engine.Expression;
    }

    private void RaiseBaseValues()
    {
        OnPropertyChanged(nameof(HexValue));
        OnPropertyChanged(nameof(DecValue));
        OnPropertyChanged(nameof(OctValue));
        OnPropertyChanged(nameof(BinValue));
    }

    private void UpdateDisplay()
    {
        DisplayText = _engine.GetDisplayValue();
        Expression = _engine.Expression;
        // Engine-level failures (divide by zero, overflow, shifts) must paint the error surface too.
        IsError = _engine.HasError;
        RaiseBaseValues();
    }

    private void ShowError(string message)
    {
        _engine.Clear();
        DisplayText = message;
        Expression = string.Empty;
        IsError = true;
        RaiseBaseValues();
    }

    private void MemoryStore()
    {
        if (decimal.TryParse(DisplayText, System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            _memory = value;
            OnPropertyChanged(nameof(HasMemory));
        }
    }

    private void MemoryRecall()
    {
        _engine.SetInput(_memory.ToString(System.Globalization.CultureInfo.InvariantCulture));
        UpdateDisplay();
        OnPropertyChanged(nameof(HasMemory));
    }

    private void MemoryAdd()
    {
        if (decimal.TryParse(DisplayText, System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            try
            {
                _memory += value;
                OnPropertyChanged(nameof(HasMemory));
            }
            catch (System.OverflowException)
            {
                ShowError("Overflow");
            }
        }
    }

    private void MemorySubtract()
    {
        if (decimal.TryParse(DisplayText, System.Globalization.CultureInfo.InvariantCulture, out var value))
        {
            try
            {
                _memory -= value;
                OnPropertyChanged(nameof(HasMemory));
            }
            catch (System.OverflowException)
            {
                ShowError("Overflow");
            }
        }
    }

    private void MemoryClear()
    {
        _memory = 0m;
        OnPropertyChanged(nameof(HasMemory));
    }

    public bool HandleKey(Key key)
    {
        if (key is >= Key.D0 and <= Key.D9)
        {
            Digit(((int)key - (int)Key.D0).ToString());
            return true;
        }

        if (key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            Digit(((int)key - (int)Key.NumPad0).ToString());
            return true;
        }

        switch (key)
        {
            case Key.Add or Key.OemPlus:
                AppendOperator('+');
                return true;
            case Key.Subtract or Key.OemMinus:
                AppendOperator('-');
                return true;
            case Key.Multiply:
                AppendOperator('*');
                return true;
            case Key.Divide or Key.Oem2:
                AppendOperator('/');
                return true;
            case Key.Decimal or Key.OemPeriod:
                AppendDecimalSeparator();
                return true;
            case Key.Enter:
                Equals();
                return true;
            case Key.Escape:
                Clear();
                return true;
            case Key.Back:
                Backspace();
                return true;
            case Key.F9 or Key.N:
                Negate();
                return true;
            default:
                return false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
