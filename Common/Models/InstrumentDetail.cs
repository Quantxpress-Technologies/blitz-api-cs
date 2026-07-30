using System.Text.Json.Serialization;

namespace BlitzConnect.Common.Models;

public class InstrumentDetail
{
    [JsonPropertyName("instrumentId")]   public long InstrumentId { get; init; }
    [JsonPropertyName("exchange")]       public string Exchange { get; init; } = "";
    [JsonPropertyName("symbol")]         public string Symbol { get; init; } = "";
    [JsonPropertyName("ticker")]         public string Ticker { get; init; } = "";
    [JsonPropertyName("exchangeSegment")]public string ExchangeSegment { get; init; } = "";
    [JsonPropertyName("instrumentType")] public string InstrumentType { get; init; } = "";
    [JsonPropertyName("instrumentName")] public string InstrumentName { get; init; } = "";
    [JsonPropertyName("exchangeInstrumentId")] public int ExchangeInstrumentId { get; init; }
    [JsonPropertyName("marketInstrumentId")]  public long MarketInstrumentId { get; init; }
    [JsonPropertyName("series")]         public string? Series { get; init; }
    [JsonPropertyName("tickSize")]       public double TickSize { get; init; }
    [JsonPropertyName("isin")]           public string? Isin { get; init; }
    [JsonPropertyName("lotSize")]        public int LotSize { get; init; }
    [JsonPropertyName("expiryDate")]     public string? ExpiryDate { get; init; }
    [JsonPropertyName("strikePrice")]    public double? StrikePrice { get; init; }
    [JsonPropertyName("optionType")]     public string? OptionType { get; init; }
    [JsonPropertyName("open")]           public double Open { get; init; }
    [JsonPropertyName("high")]           public double High { get; init; }
    [JsonPropertyName("low")]            public double Low { get; init; }
    [JsonPropertyName("close")]          public double Close { get; init; }
    [JsonPropertyName("ltp")]            public double Ltp { get; init; }
    [JsonPropertyName("freezeQty")]      public int FreezeQty { get; init; }
    [JsonPropertyName("multiplier")]     public int Multiplier { get; init; }
}
