// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Collections.Immutable;
using System.Diagnostics;

namespace MMKiwi.MdbReader.Values;

/// <summary>
/// A database value corresponding to an Access <see cref="MdbColumnType.OLE"/>. 
/// </summary>
/// <remarks>
/// <para>
/// This is variable-length binary data and <see cref="Value" /> returns a 
/// <see cref="MdbLValStream" /> to get the full value incrementally. 
/// Referred to in the Access GUI as a OLE column , and as an <c>LONGBINARY</c> in SQL.
/// </para>
/// </remarks>
[DebuggerDisplay("{Column.Name}: [OLE]")]
public class MdbOleValue : MdbLongValField<MdbLValStream?>, IValueAllowableType
{
    internal MdbOleValue(Jet3Reader reader, MdbColumn column, bool isNull, ImmutableArray<byte> binaryValue)
        : base(reader, column, isNull, binaryValue, AllowableType)
    {
    }
    /// <summary>
    /// A <see cref="MdbLValStream" /> that gets the full values of the field 
    /// Call <see cref="MdbLValStream.ReadToEnd" /> to get a byte array containing
    /// the entire value.
    /// </summary>
    /// <returns>The stream for for value value, or null if the value is null.</returns>
    public override MdbLValStream? Value => IsNull ? null : new MdbLValStream(new Jet3Reader.LvalStream(Reader, Column, BinaryValue));

    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this value.
    /// This will always be <see cref="MdbColumnType.OLE" />
    /// </summary>
    public static MdbColumnType AllowableType => MdbColumnType.OLE;
}
