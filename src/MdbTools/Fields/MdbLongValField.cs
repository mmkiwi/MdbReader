// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;

public abstract class MdbLongValField<TOut> : MdbField<TOut>
{
    protected MdbLongValField(Jet3Reader reader, MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue, ColumnType[] allowableTypes)
        : base(column, isNull, binaryValue, column.Flags.HasFlag(ColumnFlags.CanBeNull), 0, int.MaxValue, allowableTypes)
    {
        Reader = reader;
    }

    protected private Jet3Reader Reader { get; }

}
