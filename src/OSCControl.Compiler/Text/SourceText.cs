namespace OSCControl.Compiler.Text;

public sealed class SourceText
{
    private readonly string _text;
    private readonly int[] _lineStarts;

    public SourceText(string text)
    {
        _text = text ?? string.Empty;
        _lineStarts = BuildLineStarts(_text);
    }

    public string Text => _text;
    public int Length => _text.Length;

    public char this[int index] => index >= 0 && index < _text.Length ? _text[index] : '\0';

    public string Slice(int start, int length) => _text.Substring(start, length);

    public SourcePosition GetPosition(int offset)
    {
        var safeOffset = Math.Clamp(offset, 0, _text.Length);
        var lineIndex = Array.BinarySearch(_lineStarts, safeOffset);
        if (lineIndex < 0)
        {
            lineIndex = ~lineIndex - 1;
        }

        if (lineIndex < 0)
        {
            lineIndex = 0;
        }

        var lineStart = _lineStarts[lineIndex];
        return new SourcePosition(safeOffset, lineIndex + 1, (safeOffset - lineStart) + 1);
    }

    private static int[] BuildLineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                starts.Add(i + 1);
                continue;
            }

            if (text[i] == '\n')
            {
                starts.Add(i + 1);
            }
        }

        return starts.ToArray();
    }
}
