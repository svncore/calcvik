using System;
using System.Globalization;

namespace Calcvik.Models;

public enum BaseMode
{
    Decimal,
    Binary,
    Octal,
    Hexadecimal
}

public class CalculatorEngine
{
    private const int MaxLen = 16;
    private string _input = "0";
    private decimal _acc;
    private bool _hasAcc, _reset, _error;
    private char _op;
    private AngleMode _angleMode = AngleMode.Degrees;
    private BaseMode _baseMode = BaseMode.Decimal;

    public string Expression { get; private set; } = "";

    public void AppendDigit(int d)
    {
        if (_error) Clear();
        if (d is < 0 or > 9) return;
        // Digits 0-9 are valid in every base; letters A-F for HEX go through AppendHexDigit.
        if (_baseMode == BaseMode.Binary && d > 1) return;
        if (_baseMode == BaseMode.Octal && d > 7) return;
        if (_reset) { _input = "0"; _reset = false; }

        if (_baseMode != BaseMode.Decimal)
        {
            // Non-decimal input: keep _input as the decimal value, the base conversion happens on display.
            var radix = _baseMode == BaseMode.Binary ? 2 : _baseMode == BaseMode.Octal ? 8 : 16;
            if (!decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out var current)) current = 0m;
            var next = current * radix + d;
            if (next > long.MaxValue || next < long.MinValue) return;
            _input = next.ToString(CultureInfo.InvariantCulture);
            return;
        }

