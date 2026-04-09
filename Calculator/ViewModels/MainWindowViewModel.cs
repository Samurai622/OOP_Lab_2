using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Styling;
using Calculator.Models;
using Calculator.Commands;
using Calculator.Services;

namespace Calculator.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly CalculatorEngine _engine = new();
    private readonly HistoryManager _history = new();
    private readonly CurrencyService _currencyService = new();

    [ObservableProperty] private string _display = "0";
    [ObservableProperty] private string _equation = "";
    
    [ObservableProperty] private bool _isStandardVisible = true;
    [ObservableProperty] private bool _isScientificVisible = false;
    [ObservableProperty] private bool _isCurrencyVisible = false;
    
    [ObservableProperty] private string _modeButtonText = "Звичайний";

    [ObservableProperty] private ObservableCollection<string> _availableCurrencies = new();
    [ObservableProperty] private string? _selectedFrom;
    [ObservableProperty] private string? _selectedTo;
    [ObservableProperty] private bool _isMenuOpen = false;

    private bool _isNewInput = true;
    
    [ObservableProperty] private bool _isSecondaryMathVisible = false;
    
    private double? _lastRightOperand = null;
    private string _lastOperator = "";

    // ==========================================
    // НОВІ ВЛАСТИВОСТІ ДЛЯ ВЕРХНЬОГО ТА НИЖНЬОГО РЯДУ
    // ==========================================
    
    // Пам'ять
    [ObservableProperty] private bool _hasMemory = false;
    [ObservableProperty] private double _memoryValue = 0;

    // Режими
    [ObservableProperty] private string _angleModeText = "DEG"; // DEG, RAD, GRAD
    [ObservableProperty] private bool _isHyperbolic = false;
    [ObservableProperty] private bool _isExponentialMode = false;


    partial void OnSelectedFromChanged(string? value) => RealTimeCurrencyConvert();
    partial void OnSelectedToChanged(string? value) => RealTimeCurrencyConvert();
    partial void OnDisplayChanged(string value) => RealTimeCurrencyConvert();

    private CalculatorState GetState() => new(Display, Equation, null, "", _isNewInput);
    private void RestoreState(CalculatorState state)
    {
        Display = state.Display; Equation = state.Equation; _isNewInput = state.IsNewInput;
    }
    private void ExecuteWithHistory(Action action) => _history.ExecuteCommand(new StateCommand(action, GetState, RestoreState));

    [RelayCommand] public void Undo() => _history.Undo();
    [RelayCommand] public void Redo() => _history.Redo();
    [RelayCommand] public void ToggleSecondaryMath() => IsSecondaryMathVisible = !IsSecondaryMathVisible;

    // ==========================================
    // НОВІ КОМАНДИ ДЛЯ НАЛАШТУВАНЬ (DEG, HYP, F-E)
    // ==========================================
    
    [RelayCommand]
    public void ToggleAngleMode()
    {
        if (AngleModeText == "DEG") AngleModeText = "RAD";
        else if (AngleModeText == "RAD") AngleModeText = "GRAD";
        else AngleModeText = "DEG";
    }

    [RelayCommand]
    public void ToggleHyp() => IsHyperbolic = !IsHyperbolic;

    [RelayCommand]
    public void ToggleFE()
    {
        IsExponentialMode = !IsExponentialMode;
        // Оновлюємо відображення поточного числа
        if (double.TryParse(Display, CultureInfo.InvariantCulture, out double val))
        {
            Display = FormatResult(val);
        }
    }

    // ==========================================
    // НОВА КОМАНДА ДЛЯ ПАМ'ЯТІ (MC, MR, MS, M+, M-)
    // ==========================================
    [RelayCommand]
    public void Memory(string action)
    {
        if (Display == "Error") return;
        
        // Отримуємо поточне число з екрану
        double currentVal = 0;
        try { currentVal = EvaluateTokens(Tokenize(Display)); } catch { return; }

        switch (action)
        {
            case "MC": // Memory Clear
                MemoryValue = 0; 
                HasMemory = false; 
                break;
            case "MR": // Memory Recall
                if (HasMemory) 
                { 
                    Display = FormatResult(MemoryValue); 
                    _isNewInput = true; 
                }
                break;
            case "MS": // Memory Store
                MemoryValue = currentVal; 
                HasMemory = true; 
                _isNewInput = true;
                break;
            case "M+": // Memory Add
                MemoryValue += currentVal; 
                HasMemory = true; 
                _isNewInput = true;
                break;
            case "M-": // Memory Subtract
                MemoryValue -= currentVal; 
                HasMemory = true; 
                _isNewInput = true;
                break;
        }
    }

    // Допоміжний метод для форматування чисел (враховує кнопку F-E)
    private string FormatResult(double result)
    {
        if (double.IsNaN(result) || double.IsInfinity(result)) return "Error";
        
        if (IsExponentialMode)
        {
            return result.ToString("E5", CultureInfo.InvariantCulture); // Науковий формат: 1.50000E+06
        }
        
        return Math.Round(result, 10).ToString(CultureInfo.InvariantCulture); // Звичайний формат
    }

    // ==========================================
    // СТАНДАРТНІ КОМАНДИ
    // ==========================================

    [RelayCommand]
    public void Digit(string digit)
    {
        if (IsCurrencyVisible && (string.IsNullOrEmpty(SelectedFrom) || string.IsNullOrEmpty(SelectedTo)))
        {
            Equation = "Спочатку оберіть валюти!"; return;
        }

        ExecuteWithHistory(() =>
        {
            if (Display == "Error") Display = "0";

            if (_isNewInput)
            {
                Display = (digit == "00") ? "0" : digit;
                _isNewInput = false;
            }
            else
            {
                if (Display == "0" && digit != "(" && digit != ")") Display = digit;
                else Display += digit;
            }
        });
    }

    [RelayCommand]
    public void Dot()
    {
        if (IsCurrencyVisible && (string.IsNullOrEmpty(SelectedFrom) || string.IsNullOrEmpty(SelectedTo))) return;
        ExecuteWithHistory(() =>
        {
            if (_isNewInput) { Display = "0."; _isNewInput = false; return; }

            var tokens = Tokenize(Display);
            string lastToken = tokens.Count > 0 ? tokens.Last() : "";
            if (!lastToken.Contains(".")) Display += ".";
        });
    }

    [RelayCommand]
    public void Backspace() => ExecuteWithHistory(() =>
    {
        if (_isNewInput || Display == "Error") return;

        var textOps = new[] { "mod", "yroot", "logy" };
        string? endOp = textOps.FirstOrDefault(o => Display.EndsWith(o));
        
        if (endOp != null) Display = Display.Substring(0, Display.Length - endOp.Length);
        else Display = Display.Length > 1 ? Display[..^1] : "0";

        if (Display == "0" || Display == "") { Display = "0"; _isNewInput = true; }
    });

    [RelayCommand]
    public void Clear() => ExecuteWithHistory(() =>
    {
        Display = "0"; Equation = ""; _isNewInput = true;
        _lastOperator = ""; _lastRightOperand = null;
    });

    [RelayCommand]
    public void Operator(string op)
    {
        if (IsCurrencyVisible) return;
        ExecuteWithHistory(() =>
        {
            if (Display == "Error") return;
            _isNewInput = false;

            string stringOp = op;
            var allOps = new[] { "+", "-", "×", "÷", "^", "mod", "yroot", "logy" };
            
            string? endOp = allOps.FirstOrDefault(o => Display.EndsWith(o));

            if (Display.EndsWith("(")) Display += stringOp;
            else if (endOp != null) Display = Display.Substring(0, Display.Length - endOp.Length) + stringOp;
            else Display += stringOp;
        });
    }

    [RelayCommand]
    public void Calculate()
    {
        if (IsCurrencyVisible) return;
        ExecuteWithHistory(() =>
        {
            if (Display == "Error") return;

            if (_isNewInput && !string.IsNullOrEmpty(_lastOperator) && _lastRightOperand != null)
            {
                string opForDisplay = _lastOperator.Replace("*", "×").Replace("/", "÷");
                Equation = $"{Display}{opForDisplay}{_lastRightOperand.Value.ToString(CultureInfo.InvariantCulture)}";
                try
                {
                    double result = EvaluateTokens(Tokenize(Equation));
                    Display = FormatResult(result);
                }
                catch { Display = "Error"; }
                return;
            }

            Equation = Display; 
            try
            {
                var tokens = Tokenize(Equation);
                
                if (tokens.Count >= 3)
                {
                    string possibleOp = tokens[tokens.Count - 2];
                    if (new[]{"*","/","+","-","^","mod","yroot","logy"}.Contains(possibleOp) && 
                        double.TryParse(tokens.Last(), CultureInfo.InvariantCulture, out double right))
                    {
                        _lastOperator = possibleOp;
                        _lastRightOperand = right;
                    }
                }

                double result = EvaluateTokens(tokens);
                Display = FormatResult(result);
                _isNewInput = true;
            }
            catch { Display = "Error"; }
        });
    }

    [RelayCommand]
    public void Scientific(string func) => ExecuteWithHistory(() =>
    {
        if (Display == "Error") return;

        if (func == "negate")
        {
            if (Display.StartsWith("-")) Display = Display.Substring(1);
            else if (Display != "0") Display = "-" + Display;
            return;
        }

        try
        {
            double val = EvaluateTokens(Tokenize(Display));
            double res = 0;

            // Налаштування для DEG, RAD, GRAD
            double angleFactor = 1.0;
            if (AngleModeText == "DEG") angleFactor = Math.PI / 180.0;
            else if (AngleModeText == "GRAD") angleFactor = Math.PI / 200.0;
            // Для RAD angleFactor залишається 1.0

            switch (func)
            {
                case "sqr": res = val * val; break;
                case "sqrt": res = Math.Sqrt(val); break;
                case "log": res = Math.Log10(val); break;
                case "pow10": res = Math.Pow(10, val); break;
                case "exp": res = Math.Exp(val); break;
                case "fact": res = CalculateFactorial(val); break;
                case "cube": res = Math.Pow(val, 3); break; 
                case "cbrt": res = Math.Cbrt(val); break;   
                case "pow2": res = Math.Pow(2, val); break; 
                
                // Тригонометрія з підтримкою HYP та DEG/RAD/GRAD
                case "sin": res = IsHyperbolic ? Math.Sinh(val) : Math.Sin(val * angleFactor); break;
                case "cos": res = IsHyperbolic ? Math.Cosh(val) : Math.Cos(val * angleFactor); break;
                case "tan": res = IsHyperbolic ? Math.Tanh(val) : Math.Tan(val * angleFactor); break;
                case "asin": res = IsHyperbolic ? Math.Asinh(val) : Math.Asin(val) / angleFactor; break; 
                case "acos": res = IsHyperbolic ? Math.Acosh(val) : Math.Acos(val) / angleFactor; break; 
                case "atan": res = IsHyperbolic ? Math.Atanh(val) : Math.Atan(val) / angleFactor; break; 
                
                default: res = _engine.CalculateScientific(val, func); break;
            }

            // Формуємо красивий текст для рівняння
            string funcPrefix = IsHyperbolic ? "h" : "";
            string funcName = func == "cube" ? "cube" : func == "cbrt" ? "cbrt" : func == "asin" ? $"asin{funcPrefix}" : func == "acos" ? $"acos{funcPrefix}" : func == "atan" ? $"atan{funcPrefix}" : func == "pow2" ? "2^" : func;
            
            if (new[] { "sin", "cos", "tan" }.Contains(func)) funcName = func + funcPrefix;

            Equation = $"{funcName}({Display})";
            Display = double.IsNaN(res) || double.IsInfinity(res) ? "Error" : FormatResult(res);
            _isNewInput = true;
        }
        catch { Display = "Error"; }
    });

    // ==========================================
    // ДВИГУН ПАРСИНГУ
    // ==========================================
    private List<string> Tokenize(string expr)
    {
        expr = expr.Replace(" ", "").Replace("×", "*").Replace("÷", "/").Replace(",", ".");
        var rawTokens = Regex.Split(expr, @"([\*\/\+\-\^\(\)]|mod|yroot|logy)").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        var tokens = new List<string>();
        for (int i = 0; i < rawTokens.Count; i++)
        {
            if (rawTokens[i] == "-" && (i == 0 || new[]{"*","/","+","-","^","mod","yroot","logy","("}.Contains(tokens.LastOrDefault())))
            {
                if (i + 1 < rawTokens.Count) { tokens.Add("-" + rawTokens[i + 1]); i++; }
                else tokens.Add("-");
            }
            else tokens.Add(rawTokens[i]);
        }
        return tokens;
    }

    private double EvaluateTokens(List<string> tokens)
    {
        var t = new List<string>(tokens);

        while (t.Contains("("))
        {
            int openIdx = t.LastIndexOf("(");
            int closeIdx = t.IndexOf(")", openIdx);
            if (closeIdx == -1) closeIdx = t.Count; 

            var subExpr = t.GetRange(openIdx + 1, closeIdx - openIdx - 1);
            double subRes = EvaluateTokens(subExpr); 

            int countToRemove = (closeIdx < t.Count) ? (closeIdx - openIdx + 1) : (closeIdx - openIdx);
            t.RemoveRange(openIdx, countToRemove);
            t.Insert(openIdx, subRes.ToString(CultureInfo.InvariantCulture));
        }

        if (t.Count % 2 == 0 && t.Count > 0 && new[]{"*","/","+","-","^","mod","yroot","logy"}.Contains(t.Last())) 
            t.RemoveAt(t.Count - 1); 

        ProcessOperations(t, new[] { "^", "yroot", "logy" });
        ProcessOperations(t, new[] { "*", "/", "mod" });
        ProcessOperations(t, new[] { "+", "-" });

        if (t.Count > 0 && double.TryParse(t[0], CultureInfo.InvariantCulture, out double result)) return result;
        return 0;
    }

    private void ProcessOperations(List<string> tokens, string[] ops)
    {
        for (int i = 1; i < tokens.Count - 1; i += 2)
        {
            if (Array.IndexOf(ops, tokens[i]) >= 0)
            {
                if (double.TryParse(tokens[i - 1], CultureInfo.InvariantCulture, out double left) &&
                    double.TryParse(tokens[i + 1], CultureInfo.InvariantCulture, out double right))
                {
                    double res = 0;
                    switch (tokens[i])
                    {
                        case "^": res = Math.Pow(left, right); break;
                        case "yroot": if (right == 0) throw new DivideByZeroException(); res = Math.Pow(left, 1.0 / right); break;
                        case "logy": res = Math.Log(left, right); break; 
                        case "*": res = left * right; break;
                        case "/": if (right == 0) throw new DivideByZeroException(); res = left / right; break;
                        case "mod": res = left % right; break;
                        case "+": res = left + right; break;
                        case "-": res = left - right; break;
                    }
                    tokens[i - 1] = res.ToString(CultureInfo.InvariantCulture);
                    tokens.RemoveRange(i, 2);
                    i -= 2; 
                }
            }
        }
    }

    private double CalculateFactorial(double n)
    {
        if (n < 0 || n % 1 != 0) return double.NaN;
        if (n == 0 || n == 1) return 1;
        double result = 1;
        for (int i = 2; i <= n; i++) result *= i;
        return result;
    }

    private void RealTimeCurrencyConvert()
    {
        if (!IsCurrencyVisible) return;
        if (string.IsNullOrEmpty(SelectedFrom) || string.IsNullOrEmpty(SelectedTo)) { Equation = "Оберіть валюти"; return; }
        
        try
        {
            double amount = EvaluateTokens(Tokenize(Display));
            double res = _currencyService.Convert(amount, SelectedFrom, SelectedTo);
            Equation = $"= {Math.Round(res, 2).ToString(CultureInfo.InvariantCulture)} {SelectedTo}";
        }
        catch { Equation = "Помилка вводу"; }
    }

    [RelayCommand] public void ToggleMenu() => IsMenuOpen = !IsMenuOpen;

    [RelayCommand]
    public async Task SetModeAsync(string mode)
    {
        IsMenuOpen = false;
        IsStandardVisible = false; IsScientificVisible = false; IsCurrencyVisible = false;
        Equation = ""; Display = "0"; _isNewInput = true;

        switch (mode)
        {
            case "Standard": IsStandardVisible = true; ModeButtonText = "Звичайний"; break;
            case "Scientific": IsScientificVisible = true; ModeButtonText = "Інженерний"; break;
            case "Currency":
                IsCurrencyVisible = true; ModeButtonText = "Валюти";
                if (AvailableCurrencies.Count == 0)
                {
                    Equation = "Завантаження..."; await _currencyService.FetchRatesAsync();
                    AvailableCurrencies = new ObservableCollection<string>(_currencyService.GetCurrencies());
                }
                RealTimeCurrencyConvert(); break;
        }
    }

    [RelayCommand] public void ToggleTheme()
    {
        var app = Application.Current;
        if (app != null) app.RequestedThemeVariant = app.RequestedThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    }
}