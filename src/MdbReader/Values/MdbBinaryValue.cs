// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Diagnostics;
using MMKiwi.MdbReader.Schema;

namespace MMKiwi.MdbReader.Values;

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
internal sealed class MdbBinaryValue : MdbValue<ImmutableArray<byte>>, IValueAllowableType
{
    internal MdbBinaryValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, 0, column.Length, AllowableType) { }

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
}