using OSCControl.Compiler.Diagnostics;
using OSCControl.Compiler.Text;

namespace OSCControl.Compiler.Lexing;

public sealed class Tokenizer
{
    private static readonly Dictionary<string, TokenKind> Keywords = new(StringComparer.Ordinal)
    {
        ["endpoint"] = TokenKind.KeywordEndpoint,
        ["state"] = TokenKind.KeywordState,
        ["func"] = TokenKind.KeywordFunc,
        ["on"] = TokenKind.KeywordOn,
        ["receive"] = TokenKind.KeywordReceive,
        ["when"] = TokenKind.KeywordWhen,
        ["timer"] = TokenKind.KeywordTimer,
        ["startup"] = TokenKind.KeywordStartup,
        ["send"] = TokenKind.KeywordSend,
        ["address"] = TokenKind.KeywordAddress,
        ["args"] = TokenKind.KeywordArgs,
        ["body"] = TokenKind.KeywordBody,
        ["headers"] = TokenKind.KeywordHeaders,
        ["set"] = TokenKind.KeywordSet,
        ["store"] = TokenKind.KeywordStore,
        ["log"] = TokenKind.KeywordLog,
        ["call"] = TokenKind.KeywordCall,
        ["stop"] = TokenKind.KeywordStop,
        ["let"] = TokenKind.KeywordLet,
        ["if"] = TokenKind.KeywordIf,
        ["else"] = TokenKind.KeywordElse,
        ["for"] = TokenKind.KeywordFor,
        ["in"] = TokenKind.KeywordIn,
        ["while"] = TokenKind.KeywordWhile,
        ["break"] = TokenKind.KeywordBreak,
        ["continue"] = TokenKind.KeywordContinue,
        ["trace"] = TokenKind.KeywordTrace,
        ["debug"] = TokenKind.KeywordDebug,
        ["info"] = TokenKind.KeywordInfo,
        ["warn"] = TokenKind.KeywordWarn,
        ["error"] = TokenKind.KeywordError,
        ["and"] = TokenKind.KeywordAnd,
        ["or"] = TokenKind.KeywordOr,
        ["not"] = TokenKind.KeywordNot,
        ["true"] = TokenKind.KeywordTrue,
        ["false"] = TokenKind.KeywordFalse,
        ["null"] = TokenKind.KeywordNull,
        ["mode"] = TokenKind.KeywordMode,
        ["codec"] = TokenKind.KeywordCodec,
        ["host"] = TokenKind.KeywordHost,
        ["port"] = TokenKind.KeywordPort,
        ["path"] = TokenKind.KeywordPath,
        ["input"] = TokenKind.KeywordInput,
        ["output"] = TokenKind.KeywordOutput,
        ["duplex"] = TokenKind.KeywordDuplex,
        ["osc"] = TokenKind.KeywordOsc,
        ["json"] = TokenKind.KeywordJson,
        ["text"] = TokenKind.KeywordText,
        ["bytes"] = TokenKind.KeywordBytes,
        ["osc.udp"] = TokenKind.KeywordOscUdp,
        ["ws.client"] = TokenKind.KeywordWsClient,
        ["ws.server"] = TokenKind.KeywordWsServer
    };

    private readonly SourceText _source;
    private readonly DiagnosticBag _diagnostics = new();
    private int _position;

    public Tokenizer(SourceText source)
    {
        _source = source;
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.Items;

    public IReadOnlyList<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (!IsAtEnd)
        {
            SkipTrivia();
            if (IsAtEnd)
            {
                break;
            }

            tokens.Add(ReadToken());
        }

        tokens.Add(CreateToken(TokenKind.EndOfFile, string.Empty, null, _position, _position));
        return tokens;
    }

    private bool IsAtEnd => _position >= _source.Length;
    private char Current => _source[_position];
    private char Peek(int offset)
    {
        var index = _position + offset;
        return index >= _source.Length ? '\0' : _source[index];
    }

    private void SkipTrivia()
    {
        while (!IsAtEnd)
        {
            if (char.IsWhiteSpace(Current))
            {
                _position++;
                continue;
            }

            if (Current == '#')
            {
                SkipLineComment();
                continue;
            }

            if (Current == '/' && Peek(1) == '/')
            {
                SkipLineComment();
                continue;
            }

            break;
        }
    }

    private void SkipLineComment()
    {
        while (!IsAtEnd && Current is not '\r' and not '\n')
        {
            _position++;
        }
    }

