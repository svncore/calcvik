using System;
using System.Globalization;

namespace Calcvik.Models
{
    public sealed class Tokenizer
    {
        private readonly string _input;
        private int _pos;
        private readonly int _len;

        public Tokenizer(string input)
        {
            _input = input ?? string.Empty;
            _len = _input.Length;
            _pos = 0;
        }

        public Token NextToken()
        {
            SkipWhite();
            if (_pos >= _len) return new Token(TokenType.End);
            char c = _input[_pos];
            switch (c)
            {
                case '+': _pos++; return new Token(TokenType.Plus);
                case '-': _pos++; return new Token(TokenType.Minus);
                case '*': _pos++; return new Token(TokenType.Multiply);
                case '/': _pos++; return new Token(TokenType.Divide);
                case '^': _pos++; return new Token(TokenType.Power);
                case '%': _pos++; return new Token(TokenType.Percent);
                case '!': _pos++; return new Token(TokenType.Factorial);
                case '(': _pos++; return new Token(TokenType.LParen);
                case ')': _pos++; return new Token(TokenType.RParen);
                case ',': _pos++; return new Token(TokenType.Comma);
                default:
                    if (char.IsLetter(c))
                    {
                        int start = _pos;
                        while (_pos < _len && char.IsLetterOrDigit(_input[_pos])) _pos++;
                        string name = _input.Substring(start, _pos - start);
                        return new Token(TokenType.Identifier, name);
                    }
                    if (char.IsDigit(c) || c == '.')
                    {
                        int start = _pos;
                        bool dot = false;
                        while (_pos < _len && (char.IsDigit(_input[_pos]) || (_input[_pos] == '.' && !dot)))
                        {
                            if (_input[_pos] == '.') dot = true;
                            _pos++;
                        }
                        string num = _input.Substring(start, _pos - start);
                        return new Token(TokenType.Number,
                            decimal.Parse(num, NumberStyles.Number, CultureInfo.InvariantCulture));
                    }
                    throw new FormatException($"Unexpected character '{c}'");
            }
        }

        private void SkipWhite()
        {
            while (_pos < _len && char.IsWhiteSpace(_input[_pos])) _pos++;
        }
    }

    public enum TokenType
    {
        Number,
        Identifier,
        Plus,
        Minus,
        Multiply,
        Divide,
        Power,
        Percent,
        Factorial,
        LParen,
        RParen,
        Comma,
        End
    }

    public sealed class Token
    {
        public TokenType Type { get; }
        public decimal Number { get; }
        public string Identifier { get; } = string.Empty;

        public Token(TokenType type)
        {
            Type = type;
        }

        public Token(TokenType type, decimal number)
        {
            Type = type;
            Number = number;
        }

        public Token(TokenType type, string identifier)
        {
            Type = type;
            Identifier = identifier;
        }
    }
}
