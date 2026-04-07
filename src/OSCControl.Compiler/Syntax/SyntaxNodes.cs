using OSCControl.Compiler.Text;

namespace OSCControl.Compiler.Syntax;

public abstract record SyntaxNode(SourceSpan Span);

public sealed record ProgramSyntax(IReadOnlyList<DeclarationSyntax> Declarations, SourceSpan Span) : SyntaxNode(Span);

public abstract record DeclarationSyntax(SourceSpan Span) : SyntaxNode(Span);
public abstract record StatementSyntax(SourceSpan Span) : SyntaxNode(Span);
public abstract record ExpressionSyntax(SourceSpan Span) : SyntaxNode(Span);
public abstract record TriggerSyntax(SourceSpan Span) : SyntaxNode(Span);

public sealed record IdentifierSyntax(string Name, SourceSpan Span) : ExpressionSyntax(Span);

public sealed record EndpointDeclarationSyntax(IdentifierSyntax Name, string EndpointType, ObjectLiteralExpressionSyntax Config, SourceSpan Span) : DeclarationSyntax(Span);
public sealed record VrchatEndpointDeclarationSyntax(ObjectLiteralExpressionSyntax? Config, SourceSpan Span) : DeclarationSyntax(Span);
public sealed record StateDeclarationSyntax(IdentifierSyntax Name, ExpressionSyntax Value, SourceSpan Span) : DeclarationSyntax(Span);
public sealed record FunctionDeclarationSyntax(IdentifierSyntax Name, IReadOnlyList<IdentifierSyntax> Parameters, ExecBlockSyntax Body, SourceSpan Span) : DeclarationSyntax(Span);
public sealed record RuleDeclarationSyntax(TriggerSyntax Trigger, ExpressionSyntax? Condition, ExecBlockSyntax Body, SourceSpan Span) : DeclarationSyntax(Span);

public sealed record ReceiveTriggerSyntax(IdentifierSyntax EndpointName, SourceSpan Span) : TriggerSyntax(Span);
public sealed record AddressTriggerSyntax(StringLiteralExpressionSyntax Value, SourceSpan Span) : TriggerSyntax(Span);
public sealed record TimerTriggerSyntax(NumberLiteralExpressionSyntax Interval, SourceSpan Span) : TriggerSyntax(Span);
public sealed record StartupTriggerSyntax(SourceSpan Span) : TriggerSyntax(Span);
public sealed record VrchatAvatarChangeTriggerSyntax(SourceSpan Span) : TriggerSyntax(Span);
public sealed record VrchatAvatarParameterTriggerSyntax(IdentifierSyntax ParameterName, SourceSpan Span) : TriggerSyntax(Span);

public sealed record ExecBlockSyntax(IReadOnlyList<StatementSyntax> Statements, SourceSpan Span) : SyntaxNode(Span);

public sealed record SendStatementSyntax(IdentifierSyntax Target, ObjectLiteralExpressionSyntax? Payload, SourceSpan Span) : StatementSyntax(Span);
public sealed record SetStatementSyntax(ExpressionSyntax Target, ExpressionSyntax Value, SourceSpan Span) : StatementSyntax(Span);
public sealed record StoreStatementSyntax(IdentifierSyntax Name, ExpressionSyntax Value, SourceSpan Span) : StatementSyntax(Span);
public sealed record LogStatementSyntax(string? Level, ExpressionSyntax Value, SourceSpan Span) : StatementSyntax(Span);
public sealed record CallStatementSyntax(IdentifierSyntax Name, IReadOnlyList<ExpressionSyntax> Arguments, SourceSpan Span) : StatementSyntax(Span);
public sealed record StopStatementSyntax(SourceSpan Span) : StatementSyntax(Span);
public sealed record LetStatementSyntax(IdentifierSyntax Name, ExpressionSyntax Value, SourceSpan Span) : StatementSyntax(Span);
public sealed record IfStatementSyntax(ExpressionSyntax Condition, ExecBlockSyntax ThenBlock, ExecBlockSyntax? ElseBlock, SourceSpan Span) : StatementSyntax(Span);
public sealed record ForEachStatementSyntax(IdentifierSyntax Iterator, ExpressionSyntax Source, ExecBlockSyntax Body, SourceSpan Span) : StatementSyntax(Span);
public sealed record WhileStatementSyntax(ExpressionSyntax Condition, ExecBlockSyntax Body, SourceSpan Span) : StatementSyntax(Span);
public sealed record BreakStatementSyntax(SourceSpan Span) : StatementSyntax(Span);
public sealed record ContinueStatementSyntax(SourceSpan Span) : StatementSyntax(Span);
public sealed record VrchatAvatarParameterStatementSyntax(IdentifierSyntax ParameterName, ExpressionSyntax Value, SourceSpan Span) : StatementSyntax(Span);
public sealed record VrchatInputStatementSyntax(IdentifierSyntax InputName, ExpressionSyntax Value, SourceSpan Span) : StatementSyntax(Span);
public sealed record VrchatChatStatementSyntax(ExpressionSyntax Text, ExpressionSyntax? SendValue, ExpressionSyntax? NotifyValue, SourceSpan Span) : StatementSyntax(Span);
public sealed record VrchatTypingStatementSyntax(ExpressionSyntax Value, SourceSpan Span) : StatementSyntax(Span);

public sealed record NumberLiteralExpressionSyntax(double Value, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record StringLiteralExpressionSyntax(string Value, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record BooleanLiteralExpressionSyntax(bool Value, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record NullLiteralExpressionSyntax(SourceSpan Span) : ExpressionSyntax(Span);
public sealed record ListLiteralExpressionSyntax(IReadOnlyList<ExpressionSyntax> Items, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record ObjectLiteralExpressionSyntax(IReadOnlyList<ObjectPropertySyntax> Properties, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record ObjectPropertySyntax(string Key, ExpressionSyntax Value, SourceSpan Span) : SyntaxNode(Span);
public sealed record CallExpressionSyntax(ExpressionSyntax Callee, IReadOnlyList<ExpressionSyntax> Arguments, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record MemberExpressionSyntax(ExpressionSyntax Target, IdentifierSyntax Member, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record IndexExpressionSyntax(ExpressionSyntax Target, ExpressionSyntax Index, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record UnaryExpressionSyntax(string Operator, ExpressionSyntax Operand, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record BinaryExpressionSyntax(ExpressionSyntax Left, string Operator, ExpressionSyntax Right, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record ParenthesizedExpressionSyntax(ExpressionSyntax Expression, SourceSpan Span) : ExpressionSyntax(Span);
