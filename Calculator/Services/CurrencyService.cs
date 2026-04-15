using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Calculator.Models;

namespace Calculator.Services;

public class CurrencyService
{
    private static readonly HttpClient _httpClient = new();
    private Dictionary<string, double>? _rates;
    private const string ApiUrl = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json";

    public async Task FetchRatesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(ApiUrl);

            var data = JsonSerializer.Deserialize<List<NbuCurrencyRate>>(response);
            
            if (data != null)
            {
                _rates = new Dictionary<string, double>();
                
                foreach (var item in data)
                {
                    if (!string.IsNullOrEmpty(item.CurrencyCode))
                    {
                        _rates[item.CurrencyCode] = item.Rate;
                    }
                }
                
                _rates["UAH"] = 1.0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка завантаження валют НБУ: {ex.Message}");
        }
    }

    public List<string> GetCurrencies()
    {
        if (_rates == null) return new List<string> { "USD", "EUR", "UAH" };
        
        var popular = new[] { "USD", "EUR", "UAH", "PLN", "GBP" };
        var all = _rates.Keys.ToList();
        return popular.Concat(all.Except(popular)).ToList();
    }

    public double Convert(double amount, string from, string to)
    {
        if (_rates == null || !_rates.ContainsKey(from) || !_rates.ContainsKey(to)) return 0;

        double amountInUah = amount * _rates[from];
        
        return amountInUah / _rates[to];
    }
}