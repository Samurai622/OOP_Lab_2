using System.Text.Json.Serialization;

namespace Calculator.Models;

public class NbuCurrencyRate
{
    [JsonPropertyName("cc")]
    public string CurrencyCode { get; set; } = string.Empty;

    [JsonPropertyName("rate")]
    public double Rate { get; set; }
}