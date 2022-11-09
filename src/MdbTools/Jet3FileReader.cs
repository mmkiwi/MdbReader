// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbTools.Helpers;

namespace MMKiwi.MdbTools;

internal class Jet3FileReader : Jet3Reader, IDisposable, IAsyncDisposable
{
    public Jet3FileReader(string filePath, MdbHeaderInfo db) : base(db)
    {
        FilePath = filePath;
        MdbStream = OpenStream();
    }

    public string FilePath { get; }

    /// <summary>
    /// This stream is only used to keep the file locked so nobody can edit or delete it while we are in it.
    /// </summary>
    private FileStream MdbStream { get; }

    private FileStream OpenStream() => File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

    protected override async Task ReadPartialPageToBufferAsync(int pageNo, Memory<byte> buffer, int start, CancellationToken ct)
    {
        // Can't do thread safety with async because you run into a race condition between the seek and the read.
        // So just running synchronously :(
        FileStream stream = OpenStream();
        await using (stream.ConfigureAwait(false))
        {
            stream.Seek(pageNo * PageSize + start, SeekOrigin.Begin);
            stream.Read(buffer.Span);
            DecryptPage(pageNo, buffer.Span);
        }
    }

    protected override void ReadPartialPageToBuffer(int pageNo, Span<byte> buffer, int start)
    {
        FileStream stream = OpenStream();
        stream.Seek(pageNo * PageSize + start, SeekOrigin.Begin);
        stream.Read(buffer);
        DecryptPage(pageNo, buffer);
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