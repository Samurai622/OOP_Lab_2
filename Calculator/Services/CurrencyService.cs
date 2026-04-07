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
    private const string ApiUrl = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json"; // API НБУ

    public async Task FetchRatesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(ApiUrl);
            
            // НБУ повертає масив, тому десеріалізуємо у List
            var data = JsonSerializer.Deserialize<List<NbuCurrencyRate>>(response);
            
            if (data != null)
            {
                _rates = new Dictionary<string, double>();
                
                // Перепаковуємо масив у зручний для нас словник (Dictionary)
                foreach (var item in data)
                {
                    if (!string.IsNullOrEmpty(item.CurrencyCode))
                    {
                        _rates[item.CurrencyCode] = item.Rate;
                    }
                }
                
                // НБУ не передає гривню (бо це база). Додаємо її вручну: 1 гривня = 1 гривня.
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
        
        // Ставимо популярні валюти на початок списку
        var popular = new[] { "USD", "EUR", "UAH", "PLN", "GBP" };
        var all = _rates.Keys.ToList();
        return popular.Concat(all.Except(popular)).ToList();
    }

    public double Convert(double amount, string from, string to)
    {
        if (_rates == null || !_rates.ContainsKey(from) || !_rates.ContainsKey(to)) return 0;

        // Математика НБУ:
        // rate[From] - це скільки гривень коштує 1 одиниця ВАЛЮТИ_1
        // Переводимо введену суму в гривні:
        double amountInUah = amount * _rates[from];
        
        // Ділимо гривні на курс ВАЛЮТИ_2
        return amountInUah / _rates[to];
    }
}