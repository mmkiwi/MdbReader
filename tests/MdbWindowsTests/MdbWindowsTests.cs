// This file is part of MdbReader. Licensed under the LGPL version 2.0.
// You should have received a coy of the GNU LGPL version along with this
// program. If not, see https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html
//
// Copyright Micah Makaiwi.
// Based on code from libmdb (https://github.com/mdbtools/mdbtools)

using System.Data;
using System.Data.OleDb;

using FluentAssertions;
using FluentAssertions.Execution;

using Xunit.Abstractions;

namespace MMKiwi.MdbReader.WindowsTests;

public sealed class MdbWindowsTests
{
    private ITestOutputHelper Output { get; }

    public MdbWindowsTests(ITestOutputHelper output)
    {
        Output = output;
    }

    [Theory]
    [ClassData(typeof(MdbTestDatabases))]
    public void TestFullEquivalency(string mdbPath, string tableName, string[] primaryKeys)
    {
        using (new AssertionScope())
        {
#if Debug
            Jet3Reader.SetDebugCallback(m => Output.WriteLine(m));
#endif

            using MdbConnection handle = MdbConnection.Open(mdbPath);

            using OleDbConnection connection = new($"Provider={(handle.JetVersion == JetVersion.Jet3 ? "Microsoft.Jet.OLEDB.4.0" : "Microsoft.ACE.OLEDB.16.0")};Data Source={mdbPath}");
            connection.Open();

            var mdbTable = handle.Tables[tableName];
            Output.WriteLine(mdbTable.Name);
            OleDbCommand command = new($"SELECT * FROM [{tableName}] {(primaryKeys.Any() ? $"ORDER BY [{string.Join("], [", primaryKeys)}]" : "")}", connection);
            using OleDbDataReader reader = command.ExecuteReader();

            IEnumerable<MdbDataRow> rows = mdbTable.Rows;
            foreach (var pk in primaryKeys)
            {
                if (rows is IOrderedEnumerable<MdbDataRow> orderedRows)
                    rows = orderedRows.ThenBy(r => r.GetValue(pk));
                else
                    rows = rows.OrderBy(r => r.GetValue(pk));
            }
            var mdbEnum = rows.GetEnumerator();

            while (reader.Read())
            {
                mdbEnum.MoveNext().Should().BeTrue("number of rows should be identical");

                object[] row = new object[reader.FieldCount];
                reader.GetValues(row);

                MdbDataRow mdbRow = mdbEnum.Current;

                foreach (var field in mdbTable.Columns)
                {

                    if (reader[field.Name] is DBNull)
                    {
                        mdbRow.IsNull(field.Name).Should().BeTrue();
                    }
                    else if (field.Type == MdbColumnType.Memo)
                    {
                        mdbRow.GetString(field.Name).Should().Be((string)reader[field.Name], $"{field.Name} should be identical");
                    }
                    else if (field.Type == MdbColumnType.OLE)
                    {
                        byte[] mdbLibVal = mdbRow.GetBytes(field.Name);
                        int index = reader.GetOrdinal(field.Name);
                        using Stream oleStream = reader.GetStream(index);
                        mdbLibVal.LongLength.Should().Be(oleStream.Length);
                        byte[] oleVal = new byte[mdbLibVal.Length];
                        oleStream.ReadExactly(oleVal.AsSpan());
                        mdbLibVal.Should().BeEquivalentTo(oleVal, $"{field.Name} should be identical");
                    }
                    else if (field.Type == MdbColumnType.Binary)
                    {
                        mdbRow.GetBytes(field.Name).Should().BeEquivalentTo((byte[])reader[field.Name], $"{field.Name} should be identical");
                    }
                    else
                    {
                        mdbRow.GetValue(field.Name).Should().Be(reader[field.Name], $"{field.Name} should be identical");
                    }
                }
            }
        }
    }
}
