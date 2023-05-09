// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Helpers;

namespace MMKiwi.MdbReader;

internal class Jet3StreamReader : Jet3Reader, IDisposable, IAsyncDisposable
{
    private readonly object _lock = new();

    public Jet3StreamReader(Stream mdbStream, MdbReaderOptions options, MdbHeaderInfo db, bool disableAsyncForThreadSafety) : base(db, options)
    {
        MdbStream = mdbStream;
        DisableAsync = disableAsyncForThreadSafety;
    }

    public Stream MdbStream { get; }
    public bool DisableAsync { get; }

    protected override async Task ReadPartialPageToBufferAsync(int pageNo, Memory<byte> buffer, int start, CancellationToken ct)
    {
        if (DisableAsync)
            ReadPartialPageToBuffer(pageNo, buffer.Span, start);
        else
        {
            MdbStream.Seek(pageNo * PageSize + start, SeekOrigin.Begin);
            await MdbStream.ReadAsync(buffer, ct).ConfigureAwait(false);
            DecryptPage(pageNo, buffer.Span);
        }
    }

    protected override void ReadPartialPageToBuffer(int pageNo, Span<byte> buffer, int start)
    {
        lock (_lock)
        {
            MdbStream.Seek(pageNo * PageSize + start, SeekOrigin.Begin);
            MdbStream.Read(buffer);
            DecryptPage(pageNo, buffer);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        IsDisposed = true;
        await MdbStream.DisposeAsync().ConfigureAwait(false);
    }
    public override void Dispose()
    {
        IsDisposed = true;
        MdbStream.Dispose();
    }
}

internal class Jet3StreamFactoryReader : Jet3Reader, IDisposable, IAsyncDisposable
{

    public Jet3StreamFactoryReader(Func<Stream> mdbStreamGenerator, MdbReaderOptions options, MdbHeaderInfo db, Stream? parentStream) : base(db, options)
    {
        MdbStreamGemerator = mdbStreamGenerator;
        ParentStream = parentStream;
    }

    public Func<Stream> MdbStreamGemerator { get; }
    public Stream? ParentStream { get; }

    protected override async Task ReadPartialPageToBufferAsync(int pageNo, Memory<byte> buffer, int start, CancellationToken ct)
    {
        using Stream mdbStream = MdbStreamGemerator();
        mdbStream.Seek(pageNo * PageSize + start, SeekOrigin.Begin);
        await mdbStream.ReadAsync(buffer, ct).ConfigureAwait(false);
        DecryptPage(pageNo, buffer.Span);
    }

    protected override void ReadPartialPageToBuffer(int pageNo, Span<byte> buffer, int start)
    {
        using Stream mdbStream = MdbStreamGemerator();
        mdbStream.Seek(pageNo * PageSize + start, SeekOrigin.Begin);
        mdbStream.Read(buffer);
        DecryptPage(pageNo, buffer);
    }

    public override async ValueTask DisposeAsync()
    {
        IsDisposed = true;
        if (ParentStream != null)
        {
            await ParentStream.DisposeAsync().ConfigureAwait(false);
        }
    }
    public override void Dispose()
    {
        IsDisposed = true;
        ParentStream?.Dispose();
    }
}
