// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Helpers;

namespace MMKiwi.MdbReader;

/// <summary>
/// The handle for an Access *.mdb database
/// </summary>
public sealed partial class MdbConnection : IDisposable, IAsyncDisposable
{
    private MdbConnection(Jet3Reader reader, MdbReaderOptions options, MdbTables tables)
    {
        Reader = reader;
        Options = options;
        Tables = tables;
    }

    /// <summary>
    /// Opens an mdb file on the filesystem.
    /// </summary>
    /// <remarks>
    /// This method is preferred over <see cref="Open(Stream, MdbReaderOptions, bool)" /> because it keeps the file locked to prevent
    /// changes by other programs while the handle is open. It also allows safe multi-threaded access asynchronously.
    /// </remarks>
    /// <param name="fileName">The filename to open</param>
    /// <param name="options">The reading options. If null, <see cref="MdbReaderOptions.Default" /> will be used.</param>
    /// <returns>The handle to the filesystem.</returns>
    /// <throws cref="UnauthorizedAccessException">The caller does not have the required permission.</throws>
    /// <throws cref="ArgumentNullException"><c>fileName</c> is null.</throws>
    /// <throws cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</throws>
    /// <throws cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive).</throws>
    /// <throws cref="FileNotFoundException">The file specified in path was not found.</throws>
    /// <throws cref="NotSupportedException"><c>fileName</c> is in an invalid format.</throws>
    /// <throws cref="InvalidDataException">The mdb file is not a valid JET version 3 (Access 97) mdb file</throws>
    public static MdbConnection Open(string fileName, MdbReaderOptions? options = null)
    {

        options ??= MdbReaderOptions.Default;
        using FileStream mdbFileStream = File.OpenRead(fileName);

        Jet3Reader.ValidateDatabase(mdbFileStream);

        MdbHeaderInfo db = Jet3Reader.GetDatabaseInfo(mdbFileStream);
        Jet3FileReader reader = new(fileName, options, db);
        MdbConnection handle = new(reader, options, reader.GetUserTables(options.TableNameComparison));
        return handle;
    }

