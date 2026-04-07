using System.Text.Json.Serialization;

namespace Calculator.Models;

// Цей клас описує ОДИН елемент масиву з API Нацбанку
public class NbuCurrencyRate
{
    [JsonPropertyName("cc")]
    public string CurrencyCode { get; set; } = string.Empty; // Наприклад: "USD"

    [JsonPropertyName("rate")]
    public double Rate { get; set; } // Наприклад: 41.50
}