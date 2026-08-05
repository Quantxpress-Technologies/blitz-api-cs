using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BlitzConnect.Common;

static class TestContext
{
    public static BlitzApiClient Client = null!;
    public static BlitzConfig Config = null!;
    public static TestConfig Cfg = null!;
    public static int Pass;
    public static int Fail;
    static StreamWriter _logWriter = null!;

    public static void Log(string line)
    {
        System.Console.WriteLine(line);
        _logWriter.WriteLine(line);
    }

    public static void Test(string name, System.Action action)
    {
        try
        {
            action();
            Log($"  [PASS] {name}");
            Interlocked.Increment(ref Pass);
        }
        catch (System.Exception ex)
        {
            Log($"  [FAIL] {name}: {ex.GetType().Name}: {ex.Message}");
            Interlocked.Increment(ref Fail);
        }
    }

    public static void TestAsync(string name, System.Func<Task> action) =>
        Test(name, () => action().GetAwaiter().GetResult());

    public static async Task InitAsync()
    {
        var rootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var logPath = Path.Combine(rootDir, $"blitz-test-{System.DateTime.Now:yyyyMMdd-HHmmss}.log");
        _logWriter = new StreamWriter(logPath, append: false, encoding: Encoding.UTF8) { AutoFlush = true };

        var jsonPath = Path.Combine(rootDir, "test-config.json");
        var jsonText = File.ReadAllText(jsonPath);
        Cfg = JsonSerializer.Deserialize<TestConfig>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
              ?? throw new System.Exception("Failed to parse test-config.json");

        var envPath = Path.Combine(rootDir, ".env");
        var envVars = new Dictionary<string, string>();
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
                var eq = trimmed.IndexOf('=');
                if (eq > 0)
                    envVars[trimmed[..eq].Trim()] = trimmed[(eq + 1)..].Trim();
            }
        }

        string Env(string key, string fallback) => envVars.GetValueOrDefault(key, fallback);

        var conn = Cfg.Connection;
        Config = new BlitzConfig
        {
            MarketDataApiUrl = Env("MD_API_URL", conn.MarketDataApiUrl ?? ""),
            AuthBaseUrl = Env("AUTH_BASE_URL", conn.AuthBaseUrl ?? ""),
            OrderBaseUrl = Env("ORDER_BASE_URL", conn.OrderBaseUrl ?? ""),
            InteractiveWsUrl = Env("INTERACTIVE_WS_URL", conn.InteractiveWsUrl ?? ""),
            MarketDataWsUrl = Env("MD_WS_URL", conn.MarketDataWsUrl ?? ""),
            InstrumentGzUrl = Env("INSTRUMENT_GZ_URL", conn.InstrumentGzUrl ?? ""),
            AppKey = Env("APP_KEY", conn.AppKey ?? ""),
            UserId = Env("USER_ID", conn.UserId ?? ""),
            ClientId = Env("CLIENT_ID", conn.ClientId ?? ""),
        };

        Client = new BlitzApiClient(Config);

        Log($"Log file: {logPath}");
        Log("╔══════════════════════════════════════════════╗");
        Log("║     BlitzConnect API Test Suite              ║");
        Log("╚══════════════════════════════════════════════╝");
        Log($"  MD API: {Config.MarketDataApiUrl}");
        Log($"  Order API: {Config.OrderBaseUrl}");
        Log($"  Auth API: {Config.AuthBaseUrl}");
        Log($"  Interactive WS: {Config.InteractiveWsUrl}");
        Log($"  Market Data WS: {Config.MarketDataWsUrl}");
        Log($"  Instrument Gz: {Config.InstrumentGzUrl}");
        Log($"  AppKey: {Config.AppKey[..System.Math.Min(20, Config.AppKey.Length)]}...");
        Log($"  UserId: {Config.UserId}");
        Log(string.Empty);

        Log("── Authentication ──────────────────────────────");
        await Client.LoginAsync();
        Log("       login OK");
        Log(string.Empty);
    }

    public static void Summary()
    {
        Log(string.Empty);
        Log("══════════════════════════════════════════════════");
        Log($"RESULTS: {Pass} passed, {Fail} failed");
        Log("══════════════════════════════════════════════════");
    }
}
