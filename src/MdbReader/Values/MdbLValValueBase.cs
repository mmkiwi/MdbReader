// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// The base class for Access data types that use LVal pages, <see cref="MdbOleValue" /> and <see cref="MdbMemoValue" />
/// </summary>
/// <typeparam name="TOut">
///     The resulting output type (string for <see cref="MdbMemoValue" /> and an immutable byte array for
///     <see cref="MdbOleValue" />
/// </typeparam>
public abstract class MdbLongValField<TOut> : MdbValue<TOut>
{
    private protected MdbLongValField(Jet3Reader reader, MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue, MdbColumnType allowableType)
        : base(column, isNull, binaryValue, column.Flags.HasFlag(MdbColumnFlags.CanBeNull), 0, int.MaxValue, allowableType)
    {
        Reader = reader;
    }

    protected private Jet3Reader Reader { get; }

}
