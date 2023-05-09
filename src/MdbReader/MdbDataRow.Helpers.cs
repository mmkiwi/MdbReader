// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Diagnostics.CodeAnalysis;

using MMKiwi.Collections;
using MMKiwi.MdbReader.Values;

namespace MMKiwi.MdbReader;

public sealed partial class MdbDataRow
{
    private IMdbValue GetFieldValue(int index)
    {
        if (index < 0 || index >= Fields.Count)
            throw new IndexOutOfRangeException($"Index {index} was out of range. Must be non-negative and less than the size of the collection.");
        return Fields[index];
    }

    private IMdbValue GetFieldValue(string columnName)
    {
        if (columnName is null)
            throw new ArgumentNullException(nameof(columnName));
        if (!Fields.Contains(columnName))
            throw new IndexOutOfRangeException($"Column {columnName} does not exist.");
        return Fields[columnName];
    }

    private static void ThrowIfNullCast(IMdbValue fieldValue, string typeName)
    {
        if (fieldValue.IsNull)
            throw new InvalidCastException($"Cannot convert null value to {typeName}");
    }

    [DoesNotReturn]
    private static T ThrowInvalidCast<T>(IMdbValue fieldValue, string typeName)
    {
        throw new InvalidCastException($"Could not convert {fieldValue.Column.Type} value to {typeName}");
    }

    private class FieldCollection : ImmutableKeyedCollection<string, IMdbValue>
    {
        internal FieldCollection(ImmutableArray<IMdbValue> baseCollection, IEqualityComparer<string>? comparer, int dictionaryCreationThreshold) : base(baseCollection, comparer, dictionaryCreationThreshold)
        {
        }

        /// <inheritdocs />
        protected override string GetKeyForItem(IMdbValue item) => item.Column.Name;
    }
}