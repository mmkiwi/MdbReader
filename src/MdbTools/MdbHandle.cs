// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using MMKiwi.MdbTools.Helpers;
using MMKiwi.MdbTools.Values;
using Nito.AsyncEx;

namespace MMKiwi.MdbTools;

/// <summary>
/// The handle for an Access *.mdb database
/// </summary>
public sealed partial class MdbHandle : IDisposable, IAsyncDisposable
{
    private MdbHandle(Jet3Reader reader)
    {
        Reader = reader;
        _userTables = new(async () => await GetUserTablesAsync(default));
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

        MdbHeaderInfo db = Jet3Reader.GetDatabaseInfo(mdbFileStream);

        return new(new Jet3FileReader(fileName, db));
    }

    /// <summary>
    /// Asynchronously opens an mdb file on the filesystem.
    /// </summary>
    /// <param name="fileName">The filename to open</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The handle to the filesystem.</returns>
    /// <throws cref="UnauthorizedAccessException">The caller does not have the required permission.</throws>
    /// <throws cref="ArgumentNullException"><c>fileName</c> is null.</throws>
    /// <throws cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</throws>
    /// <throws cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive).</throws>
    /// <throws cref="FileNotFoundException">The file specified in path was not found.</throws>
    /// <throws cref="NotSupportedException"><c>fileName</c> is in an invalid format.</throws>
    /// <throws cref="InvalidDataException">The mdb file is not a valid JET version 3 (Access 97) mdb file</throws>
    public async static Task<MdbHandle> OpenAsync(string fileName, CancellationToken ct = default)
    {
        using FileStream mdbFileStream = File.OpenRead(fileName);

        MdbHeaderInfo db = await Jet3Reader.GetDatabaseInfoAsync(mdbFileStream, ct).ConfigureAwait(false);

        return new(new Jet3FileReader(fileName, db));
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

        MdbHeaderInfo db = Jet3Reader.GetDatabaseInfo(stream);

        return new(new Jet3StreamReader(stream, db, disableAsyncForThreadSafety));
    }

    /// <summary>
    /// Opens an Access database using a function that creates streams. This allows for concurrent access on different threads.
    /// </summary>
    /// <param name="streamFactory">A method returning a stream pointing to the database. Must support seeking and reading.</param>
    /// <param name="parentStream">
    /// A parent stream. Set this if you wnat to keep a handle open to the file and prevent other processes from writing to it.
    /// </param>
    /// <returns>An MdbHandle that operates over the specified stream.</returns>
    /// <throws cref="ArgumentNullException">If <c>stream</c> is null</throws>
    /// <throws cref="ArgumentException">Thrown if the stream doesn't support <see cref="Stream.CanSeek">seeking</see>
    /// or <see cref="Stream.CanRead">reading</see>.</throws>
    /// <throws cref="InvalidDataException">The mdb file is not a valid JET version 3 (Access 97) mdb file</throws>
    public static MdbHandle Open(Func<Stream> streamFactory, Stream? parentStream = null)
    {
        if (streamFactory is null)
            throw new ArgumentNullException(nameof(streamFactory));

        using var stream = streamFactory();
        if (!stream.CanSeek || !stream.CanRead)
            throw new ArgumentException($"MdbHandle requires a stream that is readable and seekable");

        MdbHeaderInfo db = Jet3Reader.GetDatabaseInfo(stream);
        return new(new Jet3StreamFactoryReader(streamFactory, db, parentStream));
    }


