// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Diagnostics;

namespace MMKiwi.MdbTools.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.Binary" />. 
/// </summary>
/// <remarks>
/// <para>
///   This is a byte array with up to <see cref="MdbColumn.Length" /> bytes. 
///   Note that if <see cref="MdbColumnFlags.FixedLength" /> is set for <see cref="MdbColumn.Flags" />, 
///   there may be null bytes appended to the end in order to reach the fixed length. 
///   These columns can't be created in the Access GUI. They have the type VARBINARY or BINARY in 
///   SQL.
/// </para>
/// <para>
///   This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: {Value}")]
public sealed class MdbBinaryValue : MdbValue<ImmutableArray<byte>>, IValueAllowableType
{
    internal MdbBinaryValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 0, column.Length, AllowableType) { }

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Binary" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Binary;

    /// <summary>
    /// The value for the specific row and column as a byte array.
    /// </summary>
    /// <remarks>
    /// This is a byte array with up to <see cref="MdbColumn.Length" /> bytes. 
    /// Note that if <see cref="MdbColumnFlags.FixedLength" /> is set for <see cref="MdbColumn.Flags" />, 
    /// there may be null bytes appended to the end in order to reach the fixed length.
    /// </remarks>
    public override ImmutableArray<byte> Value => BinaryValue;

    /// <summary>
    /// A database value corresponding to an Access <see cref="MdbColumnType.Binary" />. 
    /// </summary>
    /// <remarks>
    /// <para>
    ///   This is a byte array with up to <see cref="MdbColumn.Length" /> bytes. 
    ///   Note that if <see cref="MdbColumnFlags.FixedLength" /> is set for <see cref="MdbColumn.Flags" />, 
    ///   there may be null bytes appended to the end in order to reach the fixed length. 
    ///   These columns can't be created in the Access GUI. They have the type VARBINARY or BINARY in 
    ///   SQL.
    /// </para>
    /// <para>
    ///   This class is used for non-nullable columns only. For nullable columns, use <see cref="MdbBinaryValue" />
    /// </para>
    /// </remarks>
    [DebuggerDisplay("{Column.Name}: {Value}")]
    public sealed class Nullable : MdbValue<ImmutableArray<byte>?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 0, column.Length, AllowableType) { }

        /// <summary>
        /// The value for the specific row and column. A <see cref="Nullable{T}">nullable</see> <see cref="short" />
        /// (values from -32768 to 32767).
        /// </summary>
        public override ImmutableArray<byte>? Value => IsNull ? null : BinaryValue;

        /// <summary>
        /// Implicitly cast a non-nullable <see cref="MdbBinaryValue" /> into a <see cref="Nullable" />
        /// </summary>
        /// <param name="val">The <see cref="MdbBinaryValue" /> to convert</param>
        public static implicit operator Nullable(MdbBinaryValue val)
            => new(val.Column, val.IsNull, val.BinaryValue);
    }
}