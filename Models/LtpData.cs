using System.Text.Json.Serialization;

namespace BlitzConnect.Models;

public class LtpEntry
{
    [JsonPropertyName("instrumentId")] public long InstrumentId { get; init; }
    [JsonPropertyName("ltp")]          public double Ltp { get; init; }
}

public class LtpResponse : BlitzApiResponse<Dictionary<string, LtpEntry>> { }
