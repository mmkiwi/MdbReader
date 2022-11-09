// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;

namespace MMKiwi.MdbTools.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.Guid"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is a 128-bit GUID. <see cref="Value" /> returns a .NET <see cref="Guid" />
/// Referred to in the Access GUI as a Number column with the Replication ID size, and as a <c>GUID</c> in SQL.
/// </para>
/// <para> 
/// This class is used for non-nullable columns only. For nullable columns, use <see cref="Nullable" />
/// </para>
/// </remarks>
public sealed class MdbGuidValue : MdbValue<Guid>, IValueAllowableType
{
    internal MdbGuidValue(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(column, isNull, binaryValue, false, 16, 16, AllowableType) { }
    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.Guid" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.Guid;

    /// <summary>
    /// The value for the specific row and column. A <see cref="Guid" />
    /// </summary>
    public override Guid Value => ConversionFunctions.AsGuid(BinaryValue.AsSpan());

    /// <summary>
    /// A database value corresponding to an Access <see cref="MdbColumnType.Guid"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a 128-bit GUID. <see cref="Value" /> returns a .NET <see cref="Guid" />
    /// Referred to in the Access GUI as a Number column with the Replication ID size, and as a <c>GUID</c> in SQL.
    /// </para>
    /// <para> 
    /// This class can be used for nullable and non-nullable columns. 
    /// For non-nullable columns, use <see cref="MdbGuidValue" /> to return a non-nullable value.
    /// </para>
    /// </remarks>
    public sealed class Nullable : MdbValue<Guid?>
    {
        internal Nullable(MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
            : base(column, isNull, binaryValue, true, 16, 16, AllowableType) { }

        /// <summary>
        /// The value for the specific row and column. A <see cref="Guid" />
        /// </summary>
        public override Guid? Value => IsNull ? null : ConversionFunctions.AsGuid(BinaryValue.AsSpan());

        /// <summary>
        /// Implicitly cast a non-nullable <see cref="MdbGuidValue" /> into a <see cref="Nullable" />
        /// </summary>
        /// <param name="val">The <see cref="MdbGuidValue" /> to convert</param>
        public static implicit operator Nullable(MdbGuidValue val)
            => new(val.Column, val.IsNull, val.BinaryValue);
    }
}