        if (_input == "0") _input = d.ToString(CultureInfo.InvariantCulture);
        else if (_input == "-0") _input = "-" + d.ToString(CultureInfo.InvariantCulture);
        else if (_input.Replace("-", "").Replace(".", "").Length < MaxLen)
            _input += d.ToString(CultureInfo.InvariantCulture);
    }

    public void AppendHexDigit(char c)
    {
        if (_error) Clear();
        if (_baseMode != BaseMode.Hexadecimal) return;
        c = char.ToUpperInvariant(c);
        if (c is < 'A' or > 'F') return;
        if (_reset) { _input = "0"; _reset = false; }
        var digit = char.ToUpperInvariant(c) - 'A' + 10;
        if (!decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out var current)) current = 0m;
        var next = current * 16 + digit;
        if (next > long.MaxValue || next < long.MinValue) return;
        _input = next.ToString(CultureInfo.InvariantCulture);
    }

    public void AppendDecimalSeparator()
    {
        if (_error) Clear();
        if (_baseMode != BaseMode.Decimal) return;
        if (_reset) { _input = "0"; _reset = false; }
        if (!_input.Contains('.')) _input += ".";
    }

    public void AppendOperator(char op)
    {
        if (_error) Clear();
        if (op is not ('+' or '-' or '*' or '/')) return;

        if (_hasAcc && !_reset && _op != '\0')
        {
            if (!ComputePending()) return;
        }
        else if (!_hasAcc)
        {
            if (!decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out _acc))
            {
                SetError("Error");
                return;
            }
            _hasAcc = true;
        }

        _op = op;
        _reset = true;
        Expression = $"{FmtBase(_acc)} {OpText(op)}";
    }

    public string Equals()
    {
        if (_error) { Clear(); return _input; }
        if (!_hasAcc || _op == '\0') return _input;

        if (!decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out var right))
        {
            SetError("Error");
            return _input;
        }
        if (!TryCompute(_acc, right, _op, out var result, out var error))
        {
            SetError(error);
            return _input;
        }

        Expression = $"{FmtBase(_acc)} {OpText(_op)} {FmtBase(right)} =";
        _acc = (decimal)result;
        _input = Fmt(result);
        _op = '\0';
        _hasAcc = false;
        _reset = true;
        return _input;
    }

    public void SetInput(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        _input = text;
        _reset = false;
    }
    public void Clear()
    {
        _input = "0";
        _acc = 0;
        _hasAcc = false;
        _op = '\0';
        _reset = false;
        _error = false;
        Expression = "";
    }

    public void BackSpace()
    {
        if (_error) { Clear(); return; }
        if (_reset) return;
        if (_input.Length <= 1) { _input = "0"; return; }
        _input = _input[..^1];
        if (_input is "" or "-" or "-0") _input = "0";
    }

    public void Negate()
    {
        if (_error) { Clear(); return; }
        if (_input == "0") return;
        _input = _input.StartsWith('-') ? _input[1..] : "-" + _input;
    }

    public override string ToString() => _input;

    public void ApplyPercent()
    {
        if (_error) { Clear(); return; }
        if (!decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            SetError("Error");
            return;
        }
        var result = value / 100m;
        _input = Fmt(result);
        _acc = (decimal)result;
        _reset = true;
    }

    public void SquareRoot()
    {
        if (_error) { Clear(); return; }
        if (!decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            SetError("Error");
            return;
        }
        if (value < 0)
        {
            SetError("Cannot take square root of negative");
            return;
        }
        var result = Math.Sqrt((double)value);
        _input = Fmt((decimal)result);
        _acc = (decimal)result;
        _reset = true;
    }

    public void Reciprocal()
    {
        if (_error) { Clear(); return; }
        if (!decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            SetError("Error");
            return;
        }
        if (value == 0)
        {
            SetError("Cannot divide by zero");
            return;
        }
        var result = 1m / value;
        _input = Fmt(result);
        _acc = (decimal)result;
        _reset = true;
    }

    // Programmer mode helpers

    public string GetInputInBase()
    {
        if (_error) return _input;
        if (decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
        {
            // Base views are 64-bit: a value that does not fit cannot be shown honestly.
            if (val > long.MaxValue || val < long.MinValue) return "Overflow";
            return ConvertToBase(val);
        }
        return _input;
    }

    public string GetDisplayValue()
    {
        if (_error) return _input;
        // Decimal mode must show the real (fractional) value; the base conversion
        // truncates to long and would turn 2.4 into 2.
        if (_baseMode == BaseMode.Decimal) return _input;
        return GetInputInBase();
    }

    public void SetBaseMode(BaseMode mode) => _baseMode = mode;

    /// <summary>Formats the truncated value in the current base. Negative values are rendered as their 64-bit two's-complement pattern.</summary>
    public string ConvertToBase(decimal value) => FormatValue(value, _baseMode);

    /// <summary>Formats a value in an explicit base, independent of the active base mode.</summary>
    public string FormatValue(decimal value, BaseMode mode)
    {
        long v = ToLongChecked(value);
        return mode switch
        {
            BaseMode.Decimal => v.ToString(CultureInfo.InvariantCulture),
            BaseMode.Binary => Convert.ToString(unchecked((long)(ulong)v), 2),
            BaseMode.Octal => Convert.ToString(unchecked((long)(ulong)v), 8),
            BaseMode.Hexadecimal => v.ToString("X", CultureInfo.InvariantCulture),
            _ => v.ToString(CultureInfo.InvariantCulture),
        };
    }

    /// <summary>Truncates the entered value and the accumulator towards zero (used when entering Programmer mode).</summary>
    public void TruncateToInteger()
    {
        if (_error) return;
        if (decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            _input = Fmt(decimal.Truncate(value));
        _acc = decimal.Truncate(_acc);
    }

    /// <summary>Numeric value currently entered (0 when the input is not numeric).</summary>
    public decimal CurrentValue =>
        decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    /// <summary>True while the engine holds an error state instead of a value.</summary>
    public bool HasError => _error;

    /// <summary>Converts a decimal to long, clamping to long.MinValue/long.MaxValue instead of overflowing.</summary>
    private static long ToLongChecked(decimal d)
    {
        if (d >= long.MaxValue) return long.MaxValue;
        if (d <= long.MinValue) return long.MinValue;
        return (long)decimal.Truncate(d);
    }

    public void BitwiseAnd()
    {
        AppendBitwise('&');
    }

    public void BitwiseOr()
    {
        AppendBitwise('|');
    }

    public void BitwiseXor()
    {
        AppendBitwise('^');
    }

    public void BitwiseNot()
    {
        if (_error) Clear();
        if (!decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out var right)) { SetError("Error"); return; }
        long b = ToLongChecked(right);
        long result = ~b;
        _input = Fmt(result);
        _acc = (decimal)result;
        _reset = true;
        _op = '\0';
    }

    public void LeftShift()
    {
        AppendBitwise('<');
    }

    public void RightShift()
    {
        AppendBitwise('>');
    }

    private void AppendBitwise(char op)
    {
        if (_error) Clear();

        if (_hasAcc && !_reset && _op != '\0')
        {
            if (!ComputePending()) return;
        }
        else if (!_hasAcc || _reset)
        {
            if (!decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out var left))
            {
                SetError("Error");
                return;
            }
            _acc = left;
            _hasAcc = true;
        }

        _op = op;
        _reset = true;
        Expression = $"{FmtBase(_acc)} {OpText(op)}";
    }

    private static string OpText(char op) => op switch
    {
        '&' => "AND",
        '|' => "OR",
        '^' => "XOR",
        '<' => "<<",
        '>' => ">>",
        _ => op.ToString()
    };

    private bool ComputePending()
    {
        if (!decimal.TryParse(_input, NumberStyles.Number, CultureInfo.InvariantCulture, out var right))
        {
            SetError("Error");
            return false;
        }
        var left = _acc;
        var op = _op;
        if (!TryCompute(left, right, op, out var result, out var error))
        {
            SetError(error);
            return false;
        }
        Expression = $"{Fmt(left)} {op} {Fmt(right)} =";
        _acc = (decimal)result;
        _input = Fmt(result);
        _op = '\0';
        _reset = true;
        return true;
    }

    private static bool TryCompute(decimal a, decimal b, char op, out decimal result, out string error)
    {
        result = 0;
        error = "";
        try
        {
            switch (op)
            {
                case '+': result = a + b; return true;
                case '-': result = a - b; return true;
                case '*': result = a * b; return true;
                case '/':
                    if (b == 0) { error = "Cannot divide by zero"; return false; }
                    result = a / b;
                    return true;
                case '&': result = (decimal)(ToLongChecked(a) & ToLongChecked(b)); return true;
                case '|': result = (decimal)(ToLongChecked(a) | ToLongChecked(b)); return true;
                case '^': result = (decimal)(ToLongChecked(a) ^ ToLongChecked(b)); return true;
                case '<':
                    {
                        if (b < 0 || b >= 64) { error = "Shift out of range"; return false; }
                        result = (decimal)(ToLongChecked(a) << (int)decimal.Truncate(b));
                        return true;
                    }
                case '>':
                    {
                        if (b < 0 || b >= 64) { error = "Shift out of range"; return false; }
                        result = (decimal)(ToLongChecked(a) >> (int)decimal.Truncate(b));
                        return true;
                    }
                default: error = "Error"; return false;
            }
        }
        catch (OverflowException)
        {
            error = "Overflow";
            return false;
        }
    }

    /// <summary>Puts the engine into an error state from the view model (e.g. a value too large for a 64-bit base view).</summary>
    public void ReportOverflow() => SetError("Overflow");

    private void SetError(string message)
    {
        _error = true;
        _input = message;
        Expression = "";
        _hasAcc = false;
        _op = '\0';
        _reset = true;
    }

    public void SetAngleMode(AngleMode mode)
    {
        _angleMode = mode;
    }

    public decimal Evaluate(string expr)
    {
        var tokenizer = new Tokenizer(expr);
        var parser = new Parser(tokenizer, _angleMode);
        return parser.ParseExpression();
    }

    private static string Fmt(decimal v) => v.ToString(CultureInfo.InvariantCulture);

    /// <summary>Formats an operand for the expression caption in the active base.</summary>
    private string FmtBase(decimal v) => _baseMode == BaseMode.Decimal ? Fmt(v) : ConvertToBase(v);
}
