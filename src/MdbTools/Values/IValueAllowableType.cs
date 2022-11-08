// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

namespace MMKiwi.MdbTools.Values;

#if NET7_0_OR_GREATER
/// <summary>
/// This interface defined the static <see cref="AllowableType" /> property which specifies which
/// <see cref="ColumnType">ColumnTypes</see> are allowed for a given <see cref="IMdbValue"/>
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
public interface IValueAllowableType
{
#if NET7_0_OR_GREATER
    /// <summary>
    /// The <see cref="ColumnType" /> that can be used for this
    /// <see cref="MdbValue{T}" />
    /// </summary>
    static abstract ColumnType AllowableType { get; }
#endif
}
