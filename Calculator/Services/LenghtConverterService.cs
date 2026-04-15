using System.Collections.Generic;
using System.Linq;

namespace Calculator.Services;

public class LengthConverterService
{
    private readonly Dictionary<string, double> _conversionRates = new()
    {
        { "Міліметри (mm)", 0.001 },
        { "Сантиметри (cm)", 0.01 },
        { "Дециметри (dm)", 0.1 },
        { "Метри (m)", 1.0 },
        { "Кілометри (km)", 1000.0 },
        { "Дюйми (in)", 0.0254 },
        { "Фути (ft)", 0.3048 },
        { "Ярди (yd)", 0.9144 },
        { "Милі (mi)", 1609.344 },
        { "Морські милі (nmi)", 1852.0 }
    };

    public List<string> GetUnits() => _conversionRates.Keys.ToList();

    public double Convert(double amount, string fromUnit, string toUnit)
    {
        if (!_conversionRates.ContainsKey(fromUnit) || !_conversionRates.ContainsKey(toUnit))
            return 0;

        double inMeters = amount * _conversionRates[fromUnit];

        return inMeters / _conversionRates[toUnit];
    }
}