using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class MathExpressionEvaluator
{
    public static bool TryEvaluate(string expression, float x, out float result, out string error)
    {
        result = 0f;

        if (!TryCompileInternal(expression, out var compiled, out error))
        {
            return false;
        }

        try
        {
            result = compiled(x);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static Func<float, float> Compile(string expression, out string error)
    {
        if (!TryCompileInternal(expression, out var compiled, out error))
        {
            return null;
        }

        return compiled;
    }

    public static float Derivative(Func<float, float> f, float x, float h = 0.001f)
    {
        if (f == null)
        {
            throw new ArgumentNullException(nameof(f));
        }

        if (Mathf.Approximately(h, 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(h), "h must be non-zero.");
        }

        return (f(x + h) - f(x - h)) / (2f * h);
    }

    private static bool TryCompileInternal(string expression, out Func<float, float> compiled, out string error)
    {
        compiled = null;
        error = null;

        if (string.IsNullOrWhiteSpace(expression))
        {
            error = "Expression is empty.";
            return false;
        }

        try
        {
            var parser = new Parser(expression);
            var root = parser.ParseExpression();
            parser.EnsureEndOfInput();
            compiled = root.Evaluate;
            return true;
        }
        catch (ParseException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private sealed class ParseException : Exception
    {
        public ParseException(string message) : base(message)
        {
        }
    }

    private sealed class Node
    {
        public Node(Func<float, float> evaluate)
        {
            Evaluate = evaluate;
        }

        public Func<float, float> Evaluate { get; }
    }

    private sealed class Parser
    {
        private static readonly Dictionary<string, int> FunctionArgCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "pow", 2 },
            { "mathf.pow", 2 },
            { "sin", 1 },
            { "mathf.sin", 1 },
            { "cos", 1 },
            { "mathf.cos", 1 },
            { "sqrt", 1 },
            { "mathf.sqrt", 1 },
            { "abs", 1 },
            { "mathf.abs", 1 },
            { "log", 1 },
            { "mathf.log", 1 },
            { "exp", 1 },
            { "mathf.exp", 1 },
            { "min", 2 },
            { "mathf.min", 2 },
            { "max", 2 },
            { "mathf.max", 2 }
        };

        private readonly string _text;
        private int _position;

        public Parser(string text)
        {
            _text = text;
        }

        public Node ParseExpression()
        {
            var node = ParseTerm();

            while (true)
            {
                SkipWhitespace();

                if (TryConsume('+'))
                {
                    var left = node;
                    var right = ParseTerm();
                    node = new Node(x => left.Evaluate(x) + right.Evaluate(x));
                    continue;
                }

                if (TryConsume('-'))
                {
                    var left = node;
                    var right = ParseTerm();
                    node = new Node(x => left.Evaluate(x) - right.Evaluate(x));
                    continue;
                }

                return node;
            }
        }

        public void EnsureEndOfInput()
        {
            SkipWhitespace();

            if (!IsAtEnd)
            {
                throw Error($"Unexpected character '{CurrentChar}'.");
            }
        }

        private Node ParseTerm()
        {
            var node = ParseUnary();

            while (true)
            {
                SkipWhitespace();

                if (TryConsume('*'))
                {
                    var left = node;
                    var right = ParseUnary();
                    node = new Node(x => left.Evaluate(x) * right.Evaluate(x));
                    continue;
                }

                if (TryConsume('/'))
                {
                    var left = node;
                    var right = ParseUnary();
                    node = new Node(x => left.Evaluate(x) / right.Evaluate(x));
                    continue;
                }

                return node;
            }
        }

        private Node ParseUnary()
        {
            SkipWhitespace();

            if (TryConsume('+'))
            {
                return ParseUnary();
            }

            if (TryConsume('-'))
            {
                var operand = ParseUnary();
                return new Node(x => -operand.Evaluate(x));
            }

            return ParsePrimary();
        }

        private Node ParsePrimary()
        {
            SkipWhitespace();

            if (TryConsume('('))
            {
                var node = ParseExpression();
                SkipWhitespace();

                if (!TryConsume(')'))
                {
                    throw Error("Missing closing parenthesis.");
                }

                return node;
            }

            if (IsNumberStart())
            {
                return ParseNumber();
            }

            if (IsIdentifierStart(CurrentChar))
            {
                return ParseIdentifierOrFunction();
            }

            if (IsAtEnd)
            {
                throw Error("Unexpected end of expression.");
            }

            throw Error($"Unexpected character '{CurrentChar}'.");
        }

        private Node ParseNumber()
        {
            int start = _position;

            while (!IsAtEnd && (char.IsDigit(CurrentChar) || CurrentChar == '.'))
            {
                _position++;
            }

            string token = _text.Substring(start, _position - start);
            if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                throw Error($"Invalid number '{token}'.");
            }

            return new Node(_ => value);
        }

        private Node ParseIdentifierOrFunction()
        {
            string identifier = ParseIdentifier();
            SkipWhitespace();

            if (TryConsume('('))
            {
                return ParseFunctionCall(identifier);
            }

            if (string.Equals(identifier, "x", StringComparison.OrdinalIgnoreCase))
            {
                return new Node(x => x);
            }

            if (string.Equals(identifier, "Mathf.PI", StringComparison.OrdinalIgnoreCase) || string.Equals(identifier, "PI", StringComparison.OrdinalIgnoreCase))
            {
                return new Node(_ => Mathf.PI);
            }

            throw Error($"Unknown identifier '{identifier}'.");
        }

        private Node ParseFunctionCall(string functionName)
        {
            if (!FunctionArgCounts.TryGetValue(functionName, out int expectedArgCount))
            {
                throw Error($"Unknown function '{functionName}'.");
            }

            var arguments = new List<Node>();
            SkipWhitespace();

            if (!TryConsume(')'))
            {
                while (true)
                {
                    arguments.Add(ParseExpression());
                    SkipWhitespace();

                    if (TryConsume(')'))
                    {
                        break;
                    }

                    if (!TryConsume(','))
                    {
                        throw Error("Expected ',' or ')' in function call.");
                    }
                }
            }

            if (arguments.Count != expectedArgCount)
            {
                throw Error($"Function '{functionName}' expects {expectedArgCount} argument(s), got {arguments.Count}.");
            }

            return new Node(x => EvaluateFunction(functionName, arguments, x));
        }

        private static float EvaluateFunction(string functionName, List<Node> arguments, float x)
        {
            switch (functionName.ToLowerInvariant())
            {
                case "pow":
                case "mathf.pow":
                    return Mathf.Pow(arguments[0].Evaluate(x), arguments[1].Evaluate(x));
                case "sin":
                case "mathf.sin":
                    return Mathf.Sin(arguments[0].Evaluate(x));
                case "cos":
                case "mathf.cos":
                    return Mathf.Cos(arguments[0].Evaluate(x));
                case "sqrt":
                case "mathf.sqrt":
                    return Mathf.Sqrt(arguments[0].Evaluate(x));
                case "abs":
                case "mathf.abs":
                    return Mathf.Abs(arguments[0].Evaluate(x));
                case "log":
                case "mathf.log":
                    return Mathf.Log(arguments[0].Evaluate(x));
                case "exp":
                case "mathf.exp":
                    return Mathf.Exp(arguments[0].Evaluate(x));
                case "min":
                case "mathf.min":
                    return Mathf.Min(arguments[0].Evaluate(x), arguments[1].Evaluate(x));
                case "max":
                case "mathf.max":
                    return Mathf.Max(arguments[0].Evaluate(x), arguments[1].Evaluate(x));
                default:
                    throw new InvalidOperationException($"Unsupported function '{functionName}'.");
            }
        }

        private string ParseIdentifier()
        {
            int start = _position;

            while (!IsAtEnd && (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_' || CurrentChar == '.'))
            {
                _position++;
            }

            return _text.Substring(start, _position - start);
        }

        private bool IsNumberStart()
        {
            return !IsAtEnd && (char.IsDigit(CurrentChar) || CurrentChar == '.');
        }

        private void SkipWhitespace()
        {
            while (!IsAtEnd && char.IsWhiteSpace(CurrentChar))
            {
                _position++;
            }
        }

        private bool TryConsume(char expected)
        {
            if (!IsAtEnd && CurrentChar == expected)
            {
                _position++;
                return true;
            }

            return false;
        }

        private ParseException Error(string message)
        {
            return new ParseException($"{message} At position {_position}.");
        }

        private static bool IsIdentifierStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private bool IsAtEnd => _position >= _text.Length;

        private char CurrentChar => IsAtEnd ? '\0' : _text[_position];
    }
}
