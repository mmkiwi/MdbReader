// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Fields;
public abstract class MdbField<TOut> : IMdbField<TOut>
{
    protected MdbField(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue, bool allowNull, int minLength, int maxLength, params ColumnType[] allowableTypes)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(binaryValue);

        if (!isNull && binaryValue.Length < minLength || binaryValue.Length > maxLength)
            throw new ArgumentException($"Invalid length {binaryValue.Length}.", nameof(binaryValue));
        if (!allowableTypes.Contains(column.Type))
            throw new ArgumentException($"Could not convert field value type {column.Type} to {string.Join(", ", allowableTypes)}", nameof(column));
        if(column.Flags.HasFlag(ColumnFlags.CanBeNull) && !allowNull)
            throw new ArgumentException($"Cannot create over nullable column", nameof(column));

        Column = column;
        IsNull = isNull;
        BinaryValue = isNull ? ImmutableArray<byte>.Empty : binaryValue;
    }

    public MdbColumn Column { get; }

    public bool IsNull { get; }

    protected ImmutableArray<byte> BinaryValue { get; }

    public abstract TOut Value { get; }

    object? IMdbField.Value => Value;

    public static implicit operator TOut(MdbField<TOut> f) => f.Value;


}
