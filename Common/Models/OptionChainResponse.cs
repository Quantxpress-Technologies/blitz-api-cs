using System.Text.Json.Serialization;

namespace BlitzConnect.Common.Models;

public class OptionChainData
{
    [JsonPropertyName("spotPrice")]  public double SpotPrice { get; init; }
    [JsonPropertyName("expiryDate")] public string ExpiryDate { get; init; } = "";
    [JsonPropertyName("atm")]        public double Atm { get; init; }
    [JsonPropertyName("chains")]     public List<OptionChainEntry> Chains { get; init; } = [];
}

public class OptionChainEntry
{
    [JsonPropertyName("strikePrice")] public double StrikePrice { get; init; }
    [JsonPropertyName("callOption")]  public OptionGreeks? CallOption { get; init; }
    [JsonPropertyName("putOption")]   public OptionGreeks? PutOption { get; init; }
}

public class OptionGreeks
{
    [JsonPropertyName("gamma")]       public double Gamma { get; init; }
    [JsonPropertyName("vega")]        public double Vega { get; init; }
    [JsonPropertyName("theta")]       public double Theta { get; init; }
    [JsonPropertyName("delta")]       public double Delta { get; init; }
    [JsonPropertyName("oi")]          public long Oi { get; init; }
    [JsonPropertyName("oiPercentage")]public double OiPercentage { get; init; }
    [JsonPropertyName("ltp")]         public double Ltp { get; init; }
    [JsonPropertyName("iv")]          public double Iv { get; init; }
    [JsonPropertyName("price")]       public double Price { get; init; }
    [JsonPropertyName("rho")]         public double Rho { get; init; }
}

public class OptionChainResponse : BlitzApiResponse<OptionChainData> { }
