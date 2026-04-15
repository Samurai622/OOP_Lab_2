using System.Collections.Generic;
using System.Linq;


namespace Calculator.Services;


public class VolumeConverterService
{
   private readonly Dictionary<string, double> _conversionRates = new()
   {
       { "Мілілітри (ml)", 0.001 },
       { "Кубічні сантиметри (cm³)", 0.001 },
       { "Літри (L)", 1.0 },
       { "Кубічні метри (m³)", 1000.0 },
       { "Чайні ложки (tsp)", 0.00492892 },
       { "Столові ложки (tbsp)", 0.0147868 },
       { "Рідкі унції (fl oz)", 0.0295735 },
       { "Чашки (cup)", 0.236588 },
       { "Пінти (pt)", 0.473176 },
       { "Галони (gal)", 3.78541 }
   };


   public List<string> GetUnits() => _conversionRates.Keys.ToList();


   public double Convert(double amount, string fromUnit, string toUnit)
   {
       if (!_conversionRates.ContainsKey(fromUnit) || !_conversionRates.ContainsKey(toUnit))
           return 0;

       double inLiters = amount * _conversionRates[fromUnit];

       return inLiters / _conversionRates[toUnit];
   }
}



