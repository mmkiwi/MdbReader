// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Text;

namespace MMKiwi.MdbTools;

public class Jet3StreamReader : Jet3Reader, IDisposable, IAsyncDisposable
{
    private readonly object _lock = new();

    public Jet3StreamReader(Stream mdbStream, Encoding encoding, bool disableAsyncForThreadSafety) : base(encoding)
    {
        MdbStream = mdbStream;
        DisableAsync = disableAsyncForThreadSafety;
    }

    public Stream MdbStream { get; }
    public bool DisableAsync { get; }

    protected override async Task ReadPartialPageToBufferAsync(int pageNo, Memory<byte> buffer, int start, CancellationToken ct)
    {
        if(DisableAsync)
        lock (_lock)
        {
            // Can't do thread safety with async because you run into a race condition between the seek and the read.
            // So just running synchronously :(
            MdbStream.Seek(pageNo * Constants.PageSize + start, SeekOrigin.Begin);
            MdbStream.Read(buffer.Span);
        }
        else
        {
            MdbStream.Seek(pageNo * Constants.PageSize + start, SeekOrigin.Begin);
            await MdbStream.ReadAsync(buffer).ConfigureAwait(false);
        }
    }

    protected override void ReadPartialPageToBuffer(int pageNo, Span<byte> buffer, int start)
    {
        lock (_lock)
        {
            MdbStream.Seek(pageNo * Constants.PageSize + start, SeekOrigin.Begin);
            MdbStream.Read(buffer);
        }
    }

    public override ValueTask DisposeAsync() => MdbStream.DisposeAsync();
    public override void Dispose() => MdbStream.Dispose();
}
