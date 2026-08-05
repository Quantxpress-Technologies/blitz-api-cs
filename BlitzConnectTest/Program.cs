using System;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var suite = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

        if (suite is "help" or "--help" or "-h")
        {
            Console.WriteLine("Usage: dotnet run -- <suite>");
            Console.WriteLine();
            Console.WriteLine("  interactive-api    test Interactive REST APIs");
            Console.WriteLine("  marketdata-api     test Market Data REST APIs");
            Console.WriteLine("  interactive-ws     test Interactive WebSocket");
            Console.WriteLine("  marketdata-ws      test Market Data WebSocket (Ctrl+C to stop)");
            Console.WriteLine("  all                run all API tests (interactive + market data)");
            Console.WriteLine();
            return 0;
        }

        await TestContext.InitAsync();

        switch (suite)
        {
            case "interactive-api":
                return await InteractiveApiTests.RunAsync();
            case "marketdata-api":
                return await MarketDataApiTests.RunAsync();
            case "interactive-ws":
                return await InteractiveWsTests.RunAsync();
            case "marketdata-ws":
                return await MarketDataWsTests.RunAsync();
            case "all":
                TestContext.Log("── Running all API tests ─────────────────────");
                TestContext.Log(string.Empty);
                var a = await MarketDataApiTests.RunAsync();
                TestContext.Log(string.Empty);
                var b = await InteractiveApiTests.RunAsync();
                return a + b;
            default:
                Console.WriteLine($"Unknown suite: {suite}");
                Console.WriteLine();
                return await Main(new[] { "help" });
        }
    }
}
