using System.Collections.Generic;
using System.Linq;

namespace Calculator.Services;

public class WeightConverterService
{
    // Значення відносно 1 Кілограма
    private readonly Dictionary<string, double> _conversionRates = new()
    {
        { "Карати (ct)", 0.0002 },
        { "Міліграми (mg)", 0.000001 },
        { "Грами (g)", 0.001 },
        { "Кілограми (kg)", 1.0 },
        { "Метричні тонни (t)", 1000.0 },
        { "Унції (oz)", 0.0283495 },
        { "Фунти (lb)", 0.453592 },
        { "Стоуни (st)", 6.35029 },
        { "Короткі тонни (US t)", 907.185 },
        { "Довгі тонни (UK t)", 1016.05 }
    };

    public List<string> GetUnits() => _conversionRates.Keys.ToList();

    public double Convert(double amount, string fromUnit, string toUnit)
    {
        if (!_conversionRates.ContainsKey(fromUnit) || !_conversionRates.ContainsKey(toUnit))
            return 0;

        // Переводимо у кілограми
        double inKilograms = amount * _conversionRates[fromUnit];
        
        // З кілограмів у цільову одиницю
        return inKilograms / _conversionRates[toUnit];
    }
}