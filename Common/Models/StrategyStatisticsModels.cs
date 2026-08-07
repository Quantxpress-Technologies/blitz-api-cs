using System.Collections.Generic;
using System.Text.Json;

namespace BlitzConnect.Common.Models;

public class StrategyStatistics
{
    public string? StrategyId { get; init; }
    public string? StrategyName { get; init; }
    public string? StrategyInstanceId { get; init; }
    public string? StrategyInstanceName { get; init; }
    public string? ExchangeClientId { get; init; }
    public ulong InstrumentId { get; init; }
    public string? IVName { get; init; }
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
    public double OPBuyPrice { get; init; }
    public double OPSellPrice { get; init; }
    public double Exposure { get; init; }
    public double UnRealized { get; init; }
    public double Realized { get; init; }
    public double LastTradedPrice { get; init; }
    public double OpenPositionTradeValue { get; init; }
    public double TurnoverValue { get; init; }
    public double TurnoverValueOptions { get; init; }
    public string? EntityId { get; init; }
}

/// <summary>Wraps a strategy statistics list. The server returns a bare JSON array.</summary>
public class StrategyStatisticsResponse
{
    public List<StrategyStatistics> Data { get; init; } = [];
    public int Count => Data.Count;
}

/// <summary>Wraps the client-grouped strategy instance statistics response.</summary>
public class StrategyInstanceStatisticsResponse
{
    public Dictionary<string, List<StrategyStatistics>> Data { get; init; } = new();
    public int Count => Data.Count;
}
