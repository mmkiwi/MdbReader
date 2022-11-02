using MMKiwi.MdbTools;

namespace MMKiwi.MdbTools.Tests;

public class PrelimTests
{
    [Fact]
    public async Task Test1()
    {
        await using MdbHandle handle = MdbHandle.Open(TestOptionsFactory.MdbTestPath);
        var table = await handle.GetTablesAsync();
        foreach(var col in table.First().Columns)
        {
            Console.WriteLine(col.Name);
        }
    }
}
