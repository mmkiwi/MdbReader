// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader.Helpers;

/// <summary>
/// Implements the JET4 Compressed string encoding.
/// <seealso href="https://github.com/mdbtools/mdbtools/blob/dev/HACKING.md#text-data-type"/>
/// </summary>
/// <remarks>
/// All JET4 Compressed string data start with <c>0xFF 0xFE</c>. A <c>0x00</c> byte goes back and 
/// forth between compressed and uncompressed mode. Strings that contain a null byte will never be compressed.
/// </remarks>
public class Jet4CompressedString : Encoding
{
    private const char UnknownCharacter = '\uFFFD';

    private Jet4CompressedString(Encoding baseEncoding)
    {
        BaseEncoding = baseEncoding;
    }

    static readonly Dictionary<int, Jet4CompressedString> EncodingCache = new();

    /// <summary>
    /// Generates a Jet4CompressedString encoding with the specified <see cref="BaseEncoding"/>
    /// </summary>
    /// <remarks>
    /// This class caches instances of Jet4CompressedString for each <see cref="BaseEncoding"/>.
    /// </remarks>
    /// <param name="baseEncoding">The base encoding that is being compressed (pulled from the column)</param>
    /// <returns>A Jet4CompressedString instance.</returns>
    public static Jet4CompressedString GetForEncoding(Encoding baseEncoding)
    {
        int codePage = baseEncoding.CodePage;
        return !EncodingCache.ContainsKey(codePage) ? (EncodingCache[codePage] = new(baseEncoding)) : EncodingCache[codePage];
    }

    #region Metadata
    /// <summary>
    /// The base encoding that is being compressed (pulled from the column)
    /// </summary>
    public Encoding BaseEncoding { get; }
    /// <inheritdoc/>
    public override int CodePage => -1;
    /// <inheritdoc/>
    public override string BodyName => "";
    /// <inheritdoc/>
    public override string EncodingName => "Microsoft Acess compressed unicode";
    /// <inheritdoc/>
    public override string HeaderName => "";
    /// <inheritdoc/>
    public override bool IsSingleByte => false;
    /// <inheritdoc/>
    public override int WindowsCodePage => Unicode.WindowsCodePage;
    /// <inheritdoc/>
    public override bool IsBrowserDisplay => false;
    /// <inheritdoc/>
    public override bool IsBrowserSave => false;
    /// <inheritdoc/>
    public override bool IsMailNewsDisplay => false;
    /// <inheritdoc/>
    public override bool IsMailNewsSave => false;
    /// <inheritdoc/>
    public override string WebName => "";
    #endregion

    private static int GetCharCountCore(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2 || bytes[0] != 0xff || bytes[1] != 0xfe) // skip first two characters if BOM
        {
            return Unicode.GetCharCount(bytes);
        }

        bool compressedMode = true;
        ReadOnlySpan<byte> byteBody = bytes[2..];

