using System;
using System.Globalization;

namespace Calcvik.Models
{
    public sealed class Parser
    {
        private readonly Tokenizer _tokenizer;
        private readonly AngleMode _angleMode;
        private Token _current;

        public Parser(Tokenizer tokenizer, AngleMode angleMode)
        {
            _tokenizer = tokenizer;
            _angleMode = angleMode;
            _current = _tokenizer.NextToken();
        }

        public decimal ParseExpression()
        {
            var value = ParseAddSub();
            if (_current.Type != TokenType.End)
                throw new FormatException("Unexpected token");
            return value;
        }

        private decimal ParseAddSub()
        {
            var left = ParseMulDiv();
            while (_current.Type == TokenType.Plus || _current.Type == TokenType.Minus)
            {
                var op = _current.Type;
                Consume(op);
                var right = ParseMulDiv();
                left = ApplyOp(left, right, op);
            }
            return left;
        }

        private decimal ParseMulDiv()
        {
            var left = ParseUnary();
            while (_current.Type == TokenType.Multiply || _current.Type == TokenType.Divide)
            {
                var op = _current.Type;
                Consume(op);
                var right = ParseUnary();
                left = ApplyOp(left, right, op);
            }
            return left;
        }

        private decimal ParsePower()
        {
            var left = ParsePrimary();
            if (_current.Type == TokenType.Power)
            {
                Consume(TokenType.Power);
                var right = ParseUnary();
                if (left == 0m && right == 0m) { left = 1m; }
                else if (left < 0m && right != decimal.Truncate(right)) { left = 0m; }
                else { left = SafeDecimal(Math.Pow((double)left, (double)right)); }
            }
            return left;
        }

        private decimal ParseUnary()
        {
            if (_current.Type == TokenType.Plus)
            {
                Consume(TokenType.Plus);
                return ParseUnary();
            }
            if (_current.Type == TokenType.Minus)
            {
                Consume(TokenType.Minus);
                return -ParseUnary();
            }
            return ParsePower();
        }

        private decimal ParsePrimary()
        {
            decimal value;
            if (_current.Type == TokenType.Number)
            {
                value = _current.Number;
                Consume(TokenType.Number);
            }
            else if (_current.Type == TokenType.Identifier)
            {
                var name = _current.Identifier;
                Consume(TokenType.Identifier);
                if (_current.Type == TokenType.LParen)
                {
                    Consume(TokenType.LParen);
                    var arg = ParseAddSub();
                    Consume(TokenType.RParen);
                    value = EvaluateFunction(name, arg);
                }
                else
                {
                    value = EvaluateConstant(name);
                }
            }
            else if (_current.Type == TokenType.LParen)
            {
                Consume(TokenType.LParen);
                value = ParseAddSub();
                Consume(TokenType.RParen);
            }
            else
            {
                throw new FormatException("Unexpected token");
            }

            // Handle postfix operators
            while (_current.Type == TokenType.Factorial || _current.Type == TokenType.Percent)
            {
                if (_current.Type == TokenType.Factorial)
                {
                    Consume(TokenType.Factorial);
                    value = Factorial(value);
                }
                else if (_current.Type == TokenType.Percent)
                {
                    Consume(TokenType.Percent);
                    value = value / 100m;
                }
            }
            return value;
        }

        private decimal EvaluateFunction(string name, decimal arg)
        {
            switch (name.ToLowerInvariant())
            {
                case "sin": return SafeDecimal(Math.Sin(ToRadians(arg)));
                case "cos": return SafeDecimal(Math.Cos(ToRadians(arg)));
                case "tan":
                    var radians = ToRadians(arg);
                    if (Math.Abs(Math.Cos(radians)) < 1e-12) throw new FormatException("Tangent is undefined");
                    return SafeDecimal(Math.Tan(radians));
                case "asin":
                    if (arg is < -1m or > 1m) throw new FormatException("Argument must be between -1 and 1");
                    return SafeDegreesFrom(Math.Asin((double)arg));
                case "acos":
                    if (arg is < -1m or > 1m) throw new FormatException("Argument must be between -1 and 1");
                    return SafeDegreesFrom(Math.Acos((double)arg));
                case "atan": return SafeDegreesFrom(Math.Atan((double)arg));
                case "log": return arg <= 0 ? throw new FormatException("Logarithm is undefined") : SafeDecimal(Math.Log10((double)arg));
                case "ln": return arg <= 0 ? throw new FormatException("Logarithm is undefined") : SafeDecimal(Math.Log((double)arg));
                case "sqrt": return arg < 0 ? throw new FormatException("Square root is undefined") : SafeDecimal(Math.Sqrt((double)arg));
                default: throw new FormatException($"Unknown function {name}");
            }
        }

        private decimal SafeDecimal(double d)
        {
            // Infinite or out-of-range results are reported, never silently turned into 0 or a clamped max.
            if (double.IsNaN(d)) throw new FormatException("Result is not a number");
            if (double.IsInfinity(d) || d >= (double)decimal.MaxValue || d <= (double)decimal.MinValue)
                throw new OverflowException("Result is too large");
            return (decimal)d;
        }
        private decimal SafeDegreesFrom(double radians) => ToDegrees(radians);

        private decimal EvaluateConstant(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "pi": return (decimal)Math.PI;
                case "e": return (decimal)Math.E;
                default: throw new FormatException($"Unknown constant {name}");
            }
        }

        private decimal Factorial(decimal val)
        {
            if (val < 0 || val != decimal.Truncate(val) || val > 20)
                throw new FormatException("Factorial requires an integer from 0 to 20");
            decimal result = 1m;
            for (int i = 1; i <= (int)val; i++) result *= i;
            return result;
        }

        private double ToRadians(decimal deg)
        {
            if (_angleMode == AngleMode.Degrees) return (double)deg * Math.PI / 180.0;
            return (double)deg;
        }

        private decimal ToDegrees(double rad)
        {
            return SafeDecimal(_angleMode == AngleMode.Degrees ? rad * 180.0 / Math.PI : rad);
        }

        private void Consume(TokenType expected)
        {
            if (_current.Type != expected)
                throw new FormatException($"Expected {expected}");
            _current = _tokenizer.NextToken();
        }

        private decimal ApplyOp(decimal left, decimal right, TokenType op)
        {
            return op switch
            {
                TokenType.Plus => left + right,
                TokenType.Minus => left - right,
                TokenType.Multiply => left * right,
                TokenType.Divide => right == 0 ? throw new DivideByZeroException() : left / right,
                _ => 0m,
            };
        }
    }
}
