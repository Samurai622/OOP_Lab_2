using System.Collections.Generic;

namespace Calculator.Services;

public class TemperatureConverterService
{
    private readonly List<string> _units = new()
    {
        "Цельсій (°C)",
        "Фаренгейт (°F)",
        "Кельвін (K)"
    };

    public List<string> GetUnits() => _units;

    public double Convert(double amount, string fromUnit, string toUnit)
    {
        if (fromUnit == toUnit) return amount;

        double inCelsius = fromUnit switch
        {
            "Фаренгейт (°F)" => (amount - 32) * 5.0 / 9.0,
            "Кельвін (K)" => amount - 273.15,
            _ => amount
        };

        return toUnit switch
        {
            "Фаренгейт (°F)" => (inCelsius * 9.0 / 5.0) + 32,
            "Кельвін (K)" => inCelsius + 273.15,
            _ => inCelsius
        };
    }
}