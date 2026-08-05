using System.Collections.Generic;

// ── Config classes for test-config.json ─────────────────────────────

class TestConfig
{
    public TestConnection Connection { get; set; } = new();
    public List<TestInstrument> Instruments { get; set; } = [];
    public TestMarketData MarketData { get; set; } = new();
    public TestPlaceOrder PlaceOrder { get; set; } = new();
    public TestModifyOrder ModifyOrder { get; set; } = new();
    public TestCancelOrder CancelOrder { get; set; } = new();
    public TestSignal Signal { get; set; } = new();
}

class TestConnection
{
    public string? MarketDataApiUrl { get; set; }
    public string? AuthBaseUrl { get; set; }
    public string? OrderBaseUrl { get; set; }
    public string? InteractiveWsUrl { get; set; }
    public string? MarketDataWsUrl { get; set; }
    public string? InstrumentGzUrl { get; set; }
    public string? AppKey { get; set; }
    public string? UserId { get; set; }
    public string? ClientId { get; set; }
}

class TestInstrument
{
    public long Id { get; set; }
    public string Symbol { get; set; } = "";
}

class TestMarketData
{
    public List<string> Symbols { get; set; } = [];
    public string OptionChainSymbol { get; set; } = "";
    public string OptionChainExpiry { get; set; } = "";
    public string OptionChainRequest { get; set; } = "";
    public int OptionChainExchangeSegment { get; set; }
    public long OptionChainInstrumentId { get; set; }
    public List<string> OptionChainFallbackSymbols { get; set; } = [];
    public string RelianceOptionChainSymbol { get; set; } = "";
    public string RelianceOptionChainExpiry { get; set; } = "";
    public string HistoricalSymbol { get; set; } = "";
    public string HistoricalInterval { get; set; } = "";
    public List<long> InstrumentIds { get; set; } = [];
    public int WsTimeoutSeconds { get; set; } = 15;
}

class TestPlaceOrder
{
    public int Quantity { get; set; }
    public string Product { get; set; } = "";
    public string Tif { get; set; } = "";
    public double Price { get; set; }
    public string OrderType { get; set; } = "";
    public string OrderSide { get; set; } = "";
    public int DisclosedQuantity { get; set; }
    public double StopPrice { get; set; }
    public long InstrumentId { get; set; }
    public string ClientId { get; set; } = "";
}

class TestModifyOrder
{
    public long BlitzOrderId { get; set; }
    public int ModifiedOrderQuantity { get; set; }
    public double Price { get; set; }
    public string OrderType { get; set; } = "";
    public string Tif { get; set; } = "";
    public int DisclosedQuantity { get; set; }
    public double StopPrice { get; set; }
    public long InstrumentId { get; set; }
    public string Symbol { get; set; } = "";
}

class TestCancelOrder
{
    public long BlitzOrderId { get; set; }
    public long InstrumentId { get; set; }
}

class TestSignal
{
    public string SourceStrategy { get; set; } = "";
    public string DestinationStrategy { get; set; } = "";
    public string SourceSID { get; set; } = "";
    public string InstanceRunningMode { get; set; } = "";
    public string GlobalAction { get; set; } = "";
    public string ExchangeSegment { get; set; } = "";
    public string InstrumentName { get; set; } = "";
    public string Action { get; set; } = "";
    public string Lot { get; set; } = "";
    public string InfoText { get; set; } = "";
    public string BaseTime { get; set; } = "";
}
