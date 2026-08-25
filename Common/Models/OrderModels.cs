using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlitzConnect.Common.Models;

public class PlaceOrderRequest
{
    public string CorrelationOrderId { get; set; } = "";
    public int Quantity { get; set; }
    public string Product { get; set; } = "";
    [JsonPropertyName("TIF")] public string Tif { get; set; } = "";
    public double Price { get; set; }
    public string OrderType { get; set; } = "";
    public string OrderSide { get; set; } = "";
    public int DisclosedQuantity { get; set; }
    public double StopPrice { get; set; }
    public string ClientId { get; set; } = "";
    [JsonPropertyName("TiF_GTD_Date")] public string TifGtdDate { get; set; } = "";
    public string? ExchangeSegment { get; set; }
    public long? InstrumentId { get; set; }
    public string? Symbol { get; set; }
}

public class ModifyOrderRequest
{
    public long BlitzOrderId { get; set; }
    public int ModifiedOrderQuantity { get; set; }
    public double Price { get; set; }
    public string OrderType { get; set; } = "";
    [JsonPropertyName("TIF")] public string Tif { get; set; } = "";
    public int DisclosedQuantity { get; set; }
    public double StopPrice { get; set; }
    [JsonPropertyName("TiF_GTD_Date")] public string TifGtdDate { get; set; } = "";
    public string? ExchangeSegment { get; set; }
    public long? InstrumentId { get; set; }
    public string? Symbol { get; set; }
}

public class CancelOrderRequest
{
    public long BlitzOrderId { get; set; }
    public string? ExchangeSegment { get; set; }
    public long? ExchangeInstrumentId { get; set; }
    public long? InstrumentId { get; set; }
    public string? Symbol { get; set; }
}

public class OrderEntry
{
    public long BlitzOrderId { get; init; }
}

public class SignalRequest
{
    public string? ID { get; set; }
    public string SourceStrategy { get; set; } = "";
    public string DestinationStrategy { get; set; } = "";
    public string? SL { get; set; }
    public string SourceSID { get; set; } = "";
    public string InstanceRunningMode { get; set; } = "";
    public string GlobalAction { get; set; } = "";
    public List<SignalInstrument> Instruments { get; set; } = new();
}

public class SignalInstrument
{
    public string ExchangeSegment { get; set; } = "";
    public string InstrumentName { get; set; } = "";
    public string Action { get; set; } = "";
    public string Lot { get; set; } = "";
    public string TimeStamp { get; set; } = "";
    public string InfoText { get; set; } = "";
}
public class PlaceOrderData
{
    public long BlitzOrderId { get; init; }
    public string? CorrelationOrderId { get; init; }
}

/// <summary>Envelope returned by the modify-order endpoint. Data carries a server message string.</summary>
public class ModifyOrderResponse
{
    public string Status { get; init; } = "";
    public string? Message { get; init; }
    public string? Data { get; init; }
}

/// <summary>Wraps an order list response. The server returns a bare JSON array.</summary>
public class OrdersResponse
{
    public List<OrderEntry> Data { get; init; } = [];
    public int Count => Data.Count;
}

/// <summary>Wraps a trades response. The server returns a bare JSON array.</summary>
public class TradesResponse
{
    public List<JsonElement> Data { get; init; } = [];
    public int Count => Data.Count;
}

/// <summary>Standard gateway envelope returned by write operations.</summary>
public class GatewayResponse
{
    public string? Status { get; init; }
    public string? Message { get; init; }
    public JsonElement? Data { get; init; }
}
