using OSCControl.Compiler.Diagnostics;
using OSCControl.Compiler.Lexing;
using OSCControl.Compiler.Text;

namespace OSCControl.Compiler.Syntax;

public sealed class Parser
{
    private readonly IReadOnlyList<Token> _tokens;
    private readonly DiagnosticBag _diagnostics = new();
    private int _position;

    public Parser(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.Items;

    public ProgramSyntax ParseProgram()
    {
        var declarations = new List<DeclarationSyntax>();
        var start = Current.Span.Start;

        while (Current.Kind != TokenKind.EndOfFile)
        {
            var declaration = ParseDeclaration();
            if (declaration is not null)
            {
                declarations.Add(declaration);
            }
            else
            {
                RecoverToNextDeclaration();
            }
        }

        return new ProgramSyntax(declarations, SourceSpan.FromBounds(start, Current.Span.End));
    }

    private DeclarationSyntax? ParseDeclaration()
    {
        if (IsVrchatIdentifier(Current))
        {
            return ParseVrchatDeclaration();
        }

        return Current.Kind switch
        {
            TokenKind.KeywordEndpoint => ParseEndpointDeclaration(),
            TokenKind.KeywordState => ParseStateDeclaration(),
            TokenKind.KeywordFunc => ParseFunctionDeclaration(),
            TokenKind.KeywordOn => ParseRuleDeclaration(),
            _ => UnexpectedDeclaration()
        };
    }

    private DeclarationSyntax? UnexpectedDeclaration()
    {
        _diagnostics.ReportError(Current.Span, $"Unexpected token '{Current.Text}' at top level.");
        return null;
    }

    private DeclarationSyntax? ParseVrchatDeclaration()
    {
        var start = ConsumeNameLike("Expected 'vrchat'.").Span.Start;
        Consume(TokenKind.Dot);
        var command = ConsumeNameLike("Expected VRChat declaration after 'vrchat.'.");

        if (string.Equals(command.Name, "endpoint", StringComparison.OrdinalIgnoreCase))
        {
            ObjectLiteralExpressionSyntax? config = null;
            if (Current.Kind == TokenKind.LeftBrace)
            {
                config = ParseObjectLiteral();
            }

            var end = config?.Span.End ?? command.Span.End;
            return new VrchatEndpointDeclarationSyntax(config, SourceSpan.FromBounds(start, end));
        }

        _diagnostics.ReportError(command.Span, $"Unsupported VRChat declaration '{command.Name}'.");
        return null;
    }

    private EndpointDeclarationSyntax ParseEndpointDeclaration()
    {
        var start = Consume(TokenKind.KeywordEndpoint).Span.Start;
        var name = ParseIdentifier();
        Consume(TokenKind.Colon);
        var endpointType = ConsumeNameLike("Expected endpoint type after ':'.");
        var config = ParseObjectLiteral();
        return new EndpointDeclarationSyntax(name, endpointType.Name, config, SourceSpan.FromBounds(start, config.Span.End));
    }

    private StateDeclarationSyntax ParseStateDeclaration()
    {
        var start = Consume(TokenKind.KeywordState).Span.Start;
        var name = ParseIdentifier();
        Consume(TokenKind.Equal);
        var value = ParseExpression();
        return new StateDeclarationSyntax(name, value, SourceSpan.FromBounds(start, value.Span.End));
    }

    private FunctionDeclarationSyntax ParseFunctionDeclaration()
    {
        var start = Consume(TokenKind.KeywordFunc).Span.Start;
        var name = ParseIdentifier();
        Consume(TokenKind.LeftParen);

        var parameters = new List<IdentifierSyntax>();
        if (Current.Kind != TokenKind.RightParen)
        {
            do
            {
                parameters.Add(ParseIdentifier());
            }
            while (Match(TokenKind.Comma));
        }

        Consume(TokenKind.RightParen);
        var body = ParseExecBlock();
        return new FunctionDeclarationSyntax(name, parameters, body, SourceSpan.FromBounds(start, body.Span.End));
    }

    private RuleDeclarationSyntax ParseRuleDeclaration()
    {
        var start = Consume(TokenKind.KeywordOn).Span.Start;
        var trigger = ParseTrigger();

        ExpressionSyntax? condition = null;
        if (Match(TokenKind.KeywordWhen))
        {
            condition = ParseExpression();
        }

        var body = ParseExecBlock();
        return new RuleDeclarationSyntax(trigger, condition, body, SourceSpan.FromBounds(start, body.Span.End));
    }

    private TriggerSyntax ParseTrigger()
    {
        var start = Current.Span.Start;

        if (TryParseVrchatTrigger(out var vrchatTrigger))
        {
            return vrchatTrigger;
        }

        if (Match(TokenKind.KeywordReceive))
        {
            var endpoint = ParseIdentifier();
            return new ReceiveTriggerSyntax(endpoint, SourceSpan.FromBounds(start, endpoint.Span.End));
        }

        if (Match(TokenKind.KeywordAddress))
        {
            var value = ParseStringLiteral();
            return new AddressTriggerSyntax(value, SourceSpan.FromBounds(start, value.Span.End));
        }

        if (Match(TokenKind.KeywordTimer))
        {
            var interval = ParseNumberLiteral();
            return new TimerTriggerSyntax(interval, SourceSpan.FromBounds(start, interval.Span.End));
        }

        if (Match(TokenKind.KeywordStartup))
        {
            return new StartupTriggerSyntax(SourceSpan.FromBounds(start, Previous.Span.End));
        }

        _diagnostics.ReportError(Current.Span, "Expected rule trigger after 'on'.");
        var fallback = ParseIdentifier();
        return new ReceiveTriggerSyntax(fallback, fallback.Span);
    }

    private bool TryParseVrchatTrigger(out TriggerSyntax trigger)
    {
        trigger = null!;
        if (!IsVrchatIdentifier(Current))
        {
            return false;
        }

        var start = Current.Span.Start;
        NextToken();
        Consume(TokenKind.Dot);
        var name = ConsumeNameLike("Expected VRChat trigger after 'vrchat.'.");

        if (string.Equals(name.Name, "avatar_change", StringComparison.OrdinalIgnoreCase))
        {
            trigger = new VrchatAvatarChangeTriggerSyntax(SourceSpan.FromBounds(start, name.Span.End));
            return true;
        }

        if (string.Equals(name.Name, "param", StringComparison.OrdinalIgnoreCase))
        {
            var parameter = ConsumeNameLike("Expected avatar parameter name after 'vrchat.param'.");
            trigger = new VrchatAvatarParameterTriggerSyntax(parameter, SourceSpan.FromBounds(start, parameter.Span.End));
            return true;
        }

        _diagnostics.ReportError(name.Span, $"Unsupported VRChat trigger '{name.Name}'.");
        trigger = new VrchatAvatarChangeTriggerSyntax(SourceSpan.FromBounds(start, name.Span.End));
        return true;
    }

    private ExecBlockSyntax ParseExecBlock()
    {
        var start = Consume(TokenKind.LeftBracket).Span.Start;
        var statements = new List<StatementSyntax>();

        while (Current.Kind is not TokenKind.RightBracket and not TokenKind.EndOfFile)
        {
            var statement = ParseStatement();
            if (statement is not null)
            {
                statements.Add(statement);
            }
            else
            {
                RecoverToBlockBoundary();
            }
        }

        var end = Consume(TokenKind.RightBracket).Span.End;
        return new ExecBlockSyntax(statements, SourceSpan.FromBounds(start, end));
    }

    private StatementSyntax? ParseStatement() => Current.Kind switch
    {
        TokenKind.Identifier when IsVrchatIdentifier(Current) => ParseVrchatStatement(),
        TokenKind.KeywordSend => ParseSendStatement(),
        TokenKind.KeywordSet => ParseSetStatement(),
        TokenKind.KeywordStore => ParseStoreStatement(),
        TokenKind.KeywordLog => ParseLogStatement(),
        TokenKind.KeywordCall => ParseCallStatement(),
        TokenKind.KeywordStop => ParseStopStatement(),
        TokenKind.KeywordLet => ParseLetStatement(),
        TokenKind.KeywordIf => ParseIfStatement(),
        TokenKind.KeywordFor => ParseForEachStatement(),
        TokenKind.KeywordWhile => ParseWhileStatement(),
        TokenKind.KeywordBreak => ParseBreakStatement(),
        TokenKind.KeywordContinue => ParseContinueStatement(),
        _ => UnexpectedStatement()
    };

    private StatementSyntax? UnexpectedStatement()
    {
        _diagnostics.ReportError(Current.Span, $"Unexpected token '{Current.Text}' in executable block.");
        return null;
    }

    private StatementSyntax ParseVrchatStatement()
    {
        var start = ConsumeNameLike("Expected 'vrchat'.").Span.Start;
        Consume(TokenKind.Dot);
        var command = ConsumeNameLike("Expected VRChat command after 'vrchat.'.");

        return command.Name switch
        {
            "param" => ParseVrchatAvatarParameterStatement(start),
            "input" => ParseVrchatInputStatement(start),
            "chat" => ParseVrchatChatStatement(start),
            "typing" => ParseVrchatTypingStatement(start),
            _ => ParseUnsupportedVrchatStatement(start, command)
        };
    }

    private StatementSyntax ParseVrchatAvatarParameterStatement(SourcePosition start)
    {
        var name = ConsumeNameLike("Expected avatar parameter name after 'vrchat.param'.");
        Consume(TokenKind.Equal);
        var value = ParseExpression();
        return new VrchatAvatarParameterStatementSyntax(name, value, SourceSpan.FromBounds(start, value.Span.End));
    }

    private StatementSyntax ParseVrchatInputStatement(SourcePosition start)
    {
        var name = ConsumeNameLike("Expected input name after 'vrchat.input'.");
        Consume(TokenKind.Equal);
        var value = ParseExpression();
        return new VrchatInputStatementSyntax(name, value, SourceSpan.FromBounds(start, value.Span.End));
    }

    private StatementSyntax ParseVrchatChatStatement(SourcePosition start)
    {
        var text = ParseExpression();
        ExpressionSyntax? send = null;
        ExpressionSyntax? notify = null;

        while (IsNameLike(Current.Kind) && Peek(1).Kind == TokenKind.Equal)
        {
            var option = ConsumeNameLike("Expected VRChat chat option name.");
            Consume(TokenKind.Equal);
            var value = ParseExpression();

            switch (option.Name)
            {
                case "send":
                    send = value;
                    break;
                case "notify":
                    notify = value;
                    break;
                default:
                    _diagnostics.ReportError(option.Span, $"Unsupported VRChat chat option '{option.Name}'.");
                    break;
            }
        }

        var end = (notify ?? send ?? text).Span.End;
        return new VrchatChatStatementSyntax(text, send, notify, SourceSpan.FromBounds(start, end));
    }

    private StatementSyntax ParseVrchatTypingStatement(SourcePosition start)
    {
        var value = ParseExpression();
        return new VrchatTypingStatementSyntax(value, SourceSpan.FromBounds(start, value.Span.End));
    }

    private StatementSyntax ParseUnsupportedVrchatStatement(SourcePosition start, IdentifierSyntax command)
    {
        _diagnostics.ReportError(command.Span, $"Unsupported VRChat command '{command.Name}'.");
        return new StopStatementSyntax(SourceSpan.FromBounds(start, command.Span.End));
    }

    private SendStatementSyntax ParseSendStatement()
    {
        var start = Consume(TokenKind.KeywordSend).Span.Start;
        var target = ParseIdentifier();
        ObjectLiteralExpressionSyntax? payload = null;

        if (Current.Kind == TokenKind.LeftBrace)
        {
            payload = ParseObjectLiteral();
        }

        var end = payload?.Span.End ?? target.Span.End;
        return new SendStatementSyntax(target, payload, SourceSpan.FromBounds(start, end));
    }

    private SetStatementSyntax ParseSetStatement()
    {
        var start = Consume(TokenKind.KeywordSet).Span.Start;
        var target = ParseExpression();
        Consume(TokenKind.Equal);
        var value = ParseExpression();
        return new SetStatementSyntax(target, value, SourceSpan.FromBounds(start, value.Span.End));
    }

    private StoreStatementSyntax ParseStoreStatement()
    {
        var start = Consume(TokenKind.KeywordStore).Span.Start;
        var name = ParseIdentifier();
        Consume(TokenKind.Equal);
        var value = ParseExpression();
        return new StoreStatementSyntax(name, value, SourceSpan.FromBounds(start, value.Span.End));
    }

    private LogStatementSyntax ParseLogStatement()
    {
        var start = Consume(TokenKind.KeywordLog).Span.Start;
        string? level = null;

        if (Current.Kind is TokenKind.KeywordTrace or TokenKind.KeywordDebug or TokenKind.KeywordInfo or TokenKind.KeywordWarn or TokenKind.KeywordError)
        {
            level = NextToken().Text;
        }

        var value = ParseExpression();
        return new LogStatementSyntax(level, value, SourceSpan.FromBounds(start, value.Span.End));
    }

    private CallStatementSyntax ParseCallStatement()
    {
        var start = Consume(TokenKind.KeywordCall).Span.Start;
        var name = ParseIdentifier();
        var arguments = ParseArgumentList();
        return new CallStatementSyntax(name, arguments, SourceSpan.FromBounds(start, Previous.Span.End));
    }

    private StopStatementSyntax ParseStopStatement()
    {
        var token = Consume(TokenKind.KeywordStop);
        return new StopStatementSyntax(token.Span);
    }

    private LetStatementSyntax ParseLetStatement()
    {
        var start = Consume(TokenKind.KeywordLet).Span.Start;
        var name = ParseIdentifier();
        Consume(TokenKind.Equal);
        var value = ParseExpression();
        return new LetStatementSyntax(name, value, SourceSpan.FromBounds(start, value.Span.End));
    }

    private IfStatementSyntax ParseIfStatement()
    {
        var start = Consume(TokenKind.KeywordIf).Span.Start;
        var condition = ParseExpression();
        var thenBlock = ParseExecBlock();
        ExecBlockSyntax? elseBlock = null;

        if (Match(TokenKind.KeywordElse))
        {
            elseBlock = ParseExecBlock();
        }

        return new IfStatementSyntax(condition, thenBlock, elseBlock, SourceSpan.FromBounds(start, (elseBlock ?? thenBlock).Span.End));
    }

    private ForEachStatementSyntax ParseForEachStatement()
    {
        var start = Consume(TokenKind.KeywordFor).Span.Start;
        var iterator = ParseIdentifier();
        Consume(TokenKind.KeywordIn);
        var source = ParseExpression();
        var body = ParseExecBlock();
        return new ForEachStatementSyntax(iterator, source, body, SourceSpan.FromBounds(start, body.Span.End));
    }

    private WhileStatementSyntax ParseWhileStatement()
    {
        var start = Consume(TokenKind.KeywordWhile).Span.Start;
        var condition = ParseExpression();
        var body = ParseExecBlock();
        return new WhileStatementSyntax(condition, body, SourceSpan.FromBounds(start, body.Span.End));
    }

    private BreakStatementSyntax ParseBreakStatement()
    {
        var token = Consume(TokenKind.KeywordBreak);
        return new BreakStatementSyntax(token.Span);
    }

    private ContinueStatementSyntax ParseContinueStatement()
    {
        var token = Consume(TokenKind.KeywordContinue);
        return new ContinueStatementSyntax(token.Span);
    }

    private ExpressionSyntax ParseExpression(int precedence = 0)
    {
        var left = ParseUnaryExpression();

        while (true)
        {
            var currentPrecedence = GetBinaryPrecedence(Current.Kind);
            if (currentPrecedence <= precedence)
            {
                break;
            }

            var operatorToken = NextToken();
            var right = ParseExpression(currentPrecedence);
            left = new BinaryExpressionSyntax(left, operatorToken.Text, right, SourceSpan.FromBounds(left.Span.Start, right.Span.End));
        }

        return left;
    }

    private ExpressionSyntax ParseUnaryExpression()
    {
        if (Current.Kind is TokenKind.KeywordNot or TokenKind.Minus)
        {
            var operatorToken = NextToken();
            var operand = ParseUnaryExpression();
            return new UnaryExpressionSyntax(operatorToken.Text, operand, SourceSpan.FromBounds(operatorToken.Span.Start, operand.Span.End));
        }

        return ParsePostfixExpression();
    }

    private ExpressionSyntax ParsePostfixExpression()
    {
        var expression = ParsePrimaryExpression();

        while (true)
        {
            if (Current.Kind == TokenKind.LeftParen)
            {
                var arguments = ParseArgumentList();
                expression = new CallExpressionSyntax(expression, arguments, SourceSpan.FromBounds(expression.Span.Start, Previous.Span.End));
                continue;
            }

            if (Match(TokenKind.Dot))
            {
                var member = ConsumeNameLike("Expected member name after '.'.");
                expression = new MemberExpressionSyntax(expression, member, SourceSpan.FromBounds(expression.Span.Start, member.Span.End));
                continue;
            }

            if (Current.Kind == TokenKind.LeftBracket && IsAdjacentToPreviousToken())
            {
                NextToken();
                var index = ParseExpression();
                var end = Consume(TokenKind.RightBracket).Span.End;
                expression = new IndexExpressionSyntax(expression, index, SourceSpan.FromBounds(expression.Span.Start, end));
                continue;
            }

            break;
        }

        return expression;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        if (Current.Kind == TokenKind.LeftParen)
        {
            var start = NextToken().Span.Start;
            var expression = ParseExpression();
            var end = Consume(TokenKind.RightParen).Span.End;
            return new ParenthesizedExpressionSyntax(expression, SourceSpan.FromBounds(start, end));
        }

        if (Current.Kind == TokenKind.LeftDoubleBracket)
        {
            return ParseListLiteral();
        }

        if (Current.Kind == TokenKind.LeftBrace)
        {
            return ParseObjectLiteral();
        }

        if (Current.Kind == TokenKind.Number)
        {
            return ParseNumberLiteral();
        }

        if (Current.Kind == TokenKind.String)
        {
            return ParseStringLiteral();
        }

        if (Match(TokenKind.KeywordTrue))
        {
            return new BooleanLiteralExpressionSyntax(true, Previous.Span);
        }

        if (Match(TokenKind.KeywordFalse))
        {
            return new BooleanLiteralExpressionSyntax(false, Previous.Span);
        }

        if (Match(TokenKind.KeywordNull))
        {
            return new NullLiteralExpressionSyntax(Previous.Span);
        }

        return ParseIdentifier();
    }

    private ListLiteralExpressionSyntax ParseListLiteral()
    {
        var start = Consume(TokenKind.LeftDoubleBracket).Span.Start;
        var items = new List<ExpressionSyntax>();

        if (Current.Kind != TokenKind.RightDoubleBracket)
        {
            do
            {
                items.Add(ParseExpression());
            }
            while (Match(TokenKind.Comma));
        }

        var end = Consume(TokenKind.RightDoubleBracket).Span.End;
        return new ListLiteralExpressionSyntax(items, SourceSpan.FromBounds(start, end));
    }

    private ObjectLiteralExpressionSyntax ParseObjectLiteral()
    {
        var start = Consume(TokenKind.LeftBrace).Span.Start;
        var properties = new List<ObjectPropertySyntax>();

        while (Current.Kind is not TokenKind.RightBrace and not TokenKind.EndOfFile)
        {
            var keyToken = NextPropertyKey();
            Consume(TokenKind.Colon);
            var value = ParseExpression();
            properties.Add(new ObjectPropertySyntax(keyToken.Text, value, SourceSpan.FromBounds(keyToken.Span.Start, value.Span.End)));
            Match(TokenKind.Comma);
        }

        var end = Consume(TokenKind.RightBrace).Span.End;
        return new ObjectLiteralExpressionSyntax(properties, SourceSpan.FromBounds(start, end));
    }

    private IReadOnlyList<ExpressionSyntax> ParseArgumentList()
    {
        Consume(TokenKind.LeftParen);
        var arguments = new List<ExpressionSyntax>();

        if (Current.Kind != TokenKind.RightParen)
        {
            do
            {
                arguments.Add(ParseExpression());
            }
            while (Match(TokenKind.Comma));
        }

        Consume(TokenKind.RightParen);
        return arguments;
    }

    private Token NextPropertyKey()
    {
        if (Current.Kind == TokenKind.String || IsNameLike(Current.Kind))
        {
            return NextToken();
        }

        _diagnostics.ReportError(Current.Span, "Expected object property name.");
        return NextToken();
    }

    private NumberLiteralExpressionSyntax ParseNumberLiteral()
    {
        var token = Consume(TokenKind.Number);
        return new NumberLiteralExpressionSyntax(token.Value is double value ? value : 0d, token.Span);
    }

    private StringLiteralExpressionSyntax ParseStringLiteral()
    {
        var token = Consume(TokenKind.String);
        return new StringLiteralExpressionSyntax(token.Value as string ?? string.Empty, token.Span);
    }

    private IdentifierSyntax ParseIdentifier() => ConsumeNameLike("Expected identifier.");

    private IdentifierSyntax ConsumeNameLike(string message)
    {
        if (IsNameLike(Current.Kind))
        {
            var token = NextToken();
            return new IdentifierSyntax(token.Text, token.Span);
        }

        _diagnostics.ReportError(Current.Span, message);
        var fallback = NextToken();
        return new IdentifierSyntax(fallback.Text, fallback.Span);
    }

    private static bool IsNameLike(TokenKind kind) =>
        kind == TokenKind.Identifier || kind.ToString().StartsWith("Keyword", StringComparison.Ordinal);

    private static bool IsVrchatIdentifier(Token token) =>
        token.Kind == TokenKind.Identifier && string.Equals(token.Text, "vrchat", StringComparison.OrdinalIgnoreCase);

    private static int GetBinaryPrecedence(TokenKind kind) => kind switch
    {
        TokenKind.KeywordOr => 1,
        TokenKind.KeywordAnd => 2,
        TokenKind.EqualEqual or TokenKind.BangEqual => 3,
        TokenKind.Less or TokenKind.LessEqual or TokenKind.Greater or TokenKind.GreaterEqual => 4,
        TokenKind.Plus or TokenKind.Minus => 5,
        TokenKind.Star or TokenKind.Slash or TokenKind.Percent => 6,
        _ => 0
    };

    private Token Consume(TokenKind kind)
    {
        if (Current.Kind == kind)
        {
            return NextToken();
        }

        _diagnostics.ReportError(Current.Span, $"Expected token '{kind}', but found '{Current.Text}'.");
        return new Token(kind, string.Empty, null, Current.Span);
    }

    private bool Match(TokenKind kind)
    {
        if (Current.Kind != kind)
        {
            return false;
        }

        NextToken();
        return true;
    }

    private Token NextToken()
    {
        var token = Current;
        if (_position < _tokens.Count - 1)
        {
            _position++;
        }

        return token;
    }

    private bool IsAdjacentToPreviousToken() =>
        Current.Span.Start.Offset == Previous.Span.End.Offset;
    private Token Current => _tokens[Math.Min(_position, _tokens.Count - 1)];
    private Token Previous => _tokens[Math.Max(0, _position - 1)];
    private Token Peek(int offset) => _tokens[Math.Min(_position + offset, _tokens.Count - 1)];

    private void RecoverToNextDeclaration()
    {
        while (Current.Kind is not TokenKind.EndOfFile &&
               Current.Kind is not TokenKind.KeywordEndpoint &&
               Current.Kind is not TokenKind.KeywordState &&
               Current.Kind is not TokenKind.KeywordFunc &&
               Current.Kind is not TokenKind.KeywordOn &&
               !IsVrchatIdentifier(Current))
        {
            NextToken();
        }
    }

    private void RecoverToBlockBoundary()
    {
        while (Current.Kind is not TokenKind.EndOfFile and not TokenKind.RightBracket)
        {
            if (Current.Kind is TokenKind.KeywordSend or TokenKind.KeywordSet or TokenKind.KeywordStore or TokenKind.KeywordLog or TokenKind.KeywordCall or TokenKind.KeywordStop or TokenKind.KeywordLet or TokenKind.KeywordIf or TokenKind.KeywordFor or TokenKind.KeywordWhile or TokenKind.KeywordBreak or TokenKind.KeywordContinue ||
                IsVrchatIdentifier(Current))
            {
                return;
            }

            NextToken();
        }
    }
}
