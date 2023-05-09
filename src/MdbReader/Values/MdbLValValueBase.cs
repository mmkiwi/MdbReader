// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// The base class for Access data types that use LVal pages, <see cref="MdbOleValue" /> and <see cref="MdbMemoValue" />
/// </summary>
/// <typeparam name="TOut">
///     The resulting output type (string for <see cref="MdbMemoValue" /> and an immutable byte array for
///     <see cref="MdbOleValue" />
/// </typeparam>
internal abstract class MdbLongValField<TOut> : MdbValue<TOut>
{
    private protected MdbLongValField(Jet3Reader reader, MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue, MdbColumnType allowableType)
        : base(column, isNull, binaryValue, 0, int.MaxValue, allowableType)
    {
        Reader = reader;
    }

    protected private Jet3Reader Reader { get; }
}
