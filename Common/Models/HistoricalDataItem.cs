using System.Text.Json.Serialization;

namespace BlitzConnect.Common.Models;

public class HistoricalDataItem
{
    [JsonPropertyName("open")]      public double Open { get; init; }
    [JsonPropertyName("high")]      public double High { get; init; }
    [JsonPropertyName("low")]       public double Low { get; init; }
    [JsonPropertyName("close")]     public double Close { get; init; }
    [JsonPropertyName("volume")]    public long Volume { get; init; }
    [JsonPropertyName("oi")]        public long Oi { get; init; }
    [JsonPropertyName("timestamp")] public string Timestamp { get; init; } = "";
}
