namespace Serilog.Rendering;

interface ICharEscaper
{
    bool NeedsEscaping(char c);
    void WriteEscape(char c, TextWriter output);
}

readonly struct QuoteEscaper : ICharEscaper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool NeedsEscaping(char c) => c == '"';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEscape(char c, TextWriter output) => output.Write("\\\"");
}

readonly struct JsonCharEscaper : ICharEscaper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool NeedsEscaping(char c) => c is < (char)32 or '\\' or '"';

    public void WriteEscape(char c, TextWriter output)
    {
        switch (c)
        {
            case '"':
                output.Write("\\\"");
                break;
            case '\\':
                output.Write("\\\\");
                break;
            case '\n':
                output.Write("\\n");
                break;
            case '\r':
                output.Write("\\r");
                break;
            case '\f':
                output.Write("\\f");
                break;
            case '\t':
                output.Write("\\t");
                break;
            default:
                output.Write("\\u");
                output.Write(((int)c).ToString("X4"));
                break;
        }
    }
}

static class StringSegmentWriter
{
    internal static void WriteEscaped<TEscaper>(string str, TextWriter output, TEscaper escaper)
        where TEscaper : struct, ICharEscaper
    {
        var cleanSegmentStart = 0;
        var anyEscaped = false;

        for (var i = 0; i < str.Length; ++i)
        {
            var c = str[i];
            if (escaper.NeedsEscaping(c))
            {
                anyEscaped = true;
                WriteSegment(str, cleanSegmentStart, i - cleanSegmentStart, output);
                escaper.WriteEscape(c, output);
                cleanSegmentStart = i + 1;
            }
        }

        if (anyEscaped)
        {
            if (cleanSegmentStart < str.Length)
                WriteSegmentToEnd(str, cleanSegmentStart, output);
        }
        else
        {
            output.Write(str);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WriteSegment(string str, int start, int count, TextWriter output)
    {
#if FEATURE_SPAN
        output.Write(str.AsSpan(start, count));
#else
        output.Write(str.Substring(start, count));
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WriteSegmentToEnd(string str, int start, TextWriter output)
    {
#if FEATURE_SPAN
        output.Write(str.AsSpan(start));
#else
        output.Write(str.Substring(start));
#endif
    }
}