    private Token ReadToken()
    {
        var start = _position;

        if (Current == '[' && Peek(1) == '[')
        {
            _position += 2;
            return CreateToken(TokenKind.LeftDoubleBracket, "[[", null, start, _position);
        }

        if (Current == ']' && Peek(1) == ']')
        {
            _position += 2;
            return CreateToken(TokenKind.RightDoubleBracket, "]]", null, start, _position);
        }

        if (char.IsLetter(Current) || Current == '_')
        {
            return ReadIdentifierOrKeyword();
        }

        if (char.IsDigit(Current))
        {
            return ReadNumber();
        }

        if (Current == '"')
        {
            return ReadString();
        }

        _position++;
        var ch = _source[start];

        return ch switch
        {
            '(' => CreateToken(TokenKind.LeftParen, "(", null, start, _position),
            ')' => CreateToken(TokenKind.RightParen, ")", null, start, _position),
            '[' => CreateToken(TokenKind.LeftBracket, "[", null, start, _position),
            ']' => CreateToken(TokenKind.RightBracket, "]", null, start, _position),
            '{' => CreateToken(TokenKind.LeftBrace, "{", null, start, _position),
            '}' => CreateToken(TokenKind.RightBrace, "}", null, start, _position),
            ':' => CreateToken(TokenKind.Colon, ":", null, start, _position),
            ',' => CreateToken(TokenKind.Comma, ",", null, start, _position),
            '.' => CreateToken(TokenKind.Dot, ".", null, start, _position),
            '+' => CreateToken(TokenKind.Plus, "+", null, start, _position),
            '-' => CreateToken(TokenKind.Minus, "-", null, start, _position),
            '*' => CreateToken(TokenKind.Star, "*", null, start, _position),
            '/' => CreateToken(TokenKind.Slash, "/", null, start, _position),
            '%' => CreateToken(TokenKind.Percent, "%", null, start, _position),
            '=' when Current == '=' => CreateToken(TokenKind.EqualEqual, "==", null, start, ++_position),
            '=' => CreateToken(TokenKind.Equal, "=", null, start, _position),
            '!' when Current == '=' => CreateToken(TokenKind.BangEqual, "!=", null, start, ++_position),
            '<' when Current == '=' => CreateToken(TokenKind.LessEqual, "<=", null, start, ++_position),
            '<' => CreateToken(TokenKind.Less, "<", null, start, _position),
            '>' when Current == '=' => CreateToken(TokenKind.GreaterEqual, ">=", null, start, ++_position),
            '>' => CreateToken(TokenKind.Greater, ">", null, start, _position),
            '!' => UnknownToken(start, "Unexpected '!'. Only '!=' is valid in v0.1."),
            _ => UnknownToken(start, $"Unexpected character '{ch}'.")
        };
    }

    private Token ReadIdentifierOrKeyword()
    {
        var start = _position;
        while (char.IsLetterOrDigit(Current) || Current == '_')
        {
            _position++;
            if (IsAtEnd)
            {
                break;
            }
        }

        var text = _source.Slice(start, _position - start);

        if (!IsAtEnd && text == "osc" && Current == '.' && MatchDottedKeyword(".udp"))
        {
            _position += 4;
            text = "osc.udp";
        }
        else if (!IsAtEnd && text == "ws" && Current == '.' && MatchDottedKeyword(".client"))
        {
            _position += 7;
            text = "ws.client";
        }
        else if (!IsAtEnd && text == "ws" && Current == '.' && MatchDottedKeyword(".server"))
        {
            _position += 7;
            text = "ws.server";
        }

        var kind = Keywords.TryGetValue(text, out var keywordKind)
            ? keywordKind
            : TokenKind.Identifier;

        return CreateToken(kind, text, null, start, _position);
    }

    private bool MatchDottedKeyword(string suffix)
    {
        for (var i = 0; i < suffix.Length; i++)
        {
            if (Peek(i) != suffix[i])
            {
                return false;
            }
        }

        var next = Peek(suffix.Length);
        return !(char.IsLetterOrDigit(next) || next == '_');
    }

    private Token ReadNumber()
    {
        var start = _position;
        while (char.IsDigit(Current))
        {
            _position++;
            if (IsAtEnd)
            {
                break;
            }
        }

        if (!IsAtEnd && Current == '.' && char.IsDigit(Peek(1)))
        {
            _position++;
            while (char.IsDigit(Current))
            {
                _position++;
                if (IsAtEnd)
                {
                    break;
                }
            }
        }

        var text = _source.Slice(start, _position - start);
        double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value);
        return CreateToken(TokenKind.Number, text, value, start, _position);
    }

    private Token ReadString()
    {
        var start = _position;
        _position++;
        var builder = new System.Text.StringBuilder();
        var terminated = false;

        while (!IsAtEnd)
        {
            var ch = Current;
            if (ch == '"')
            {
                _position++;
                terminated = true;
                break;
            }

            if (ch == '\\')
            {
                _position++;
                if (IsAtEnd)
                {
                    break;
                }

                builder.Append(Current switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => Current
                });
                _position++;
                continue;
            }

            builder.Append(ch);
            _position++;
        }

        if (!terminated)
        {
            _diagnostics.ReportError(MakeSpan(start, _position), "Unterminated string literal.");
        }

        var text = _source.Slice(start, _position - start);
        return CreateToken(TokenKind.String, text, builder.ToString(), start, _position);
    }

    private Token UnknownToken(int start, string message)
    {
        var span = MakeSpan(start, _position);
        _diagnostics.ReportError(span, message);
        var text = _source.Slice(start, Math.Max(1, _position - start));
        return new Token(TokenKind.Identifier, text, null, span);
    }

    private Token CreateToken(TokenKind kind, string text, object? value, int start, int end) =>
        new(kind, text, value, MakeSpan(start, end));

    private SourceSpan MakeSpan(int start, int end) => new(_source.GetPosition(start), _source.GetPosition(end));
}
