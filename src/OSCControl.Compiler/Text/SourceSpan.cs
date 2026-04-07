namespace OSCControl.Compiler.Text;

public readonly record struct SourceSpan(SourcePosition Start, SourcePosition End)
{
    public static SourceSpan FromBounds(SourcePosition start, SourcePosition end) => new(start, end);
}
