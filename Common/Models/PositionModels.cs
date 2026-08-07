using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlitzConnect.Common.Models;

public class Position
{
    public string? EntityId { get; init; }
    public string? ClientId { get; init; }
    public string? RMSEntityCode { get; init; }
    public ulong InstrumentId { get; init; }
    public string? ExchangeSegment { get; init; }
    public string? InstrumentName { get; init; }
    public int NonAckBuyQuantity { get; init; }
    public int NonAckSellQuantity { get; init; }
    public int OpenBuyQuantity { get; init; }
    public int OpenSellQuantity { get; init; }
    public int ShortPosition { get; init; }
    public int LongPosition { get; init; }
    public int NetPosition { get; init; }
    public int NetPositionInLot { get; init; }
    public int OrderCount { get; init; }
    public int TradeCount { get; init; }
    public int PreviousNetPosition { get; init; }
    public double TradeBuyValue { get; init; }
    public double TradeSellValue { get; init; }
    public double TradeNetValue { get; init; }
    public double AvgBuy { get; init; }
    public double AvgSell { get; init; }
    public double AvgNet { get; init; }
    public double Exposure { get; init; }
    public double UnRealized { get; init; }
    public double Realized { get; init; }
    public double LastTradedPrice { get; init; }
    public double OpenPositionTradeValue { get; init; }
    public double TurnoverValue { get; init; }
    public double TurnoverValueOptions { get; init; }
}

/// <summary>Wraps a positions response. The server returns a dictionary grouped by client.</summary>
public class PositionsResponse
{
    public Dictionary<string, List<Position>> Data { get; init; } = new();
    public int Count => Data.Count;
}
