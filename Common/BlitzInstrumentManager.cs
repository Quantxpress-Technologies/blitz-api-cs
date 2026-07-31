using System.IO.Compression;
using System.Text.Json;
using BlitzConnect.Common.Models;

namespace BlitzConnect.Common;

public class BlitzInstrumentManager
{
    private readonly Dictionary<string, long> _instruments = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<long, InstrumentDetail> _byId = new();
    private readonly Dictionary<string, InstrumentDetail> _bySymbol = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<InstrumentDetail> _all = [];

    public int Count => _instruments.Count;

    public async Task LoadInstrumentsAsync(string url, string? accessToken = null, CancellationToken ct = default)
    {
        using var http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        });

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(accessToken))
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        var response = await http.SendAsync(req, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = await JsonSerializer.DeserializeAsync<List<InstrumentDetail>>(gzip, options, ct);

        _instruments.Clear();
        _byId.Clear();
        _bySymbol.Clear();
        _all.Clear();
        if (data != null)
        {
            _all.AddRange(data);
            foreach (var entry in data)
            {
                _byId[entry.InstrumentId] = entry;
                if (!string.IsNullOrEmpty(entry.Symbol))
                    _instruments[$"{entry.ExchangeSegment}|{entry.Symbol}"] = entry.InstrumentId;
                if (!string.IsNullOrEmpty(entry.InstrumentName) &&
                    !string.Equals(entry.InstrumentName, entry.Symbol, StringComparison.OrdinalIgnoreCase))
                    _instruments[$"{entry.ExchangeSegment}|{entry.InstrumentName}"] = entry.InstrumentId;
                if (!string.IsNullOrEmpty(entry.Symbol))
                    _bySymbol[entry.Symbol] = entry;
                if (!string.IsNullOrEmpty(entry.InstrumentName) &&
                    !string.Equals(entry.InstrumentName, entry.Symbol, StringComparison.OrdinalIgnoreCase))
                    _bySymbol[entry.InstrumentName] = entry;
            }
        }
    }

    public bool TryGetInstrumentId(string key, out long id) =>
        _instruments.TryGetValue(key, out id);

    public bool TryGetLotSize(long instrumentId, out int lotSize)
    {
        if (_byId.TryGetValue(instrumentId, out var entry))
        {
            lotSize = entry.LotSize;
            return true;
        }
        lotSize = 0;
        return false;
    }

    public InstrumentDetail? GetById(long instrumentId) =>
        _byId.GetValueOrDefault(instrumentId);

    public InstrumentDetail? GetBySymbol(string symbol)
    {
        if (_bySymbol.TryGetValue(symbol, out var detail)) return detail;
        if (_instruments.TryGetValue(symbol, out var id)) return _byId.GetValueOrDefault(id);
        return null;
    }

    public IReadOnlyList<InstrumentDetail> GetAll() => _all;
}
