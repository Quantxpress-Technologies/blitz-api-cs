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
    public static long? LastPlacedBlitzOrderId;
    static StreamWriter _logWriter = null!;

    public static void Log(string line)
    {
        System.Console.WriteLine(line);
        _logWriter.WriteLine(line);
    }

    public static void Raw(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) { Log("       RAW: (empty)"); return; }
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            Log("       RAW: " + System.Text.Json.JsonSerializer.Serialize(doc.RootElement, new System.Text.Json.JsonSerializerOptions { WriteIndented = false }));
        }
        catch
        {
            Log($"       RAW: {json}");
        }
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

    public static async Task RunAsync(string name, System.Func<Task> action)
    {
        try
        {
            await action();
            Log($"  [PASS] {name}");
            Interlocked.Increment(ref Pass);
        }
        catch (System.Exception ex)
        {
            Log($"  [FAIL] {name}: {ex.GetType().Name}: {ex.Message}");
            Interlocked.Increment(ref Fail);
        }
    }

    public static async Task InitAsync()
    {
        var rootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        var logsDir = Path.Combine(rootDir, "logs");
        Directory.CreateDirectory(logsDir);
        var logPath = Path.Combine(logsDir, $"blitz-test-{System.DateTime.Now:yyyyMMdd-HHmmss}.log");
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

        string Env(string key, string fallback) =>
            System.Environment.GetEnvironmentVariable(key)
            ?? envVars.GetValueOrDefault(key, fallback);

        var conn = Cfg.Connection;
        var fallback = new BlitzConfig();

        // If a pre-issued access token is supplied, use it directly instead of logging in.
        string envToken = Env("ACCESS_TOKEN", "");
        string envUserId = Env("USER_ID", conn.UserId ?? "");
        string envClientId = Env("CLIENT_ID", conn.ClientId ?? "");

        string Or(string value, string fallbackValue) =>
            string.IsNullOrWhiteSpace(value) ? fallbackValue : value;

        Config = new BlitzConfig
        {
            MarketDataApiUrl = Or(Env("MD_API_URL", conn.MarketDataApiUrl ?? ""), fallback.MarketDataApiUrl),
            AuthBaseUrl = Or(Env("AUTH_BASE_URL", conn.AuthBaseUrl ?? ""), fallback.AuthBaseUrl),
            OrderBaseUrl = Or(Env("ORDER_BASE_URL", conn.OrderBaseUrl ?? ""), fallback.OrderBaseUrl),
            InteractiveWsUrl = Or(Env("INTERACTIVE_WS_URL", conn.InteractiveWsUrl ?? ""), fallback.InteractiveWsUrl),
            MarketDataWsUrl = Or(Env("MD_WS_URL", conn.MarketDataWsUrl ?? ""), fallback.MarketDataWsUrl),
            InstrumentBaseUrl = Or(Env("INSTRUMENT_BASE_URL", conn.InstrumentBaseUrl ?? ""), fallback.InstrumentBaseUrl),
            InstrumentGzUrl = Or(Env("INSTRUMENT_GZ_URL", conn.InstrumentGzUrl ?? ""), fallback.InstrumentGzUrl),
            AppKey = Env("APP_KEY", conn.AppKey ?? ""),
            UserId = envUserId,
            ClientId = envClientId,
            HeartbeatIntervalSeconds = Cfg.Connection.HeartbeatIntervalSeconds > 0
                ? Cfg.Connection.HeartbeatIntervalSeconds
                : fallback.HeartbeatIntervalSeconds,
        };

        // Write the effective values back into the test config, so individual
        // tests that read Cfg.Connection (e.g. order clientId) see the same
        // env-resolved values the SDK uses - not the blanked JSON defaults.
        Cfg.Connection.MarketDataApiUrl = Config.MarketDataApiUrl;
        Cfg.Connection.AuthBaseUrl = Config.AuthBaseUrl;
        Cfg.Connection.OrderBaseUrl = Config.OrderBaseUrl;
        Cfg.Connection.InteractiveWsUrl = Config.InteractiveWsUrl;
        Cfg.Connection.MarketDataWsUrl = Config.MarketDataWsUrl;
        Cfg.Connection.InstrumentBaseUrl = Config.InstrumentBaseUrl;
        Cfg.Connection.InstrumentGzUrl = Config.InstrumentGzUrl;
        Cfg.Connection.AppKey = Config.AppKey;
        Cfg.Connection.UserId = Config.UserId;
        Cfg.Connection.ClientId = Config.ClientId;

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
        Log(string.IsNullOrEmpty(Config.AppKey)
            ? "  AppKey: (blank - set APP_KEY env var or connection.appKey)"
            : $"  AppKey: {Config.AppKey[..System.Math.Min(20, Config.AppKey.Length)]}...");
        Log($"  UserId: {Config.UserId}");
        Log(string.Empty);

        if (!string.IsNullOrWhiteSpace(envToken))
        {
            Log("── Authentication ──────────────────────────────");
            Client.SetToken(envToken);
            Log("       access token injected (no login)");
            Log(string.Empty);
            return;
        }

        Log("── Authentication ──────────────────────────────");
        await Client.LoginAsync();
        Log("       login OK");
        Raw(Client.LastLoginRawResponse ?? "");
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
