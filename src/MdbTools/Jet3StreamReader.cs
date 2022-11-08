// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbTools.Helpers;

namespace MMKiwi.MdbTools;

internal class Jet3StreamReader : Jet3Reader, IDisposable, IAsyncDisposable
{

    private readonly object _lock = new();

    public Jet3StreamReader(Stream mdbStream, MdbHeaderInfo db, bool disableAsyncForThreadSafety) : base(db)
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
            MdbStream.Seek(pageNo * Constants.PageSize + start, SeekOrigin.Begin);
            await MdbStream.ReadAsync(buffer, ct).ConfigureAwait(false);
            DecryptPage(pageNo, buffer.Span);
        }
    }

    protected override void ReadPartialPageToBuffer(int pageNo, Span<byte> buffer, int start)
    {
        lock (_lock)
        {
            MdbStream.Seek(pageNo * Constants.PageSize + start, SeekOrigin.Begin);
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