    /// <summary>
    /// Opens an Access database using a function that creates streams. This allows for concurrent access on different threads.
    /// </summary>
    /// <param name="streamFactory">A method returning a stream pointing to the database. Must support seeking and reading.</param>
    /// <param name="parentStream">
    /// A parent stream. Set this if you wnat to keep a handle open to the file and prevent other processes from writing to it.
    /// </param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An MdbHandle that operates over the specified stream.</returns>
    /// <throws cref="ArgumentNullException">If <c>stream</c> is null</throws>
    /// <throws cref="ArgumentException">Thrown if the stream doesn't support <see cref="Stream.CanSeek">seeking</see>
    /// or <see cref="Stream.CanRead">reading</see>.</throws>
    /// <throws cref="InvalidDataException">The mdb file is not a valid JET version 3 (Access 97) mdb file</throws>
    public async static Task<MdbHandle> OpenAsync(Func<Stream> streamFactory, Stream? parentStream = null, CancellationToken ct = default)
    {
        if (streamFactory is null)
            throw new ArgumentNullException(nameof(streamFactory));

        using var stream = streamFactory();
        if (!stream.CanSeek || !stream.CanRead)
            throw new ArgumentException($"MdbHandle requires a stream that is readable and seekable");

        MdbHeaderInfo db = await Jet3Reader.GetDatabaseInfoAsync(stream, ct).ConfigureAwait(false);
        return new(new Jet3StreamFactoryReader(streamFactory, db, parentStream));
    }

    /// <summary>
    /// The encoding for the database.
    /// </summary>
    public Encoding Encoding => Reader.Db.Encoding;

    /// <summary>
    /// The version of the database
    /// </summary>
    public JetVersion JetVersion => Reader.Db.JetVersion;

    /// <summary>
    /// The default collation method for the database.
    /// </summary>

    public ushort Collation => Reader.Db.Collation;

    /// <summary>
    /// The encryption key for the database
    /// </summary>
    public uint DbKey => Reader.Db.DbKey;

    /// <summary>
    /// The date the database was created (not supported on JET3 databases)
    /// </summary>

    public DateTime CreationDate => Reader.Db.CreationDate;

    /*private ImmutableArray<MdbTable> GetUserTables()
    {
        MdbTable catalogTable = Reader.ReadTableDef(2).Build("MSysObjects", Reader);

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
    }*/

    private async Task<MdbTables> GetUserTablesAsync(CancellationToken ct)
    {
        var tableDef = await Reader.ReadTableDefAsync(2, ct).ConfigureAwait(false);
        MdbTable catalogTable = tableDef.Build("MSysObjects", Reader);

        var tables = await EnumerateRowsAsync(catalogTable, new HashSet<string>()
            {
                "Id",
                "Name",
                 "Type",
                 "Flags"
            }, ct)
            .Where(r => ((MdbIntValue.Nullable)r.Values.First(f => f.Column.Name == "Type")).Value == 1 &&
                        ((MdbLongIntValue.Nullable)r.Values.First(f => f.Column.Name == "Flags")).Value == 0)
            .Select(CreateMdbTableFromRecord)
            .ToArrayAsync().ConfigureAwait(false);
        ImmutableArray<MdbTable> immutTables = Unsafe.As<MdbTable[], ImmutableArray<MdbTable>>(ref tables);
        return new MdbTables(immutTables);
    }

    private MdbTable CreateMdbTableFromRecord(MdbDataRow row)
    {
        var result = row.Values.ToDictionary(field => field.Column!.Name);
        int id = ((MdbLongIntValue.Nullable)result["Id"]).Value ?? throw new FormatException("Could not get ID of table");
        string name = ((MdbStringValue.Nullable)result["Name"]).Value ?? throw new FormatException("Could not get Name of table");
        return Reader.ReadTableDef(id).Build(name, Reader);
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

    readonly AsyncLazy<MdbTables> _userTables;

    /// <summary>
    /// The tables in the database
    /// </summary>
    public MdbTables GetTables()
    {
        _userTables.Task.Start();
        return _userTables.Task.Result;
    }

    public async Task<MdbTables> GetTablesAsync()
    {
        return await _userTables;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => Reader.DisposeAsync();

    /// <inheritdoc/>
    public void Dispose() => Reader.Dispose();

    internal Jet3Reader Reader { get; }
}
