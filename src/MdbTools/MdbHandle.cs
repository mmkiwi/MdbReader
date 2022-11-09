// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using MMKiwi.MdbTools.Helpers;
using MMKiwi.MdbTools.Values;

namespace MMKiwi.MdbTools;

/// <summary>
/// The handle for an Access *.mdb database
/// </summary>
public sealed partial class MdbHandle : IDisposable, IAsyncDisposable
{
    private MdbHandle(MdbHeaderInfo headerInfo, Jet3Reader reader)
    {
        Reader = reader;
        Locale = headerInfo.Locale;
        _userTables = new(GetUserTables, true);
    }

    /// <summary>
    /// Opens an mdb file on the filesystem.
    /// </summary>
    /// <remarks>
    /// This method is preferred over <see cref="Open(Stream, bool)" /> because it keeps the file locked to prevent
    /// changes by other programs while the handle is open. It also allows safe multi-threaded access asynchronously.
    /// </remarks>
    /// <param name="fileName">The filename to open</param>
    /// <returns>The handle to the filesystem.</returns>
    /// <throws cref="UnauthorizedAccessException">The caller does not have the required permission.</throws>
    /// <throws cref="ArgumentNullException"><c>fileName</c> is null.</throws>
    /// <throws cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</throws>
    /// <throws cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive).</throws>
    /// <throws cref="FileNotFoundException">The file specified in path was not found.</throws>
    /// <throws cref="NotSupportedException"><c>fileName</c> is in an invalid format.</throws>
    /// <throws cref="InvalidDataException">The mdb file is not a valid JET version 3 (Access 97) mdb file</throws>
    public static MdbHandle Open(string fileName)
    {
        using FileStream mdbFileStream = File.OpenRead(fileName);

        MdbHeaderInfo db = GetDbHeaderInfo(mdbFileStream);

        return new(db, db.JetVersion == JetVersion.Jet3 ? new Jet3FileReader(fileName, db) : throw new InvalidDataException("Only JET3 is supported"));
    }

