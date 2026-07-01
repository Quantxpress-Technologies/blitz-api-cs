using System.Text.Json.Serialization;

namespace BlitzConnect.Models;

public class DepthEntry
{
    [JsonPropertyName("price")]    public double Price { get; init; }
    [JsonPropertyName("quantity")] public long Quantity { get; init; }
    [JsonPropertyName("orders")]   public int Orders { get; init; }
}

public class DepthData
{
    [JsonPropertyName("instrumentId")] public long InstrumentId { get; init; }
    [JsonPropertyName("bid")]          public List<DepthEntry> Bid { get; init; } = [];
    [JsonPropertyName("ask")]          public List<DepthEntry> Ask { get; init; } = [];
}

public class DepthResponse : BlitzApiResponse<DepthData> { }
