using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlitzConnect.Common.Models;

public class PlaceOrderRequest
{
    public string CorrelationOrderId { get; set; } = "";
    public int Quantity { get; set; }
    public string Product { get; set; } = "";
    [JsonPropertyName("TIF")] public string Tif { get; set; } = "GFD";
    public double Price { get; set; }
    public string OrderType { get; set; } = "";
    public string OrderSide { get; set; } = "";
    public int DisclosedQuantity { get; set; }
    public double StopPrice { get; set; }
    public string ClientId { get; set; } = "";
    [JsonPropertyName("TiF_GTD_Date")] public string TifGtdDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public long? InstrumentId { get; set; }
    public long? ExchangeInstrumentId { get; set; }
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
    [JsonPropertyName("TiF_GTD_Date")] public string TifGtdDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    public long? InstrumentId { get; set; }
    public long? ExchangeInstrumentId { get; set; }
    public string? Symbol { get; set; }
}

public class CancelOrderRequest
{
    public long BlitzOrderId { get; set; }
    public long? InstrumentId { get; set; }
    public string? Symbol { get; set; }
}

public class OrderEntry
{
    public long Id { get; init; }
    public string? CorrelationOrderId { get; init; }
    public string? EntityId { get; init; }
    public string? StrategyId { get; init; }
    public string? StrategyInstanceId { get; init; }
    public string? StrategyInstanceName { get; init; }
    public string? StrategyName { get; init; }
    public string? IVObjectName { get; init; }
    public long InstrumentId { get; init; }
    public string? ExchangeSegment { get; init; }
    public long ExchangeInstrumentId { get; init; }
    public string? InstrumentName { get; init; }
    public string? InstrumentType { get; init; }
    public long BlitzOrderId { get; init; }
    public string? ExchangeOrderId { get; init; }
    public string? ExecutionId { get; init; }
    public string? Account { get; init; }
    public string? ClientId { get; init; }
    public string? OrderType { get; init; }
    public string? OrderSide { get; init; }
    public string? OrderStatus { get; init; }
    public int OrderQuantity { get; init; }
    public double OrderPrice { get; init; }
    public double OrderStopPrice { get; init; }
    public double OrderTriggerPrice { get; init; }
    public int LastTradedQuantity { get; init; }
    public double LastTradedPrice { get; init; }
    public int CumulativeQuantity { get; init; }
    public int LeavesQuantity { get; init; }
    public string? TIF { get; init; }
    public long OrderExpiryDate { get; init; }
    public int OrderDisclosedQuantity { get; init; }
    public int MinimumQuantity { get; init; }
    public long OrderGeneratedDateTime { get; init; }
    public long LastRequestDateTime { get; init; }
    public long ExchangeTransactTime { get; init; }
    public int OrderModificationCount { get; init; }
    public int OrderTradeCount { get; init; }
    public double AverageTradedPrice { get; init; }
    public double AverageTradedValue { get; init; }
    public bool IsFictiveOrder { get; init; }
    public string? RejectType { get; init; }
    public string? RejectTypeReason { get; init; }
    public string? OrderTag { get; init; }
    public string? CTCLId { get; init; }
    public string? AlgoId { get; init; }
    public string? AlgoCategoryId { get; init; }
    public string? ClearingFirmId { get; init; }
    public string? PANId { get; init; }
    public bool IsOrderCompleted { get; init; }
    public string? UserText { get; init; }
    public string? ExecutionType { get; init; }
    public string? StrategyTag { get; init; }
    public long SequenceNumber { get; init; }
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