    /// <summary>
    /// Uses a stream to open an mdb file.
    /// </summary>
    /// <remarks>
    /// If possible, use <see cref="Open(string)" /> instead. Because new streams can't be created to access the file,
    /// there is no ability to safety have both thread safety and asynchronosity.
    /// </remarks>
    /// <param name="stream">
    /// The stream to use for the MdbFile. <see cref="Stream.CanRead" /> and <see cref="Stream.CanSeek"/> must be true.
    /// </param>
    /// <param name="disableAsyncForThreadSafety">
    /// If true, all *Async operations will run asynchronously. When this is true, this method is not thread-safe since
    /// the stream cannot be locked and can be edited by multiple threads.
    /// If false, this handle will be thread-safe, but all async operations will happen synchronously in order to ensure
    /// the stream can be locked.
    /// </param>
    /// <returns>An MdbHandle that operates over the specified stream.</returns>
    /// <throws cref="ArgumentNullException">If <c>stream</c> is null</throws>
    /// <throws cref="ArgumentException">Thrown if the stream doesn't support <see cref="Stream.CanSeek">seeking</see>
    /// or <see cref="Stream.CanRead">reading</see>.</throws>
    /// <throws cref="InvalidDataException">The mdb file is not a valid JET version 3 (Access 97) mdb file</throws>
    public static MdbHandle Open(Stream stream, bool disableAsyncForThreadSafety = false)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));
        if (!stream.CanSeek || !stream.CanRead)
            throw new ArgumentException($"MdbHandle requires a stream that is readable and seekable");

        MdbHeaderInfo db = GetDbHeaderInfo(stream);

        return new(db, db.JetVersion == JetVersion.Jet3 ? new Jet3StreamReader(stream, db, disableAsyncForThreadSafety) : throw new InvalidDataException("Only JET3 is supported"));
    }

    private static MdbHeaderInfo GetDbHeaderInfo(Stream mdbFileStream)
    {
        if (mdbFileStream.Length < Jet3Reader.Constants.PageSize * 3)
            throw new InvalidDataException("File is not a valid Jet database.");

        byte[] header = new byte[Jet3Reader.Constants.PageSize];
        mdbFileStream.Seek(0, SeekOrigin.Begin);
        mdbFileStream.Read(header);

        if (!header.AsSpan(0, Jet3Reader.Constants.JetHeader.Length).SequenceEqual(Jet3Reader.Constants.JetHeader))
            throw new InvalidDataException("File is not JET database");

        byte jetVersion = header[0x14];

        byte[] tempRc4Key = new byte[] { 0xC7, 0xDA, 0x39, 0x6B };

        RC4.ApplyInPlace(header.AsSpan(0x18, 126), tempRc4Key);
        short locale = MdbBinary.ReadInt16LittleEndian(header.AsSpan(0x3a));
        short codePage = MdbBinary.ReadInt16LittleEndian(header.AsSpan(0x3c));
        var dbKey = header.AsSpan(0x3e, 4).ToArray();

        return new(jetVersion, locale, codePage, dbKey);
    }

    /// <summary>
    /// The encoding for the database.
    /// </summary>
    public Encoding Encoding => Reader.Encoding;

    private ImmutableArray<MdbTable> GetUserTables()
    {
        MdbTable catalogTable = new("MSysObjects", Reader.ReadTableDef(2), this);
        return EnumerateRows(catalogTable, new HashSet<string>()
            {
                "Id",
                "Name",
                 "Type",
                 "Flags"
            })
            .Where(r => ((MdbIntValue.Nullable)r.Values.First(f => f.Column.Name == "Type")).Value == 1 &&
                        ((MdbLongIntValue.Nullable)r.Values.First(f => f.Column.Name == "Flags")).Value == 0)
            .Select(CreateMdbTableFromRecord)
            .ToImmutableArray();
    }

    private MdbTable CreateMdbTableFromRecord(MdbDataRow row)
    {
        var result = row.Values.ToDictionary(field => field.Column!.Name);
        int id = ((MdbLongIntValue.Nullable)result["Id"]).Value ?? throw new FormatException("Could not get ID of table");
        string name = ((MdbStringValue.Nullable)result["Name"]).Value ?? throw new FormatException("Could not get Name of table"); ;
        return new MdbTable(name, Reader.ReadTableDef(id), this);
    }

    internal async IAsyncEnumerable<MdbDataRow> EnumerateRowsAsync(MdbTable table, HashSet<string>? columnsToTake, [EnumeratorCancellation] CancellationToken ct)
    {
        byte[] usageMap = await Reader.ReadUsageMapAsync(table.UsedPagesPtr, ct).ConfigureAwait(false);

        int page = 0;
        while (true)
        {
            page = await Reader.FindNextMapAsync(usageMap, page, ct).ConfigureAwait(false);
            if (page == 0)
                break;
            await foreach (var row in Reader.ReadDataPageAsync(page, table, columnsToTake ?? new(0), ct))
                yield return new(row);
        }
    }

    internal IEnumerable<MdbDataRow> EnumerateRows(MdbTable table, HashSet<string>? columnsToTake)
    {
        byte[] usageMap = Reader.ReadUsageMap(table.UsedPagesPtr);
        //byte[] freeMap = Reader.ReadUsageMap(table.FreePagesPtr);

        int page = 0;
        while (true)
        {
            page = Reader.FindNextMap(usageMap, page);
            if (page == 0)
                break;
            foreach (var row in Reader.ReadDataPage(page, table, columnsToTake ?? new(0)))
                yield return new(row);
        }
    }

    private readonly Lazy<ImmutableArray<MdbTable>> _userTables;

    /// <summary>
    /// The tables in the database
    /// </summary>
    public ImmutableArray<MdbTable> Tables => _userTables.Value;

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => Reader.DisposeAsync();

    /// <inheritdoc/>
    public void Dispose() => Reader.Dispose();

    internal Jet3Reader Reader { get; }

    /// <summary>
    /// The locale for the database. This doens't affect any outputs, but it does allow for
    /// consumers of the class to format things similar to how it was done in the Access database.
    /// </summary>
    public CultureInfo Locale { get; }

    /// <summary>
    /// The encryption key for the database
    /// </summary>
    public byte[]? DbKey => Reader.DbKey;
}
