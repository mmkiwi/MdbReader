using System.Text;

namespace MMKiwi.MdbReader.Helpers;
public class Jet4CompressedString : Encoding
{
    private const char UnknownCharacter = '\uFFFD';
    public Encoding BaseEncoding { get; }

    private Jet4CompressedString(Encoding baseEncoding)
    {
        BaseEncoding = baseEncoding;
    }

    static readonly Dictionary<int, Jet4CompressedString> s_encodings = new Dictionary<int, Jet4CompressedString>();
    public static Jet4CompressedString GetForEncoding(Encoding baseEncoding)
    {
        int codePage = baseEncoding.CodePage;
        return !s_encodings.ContainsKey(codePage) ? (s_encodings[codePage] = new(baseEncoding)) : s_encodings[codePage];
    }

    /// <inheritdoc/>
    public override int GetCharCount(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe) // skip first two characters if BOM
        {
            bytes = bytes[2..];
        }
        int count = 0;
        int lastSwitch = 0;
        bool compressedMode = true;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == 0) // switch mode
            {
                count += (i - lastSwitch) / (compressedMode ? 1 : 2);
                lastSwitch = i;
                compressedMode = !compressedMode;
            }
        }
        count += (bytes.Length - lastSwitch) / (compressedMode ? 1 : 2) - 1;
        return count;
    }

    /// <inheritdoc/>
    public override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe) // skip first two characters if BOM
        {
            bytes = bytes[2..];
        }

        bool compressedMode = true;

        int numChars = 0;

        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == 0)
            {
                compressedMode = !compressedMode;
            }
            else if (compressedMode)
            {
                //Single byte mode
                if (BaseEncoding.GetChars(bytes.Slice(i, 1), chars.Slice(numChars)) != 1) // character overflowed into more than once character
                {
                    chars[numChars] = UnknownCharacter;
                }
                numChars++;
            }
            else
            {
                //Two byte mode. UCS-16, which in this case will map fine to UTF-16 since we won't be outside BMP.
                if (Unicode.GetChars(bytes.Slice(i, 2), chars.Slice(numChars)) != 1) // character overflowed into more than once character
                {
                    chars[numChars] = UnknownCharacter;
                }
                i++;
                numChars++;
            }
        }

        return numChars;
    }

    /// <inheritdoc/>
    public override int GetCharCount(byte[] bytes, int index, int count) => GetCharCount(bytes.AsSpan(index, count));

    /// <inheritdoc/>
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) => GetChars(bytes.AsSpan(byteIndex, byteCount), chars.AsSpan(charIndex));


    /// <inheritdoc/>
    public override int GetMaxByteCount(int charCount) => charCount * 2;

    /// <inheritdoc/>
    public override int GetMaxCharCount(int byteCount) => byteCount;

    /// <inheritdoc/>
    public override int GetByteCount(char[] chars, int index, int count)
    {
        return Unicode.GetByteCount(chars, index, count);
    }

    /// <inheritdoc/>
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        return Unicode.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
    }
}
