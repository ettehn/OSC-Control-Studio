using OSCControl.Compiler.Text;

namespace OSCControl.Compiler.Lexing;

public sealed record Token(TokenKind Kind, string Text, object? Value, SourceSpan Span);