        int count = 0;
        int lastSwitch = 0;
        for (int i = 0; i < byteBody.Length; i++)
        {
            if (byteBody[i] == 0) // switch mode
            {

                count += (i - lastSwitch) / (compressedMode ? 1 : 2);
                lastSwitch = i + 1;
                compressedMode = !compressedMode;
            }
            else
            {
                if (!compressedMode) // if in uncompressed mode, we go two bytes per run
                    i++;
            }
        }
        count += (byteBody.Length - lastSwitch) / (compressedMode ? 1 : 2);
        return count;
    }

    /// <summary>
    /// Jet4CompressedString: Decodes all the bytes in the specified read-only byte span into a character span.
    /// </summary>
    /// <param name="bytes">A read-only span containing the sequence of bytes to decode.</param>
    /// <param name="chars">The character span receiving the decoded bytes.</param>
    /// <returns>The number of decoded bytes.</returns>
    private int GetCharsCore(ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        if (bytes.Length == 0 || chars.Length == 0)
        {
            throw new ArgumentException("Must have length greater than zero", nameof(bytes));
        }

        if (bytes.Length < 2 || bytes[0] != 0xff || bytes[1] != 0xfe)
        {
            // uncompressed UTF-16
            return Unicode.GetChars(bytes, chars);
        }

        bool compressedMode = true;
        ReadOnlySpan<byte> byteBody = bytes[2..]; // skip first two characters if BOM

        int numChars = 0;

        for (int i = 0; i < byteBody.Length; i++)
        {
            if (byteBody[i] == 0)
            {
                compressedMode = !compressedMode;
            }
            else if (compressedMode)
            {
                int size = BaseEncoding.GetCharCount(byteBody.Slice(i, 1));
                if (i + 1 > byteBody.Length || numChars + size > chars.Length)
                {
                    throw new InvalidOperationException("Internal text conversion error.");
                }
                //Single byte mode
                if (BaseEncoding.GetChars(byteBody.Slice(i, 1), chars[numChars..]) != 1)
                // character overflowed into more
                //than once character, this will throw exception if an MB char is encountered near the end of the string
                {
                    chars[numChars] = UnknownCharacter;
                }
                numChars++;
            }
            else
            {

                if (i + 2 > byteBody.Length || numChars > chars.Length)
                {
                    throw new InvalidOperationException("Internal text conversion error.");
                }
                char currChar = (char)MdbBinary.ReadUInt16LittleEndian(byteBody.Slice(i, 2));
                chars[numChars] = currChar;

                i++;
                numChars++;
            }
        }

        return numChars;
    }

    /// <summary>
    /// Jet4CompressedString: Calculates the maximum number of characters produced by decoding the specified number of bytes.
    /// </summary>
    /// <param name="byteCount">The number of bytes to decode.</param>
    /// <returns>The maximum number of characters produced by decoding the specified number of bytes.</returns>
    public override int GetMaxCharCount(int byteCount) => Unicode.GetMaxCharCount(byteCount);

    #region Char > Byte (Not Supported)
    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    public override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
        => throw new NotSupportedException();

    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    [CLSCompliant(false)]
    public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        => throw new NotSupportedException();

    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    public override byte[] GetBytes(char[] chars)
        => throw new NotSupportedException();

    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        => throw new NotSupportedException();

    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    public override byte[] GetBytes(char[] chars, int index, int count)
        => throw new NotSupportedException();

    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    public override byte[] GetBytes(string s)
        => throw new NotSupportedException();

    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)

        => throw new NotSupportedException();
    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    public override int GetByteCount(ReadOnlySpan<char> chars)

        => throw new NotSupportedException();
    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    [CLSCompliant(false)]
    public override unsafe int GetByteCount(char* chars, int count)

        => throw new NotSupportedException();
    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    public override int GetByteCount(char[] chars)

        => throw new NotSupportedException();
    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    public override int GetByteCount(char[] chars, int index, int count)

        => throw new NotSupportedException();
    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    public override int GetByteCount(string s)
        => throw new NotSupportedException();

    /// <inheritdoc />
    /// <throws cref="NotSupportedException">This encoding only converts from bytes to chars, not vise versa</throws>
    public override int GetMaxByteCount(int charCount)
        => throw new NotSupportedException();
    #endregion

    #region Overrides for GetCharCount
    /// <inheritdocs/>
    [CLSCompliant(false)]
    public override unsafe int GetCharCount(byte* bytes, int count)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");
        }

        return GetCharCountCore(new(bytes, count));
    }

    /// <inheritdocs/>
    public override int GetCharCount(ReadOnlySpan<byte> bytes)
        => GetCharCountCore(bytes);

    /// <inheritdocs/>
    public override int GetCharCount(byte[] bytes)
        => GetCharCountCore(bytes.AsSpan());

    /// <inheritdocs/>
    public override int GetCharCount(byte[] bytes, int index, int count)
        => GetCharCountCore(bytes.AsSpan(index, count));
    #endregion

    #region Overrides for GetChars
    /// <inheritdocs/>
    public override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
        => GetCharsCore(bytes, chars);

    /// <inheritdocs/>
    [CLSCompliant(false)]
    public override unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        => GetCharsCore(new(bytes, byteCount), new(chars, charCount));

    /// <inheritdocs/>
    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
     => GetCharsCore(bytes.AsSpan(byteIndex, byteCount), chars.AsSpan(charIndex));

    /// <inheritdocs/>
    public override string GetString(byte[] bytes, int index, int count)
    {
        var numChars = GetCharCount(bytes, index, count);
        return string.Create(numChars, bytes, (span, byteArray) => GetCharsCore(byteArray, span));
    }

    /// <inheritdocs/>
    public override string GetString(byte[] bytes)
        => GetString(bytes, 0, bytes.Length);
    #endregion
}
