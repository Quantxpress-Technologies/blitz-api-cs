using System.Text.Json.Serialization;

namespace BlitzConnect.Models;

public class MarketQuoteEntry
{
    [JsonPropertyName("instrumentId")]         public long InstrumentId { get; init; }
    [JsonPropertyName("exchangeSegment")]      public int ExchangeSegment { get; init; }
    [JsonPropertyName("exchangeInstrumentId")] public int ExchangeInstrumentId { get; init; }
    [JsonPropertyName("instrumentName")]       public string InstrumentName { get; init; } = "";
    [JsonPropertyName("timestamp")]            public long Timestamp { get; init; }
    [JsonPropertyName("ltp")]                  public double Ltp { get; init; }
    [JsonPropertyName("ltq")]                  public int Ltq { get; init; }
    [JsonPropertyName("ltt")]                  public long Ltt { get; init; }
    [JsonPropertyName("atp")]                  public double Atp { get; init; }
    [JsonPropertyName("vtt")]                  public long Vtt { get; init; }
    [JsonPropertyName("oi")]                   public long Oi { get; init; }
    [JsonPropertyName("open")]                 public double Open { get; init; }
    [JsonPropertyName("high")]                 public double High { get; init; }
    [JsonPropertyName("low")]                  public double Low { get; init; }
    [JsonPropertyName("close")]                public double Close { get; init; }
}

public class MarketQuoteResponse : BlitzApiResponse<Dictionary<string, MarketQuoteEntry>> { }
