using System.IO.Compression;
using System.Text.Json;

namespace BlitzConnect.Common;

public class BlitzInstrumentManager
{
    private readonly Dictionary<string, long> _instruments = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<long, InstrumentEntry> _byId = new();

    public int Count => _instruments.Count;

    public async Task LoadInstrumentsAsync(string url)
    {
        using var http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        });
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = await JsonSerializer.DeserializeAsync<List<InstrumentEntry>>(gzip, options);

        _instruments.Clear();
        _byId.Clear();
        if (data != null)
        {
            foreach (var entry in data)
            {
                _byId[entry.InstrumentId] = entry;
                if (!string.IsNullOrEmpty(entry.Symbol))
                    _instruments[$"{entry.ExchangeSegment}|{entry.Symbol}"] = entry.InstrumentId;
                if (!string.IsNullOrEmpty(entry.InstrumentName) &&
                    !string.Equals(entry.InstrumentName, entry.Symbol, StringComparison.OrdinalIgnoreCase))
                    _instruments[$"{entry.ExchangeSegment}|{entry.InstrumentName}"] = entry.InstrumentId;
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

    private class InstrumentEntry
    {
        public long InstrumentId { get; set; }
        public string? Symbol { get; set; }
        public string? ExchangeSegment { get; set; }
        public string? InstrumentName { get; set; }
        public int LotSize { get; set; }
    }
}
