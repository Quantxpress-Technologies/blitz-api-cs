using System.Text.Json.Serialization;

namespace BlitzConnect.Models;

public class PlaceOrderRequest
{
    [JsonPropertyName("correlationOrderId")] public string CorrelationOrderId { get; set; } = "";
    [JsonPropertyName("quantity")]           public int Quantity { get; set; }
    [JsonPropertyName("product")]            public string Product { get; set; } = "";
    [JsonPropertyName("tif")]                public string Tif { get; set; } = "";
    [JsonPropertyName("price")]              public double Price { get; set; }
    [JsonPropertyName("orderType")]          public string OrderType { get; set; } = "";
    [JsonPropertyName("orderSide")]          public string OrderSide { get; set; } = "";
    [JsonPropertyName("disclosedQuantity")]  public int DisclosedQuantity { get; set; }
    [JsonPropertyName("stopPrice")]          public double StopPrice { get; set; }
    [JsonPropertyName("clientId")]           public string ClientId { get; set; } = "";
    [JsonPropertyName("tiF_GTD_Date")]       public string TifGtdDate { get; set; } = "";
    [JsonPropertyName("instrumentId")]       public long? InstrumentId { get; set; }
    [JsonPropertyName("symbol")]             public string? Symbol { get; set; }
}

public class ModifyOrderRequest
{
    [JsonPropertyName("blitzOrderId")]         public long BlitzOrderId { get; set; }
    [JsonPropertyName("modifiedOrderQuantity")]public int ModifiedOrderQuantity { get; set; }
    [JsonPropertyName("price")]                public double Price { get; set; }
    [JsonPropertyName("orderType")]            public string OrderType { get; set; } = "";
    [JsonPropertyName("tif")]                  public string Tif { get; set; } = "";
    [JsonPropertyName("disclosedQuantity")]    public int DisclosedQuantity { get; set; }
    [JsonPropertyName("stopPrice")]            public double StopPrice { get; set; }
    [JsonPropertyName("tiF_GTD_Date")]         public string TifGtdDate { get; set; } = "";
    [JsonPropertyName("instrumentId")]         public long? InstrumentId { get; set; }
    [JsonPropertyName("symbol")]               public string? Symbol { get; set; }
}

public class CancelOrderRequest
{
    [JsonPropertyName("blitzOrderId")]   public long BlitzOrderId { get; set; }
    [JsonPropertyName("instrumentId")]   public long? InstrumentId { get; set; }
    [JsonPropertyName("symbol")]         public string? Symbol { get; set; }
}

public class OrderEntry
{
    [JsonPropertyName("blitzOrderId")] public long BlitzOrderId { get; init; }
}

public class SignalRequest
{
    public string SourceStrategy { get; set; } 
    public string DestinationStrategy { get; set; }
    public string SourceSID { get; set; }
    public string InstanceRunningMode { get; set; }
    public string GlobalAction { get; set; }
    public List<SignalInstrument> Instruments { get; set; } = new();
}

public class SignalInstrument
{
    public string ExchangeSegment { get; set; } 
    public string InstrumentName { get; set; } 
    public string Action { get; set; } 
    public string Lot { get; set; }
    public string TimeStamp { get; set; }
    public string InfoText { get; set; } 
}
public class OrdersResponse : BlitzApiResponse<List<OrderEntry>> { }

public class PositionsResponse : BlitzApiResponse<List<object>> { }

public class TradesResponse : BlitzApiResponse<List<object>> { }
