using Microsoft.Extensions.Configuration;

namespace MMKiwi.MdbTools.Tests;

public static class TestOptionsFactory
{
    static TestOptionsFactory()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        MdbTestPath = config["Data:TestMdbPath"];
    }

    public static string MdbTestPath { get; }

}