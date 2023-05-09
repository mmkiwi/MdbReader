// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Helpers;

namespace MMKiwi.MdbReader;

internal class Jet3FileReader : Jet3Reader, IDisposable, IAsyncDisposable
{
    public Jet3FileReader(string filePath, MdbReaderOptions options, MdbHeaderInfo db) : base(db, options)
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
            WriteDebug($"Reading page {pageNo} (async)");
            stream.Seek(pageNo * PageSize + start, SeekOrigin.Begin);
            stream.Read(buffer.Span);
            DecryptPage(pageNo, buffer.Span);
        }
    }

    protected override void ReadPartialPageToBuffer(int pageNo, Span<byte> buffer, int start)
    {
        WriteDebug($"Reading page {pageNo}");
        using FileStream stream = OpenStream();
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
        MdbStream.Close();
    }
}