    /// <summary>
    /// Asynchronously opens an mdb file on the filesystem.
    /// </summary>
    /// <param name="fileName">The filename to open</param>
    /// <param name="options">The reading options. If null, <see cref="MdbReaderOptions.Default" /> will be used.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The handle to the filesystem.</returns>
    /// <throws cref="UnauthorizedAccessException">The caller does not have the required permission.</throws>
    /// <throws cref="ArgumentNullException"><c>fileName</c> is null.</throws>
    /// <throws cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length.</throws>
    /// <throws cref="DirectoryNotFoundException">The specified path is invalid, (for example, it is on an unmapped drive).</throws>
    /// <throws cref="FileNotFoundException">The file specified in path was not found.</throws>
    /// <throws cref="NotSupportedException"><c>fileName</c> is in an invalid format.</throws>
    /// <throws cref="InvalidDataException">The mdb file is not a valid JET version 3 (Access 97) mdb file</throws>
    public static Task<MdbConnection> OpenAsync(string fileName, MdbReaderOptions? options = null, CancellationToken ct = default)
    {
        options ??= MdbReaderOptions.Default;
        FileStream mdbFileStream = File.OpenRead(fileName);

        try
        {
            Jet3Reader.ValidateDatabase(mdbFileStream);
        }
        catch
        {
            mdbFileStream.Dispose();
            throw;
        }

        return Impl(fileName, options, mdbFileStream, ct);

        static async Task<MdbConnection> Impl(string fileName, MdbReaderOptions options, FileStream mdbFileStream, CancellationToken ct)
        {
            try
            {
                MdbHeaderInfo db = await Jet3Reader.GetDatabaseInfoAsync(mdbFileStream, ct).ConfigureAwait(false);

                Jet3FileReader reader = new(fileName, options, db);
                MdbConnection handle = new(reader, options, await reader.GetUserTablesAsync(options.TableNameComparison, ct).ConfigureAwait(false));
                return handle;
            }
            finally
            {
                await mdbFileStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Uses a stream to open an mdb file.
    /// </summary>
    /// <remarks>
    /// If possible, use <see cref="Open(string, MdbReaderOptions)" /> instead. Because new streams can't be created to access the file,
    /// there is no ability to safety have both thread safety and asynchronosity.
    /// </remarks>
    /// <param name="stream">
    /// The stream to use for the MdbFile. <see cref="Stream.CanRead" /> and <see cref="Stream.CanSeek"/> must be true.
    /// </param>
    /// <param name="options">The reading options. If null, <see cref="MdbReaderOptions.Default" /> will be used.</param>
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
    public static MdbConnection Open(Stream stream, MdbReaderOptions? options = null, bool disableAsyncForThreadSafety = false)
    {
        options ??= MdbReaderOptions.Default;
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));
        if (!stream.CanSeek || !stream.CanRead)
            throw new ArgumentException($"MdbHandle requires a stream that is readable and seekable");

        Jet3Reader.ValidateDatabase(stream);

        MdbHeaderInfo db = Jet3Reader.GetDatabaseInfo(stream);

        var reader = new Jet3StreamReader(stream, options, db, disableAsyncForThreadSafety);
        MdbConnection handle = new(reader, options, reader.GetUserTables(options.TableNameComparison));
        return handle;
    }

    /// <summary>
    /// Opens an Access database using a function that creates streams. This allows for concurrent access on different threads.
    /// </summary>
    /// <param name="streamFactory">A method returning a stream pointing to the database. Must support seeking and reading.</param>
    /// <param name="options">The reading options</param>
    /// <param name="parentStream">
    /// A parent stream. Set this if you wnat to keep a handle open to the file and prevent other processes from writing to it.
    /// </param>
    /// <returns>An MdbHandle that operates over the specified stream.</returns>
    /// <throws cref="ArgumentNullException">If <c>stream</c> is null</throws>
    /// <throws cref="ArgumentException">Thrown if the stream doesn't support <see cref="Stream.CanSeek">seeking</see>
    /// or <see cref="Stream.CanRead">reading</see>.</throws>
    /// <throws cref="InvalidDataException">The mdb file is not a valid JET version 3 (Access 97) mdb file</throws>
    public static MdbConnection Open(Func<Stream> streamFactory, MdbReaderOptions? options = null, Stream? parentStream = null)
    {
        options ??= MdbReaderOptions.Default;

        if (streamFactory is null)
            throw new ArgumentNullException(nameof(streamFactory));

        using var stream = streamFactory();
        if (!stream.CanSeek || !stream.CanRead)
            throw new ArgumentException($"MdbHandle requires a stream that is readable and seekable");

        Jet3Reader.ValidateDatabase(stream);

        MdbHeaderInfo db = Jet3Reader.GetDatabaseInfo(stream);
        var reader = new Jet3StreamFactoryReader(streamFactory, options, db, parentStream);
        MdbConnection handle = new(reader, options, reader.GetUserTables(options.TableNameComparison));
        return handle;
    }

    /// <summary>
    /// Opens an Access database using a function that creates streams. This allows for concurrent access on different threads.
    /// </summary>
    /// <param name="streamFactory">A method returning a stream pointing to the database. Must support seeking and reading.</param>
    /// <param name="options">The reading options. If null, <see cref="MdbReaderOptions.Default" /> will be used.</param>
    /// <param name="parentStream">
    /// A parent stream. Set this if you want to keep a handle open to the file and prevent other processes from writing to it.
    /// </param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An MdbHandle that operates over the specified stream.</returns>
    /// <throws cref="ArgumentNullException">If <c>stream</c> is null</throws>
    /// <throws cref="ArgumentException">Thrown if the stream doesn't support <see cref="Stream.CanSeek">seeking</see>
    /// or <see cref="Stream.CanRead">reading</see>.</throws>
    /// <throws cref="InvalidDataException">The mdb file is not a valid JET version 3 (Access 97) mdb file</throws>
    public static Task<MdbConnection> OpenAsync(Func<Stream> streamFactory, MdbReaderOptions? options = null, Stream? parentStream = null, CancellationToken ct = default)
    {
        options ??= MdbReaderOptions.Default;
        if (streamFactory is null)
            throw new ArgumentNullException(nameof(streamFactory));

        var stream = streamFactory();

        try
        {
            if (!stream.CanSeek || !stream.CanRead)
                throw new ArgumentException($"MdbHandle requires a stream that is readable and seekable");

            Jet3Reader.ValidateDatabase(stream);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
        return Impl(streamFactory, options, parentStream, stream, ct);

        static async Task<MdbConnection> Impl(Func<Stream> streamFactory, MdbReaderOptions options, Stream? parentStream, Stream stream, CancellationToken ct)
        {
            try
            {
                MdbHeaderInfo db = await Jet3Reader.GetDatabaseInfoAsync(stream, ct).ConfigureAwait(false);
                var reader = new Jet3StreamFactoryReader(streamFactory, options, db, parentStream);
                MdbConnection handle = new(reader, options, await reader.GetUserTablesAsync(options.TableNameComparison, ct).ConfigureAwait(false));
                return handle;
            }
            finally
            {
                await stream.DisposeAsync().ConfigureAwait(false);
            }
        }
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

    public int Collation => Reader.Db.Collation;

    /// <summary>
    /// The encryption key for the database
    /// </summary>
    public int DbKey => Reader.Db.DbKey;

    /// <summary>
    /// The date the database was created (not supported on JET3 databases)
    /// </summary>
    public DateTime CreationDate => Reader.Db.CreationDate;

    /// <summary>
    /// The tables in the specified database
    /// </summary>
    public MdbTables Tables { get; }

    /// <summary>
    /// Closes the connection. The connection is closed automatically if the
    /// MdbConnection is disposed of.
    /// </summary>
    public void Close() => Reader.Dispose();

    /// <summary>
    /// Closes the connection asynchronously. The connection is closed automatically if the
    /// MdbConnection is disposed of.
    /// </summary>
    public ValueTask CloseAsync() => Reader.DisposeAsync();

    /// <inheritdoc/>
    ValueTask IAsyncDisposable.DisposeAsync() => CloseAsync();

    /// <inheritdoc/>
    void IDisposable.Dispose() => Close();

    internal Jet3Reader Reader { get; }

    /// <summary>
    /// The options for the specified reader.
    /// </summary>
    public MdbReaderOptions Options { get; }
}
