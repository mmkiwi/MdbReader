// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using MMKiwi.MdbReader.Schema;

namespace MMKiwi.MdbReader.Values;

#if NET7_0_OR_GREATER
/// <summary>
/// This interface defined the static <see cref="AllowableType" /> property which specifies which
/// <see cref="MdbColumnType">ColumnTypes</see> are allowed for a given <see cref="IMdbValue"/>
/// </summary>
/// <remarks>
/// This interface does nothing in .NET versions prior to 7 since those versions do not support
/// static abstract properties.
/// </remarks>
#else
/// <summary>
/// This interface does nothing in .NET versions prior to 7 since those versions do not support
/// static abstract properties.
/// </summary>
#endif
internal interface IValueAllowableType
{
#if NET7_0_OR_GREATER
    /// <summary>
    /// The <see cref="MdbColumnType" /> that can be used for this
    /// <see cref="MdbValue{T}" />
    /// </summary>
    static abstract MdbColumnType AllowableType { get; }
#endif
}
