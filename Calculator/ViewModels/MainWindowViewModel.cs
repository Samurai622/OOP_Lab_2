using System;
using System.Collections.ObjectModel;
using System.Globalization;
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
    
    [ObservableProperty] private bool _isScientificVisible = false;
    [ObservableProperty] private bool _isCurrencyVisible = false;
    [ObservableProperty] private string _modeButtonText = "Звичайний";

    [ObservableProperty] private ObservableCollection<string> _availableCurrencies = new();
    
    // Зроблено nullable (з ?), щоб можна було перевірити, чи користувач їх обрав
    [ObservableProperty] private string? _selectedFrom;
    [ObservableProperty] private string? _selectedTo;

    // Властивість для відкриття/закриття меню
    [ObservableProperty] private bool _isMenuOpen = false;

    private double? _leftOperand = null;
    private string _currentOperator = "";
    private bool _isNewInput = true;

    partial void OnSelectedFromChanged(string? value) => RealTimeCurrencyConvert();
    partial void OnSelectedToChanged(string? value) => RealTimeCurrencyConvert();
    partial void OnDisplayChanged(string value) => RealTimeCurrencyConvert();

    private CalculatorState GetState() => new(Display, Equation, _leftOperand, _currentOperator, _isNewInput);
    private void RestoreState(CalculatorState state)
    {
        Display = state.Display; Equation = state.Equation;
        _leftOperand = state.LeftOperand; _currentOperator = state.CurrentOperator; _isNewInput = state.IsNewInput;
    }
    private void ExecuteWithHistory(Action action) => _history.ExecuteCommand(new StateCommand(action, GetState, RestoreState));

    [RelayCommand] public void Undo() => _history.Undo();
    [RelayCommand] public void Redo() => _history.Redo();

    [RelayCommand]
    public void Digit(string digit)
    {
        if (IsCurrencyVisible && (string.IsNullOrEmpty(SelectedFrom) || string.IsNullOrEmpty(SelectedTo)))
        {
            Equation = "Спочатку оберіть валюти!";
            return; 
        }

        ExecuteWithHistory(() =>
        {
            if (Display == "Error") Display = "0";
            if (_isNewInput || Display == "0") 
            { 
                Display = (digit == "00" && Display == "0") ? "0" : digit; 
                _isNewInput = false; 
            }
            else Display += digit;
        });
    }

    [RelayCommand]
    public void Dot()
    {
        if (IsCurrencyVisible && (string.IsNullOrEmpty(SelectedFrom) || string.IsNullOrEmpty(SelectedTo)))
        {
            Equation = "Спочатку оберіть валюти!";
            return;
        }

        ExecuteWithHistory(() => 
        { 
            if (_isNewInput) { Display = "0."; _isNewInput = false; } 
            else if (!Display.Contains('.')) Display += "."; 
        });
    }

    [RelayCommand]
    public void Backspace()
    {
        ExecuteWithHistory(() => { if (_isNewInput || Display == "Error") return; Display = Display.Length > 1 ? Display[..^1] : "0"; if (Display == "0") _isNewInput = true; });
    }

    [RelayCommand]
    public void Clear() => ExecuteWithHistory(() => { Display = "0"; Equation = ""; _leftOperand = null; _currentOperator = ""; _isNewInput = true; });

    [RelayCommand]
    public void Operator(string op)
    {
        if (IsCurrencyVisible) return; 
        ExecuteWithHistory(() =>
        {
            if (Display == "Error") return;
            if (_leftOperand != null && !_isNewInput) PerformBaseCalc();
            if (double.TryParse(Display.Replace(',', '.'), CultureInfo.InvariantCulture, out double current))
            { _leftOperand = current; _currentOperator = op; Equation = $"{current} {op}"; _isNewInput = true; }
        });
    }

    [RelayCommand]
    public void Calculate()
    {
        if (IsCurrencyVisible) return; 

        ExecuteWithHistory(() =>
        {
            if (_leftOperand == null || string.IsNullOrEmpty(_currentOperator)) return;
            double current = double.Parse(Display.Replace(',', '.'), CultureInfo.InvariantCulture);
            Equation = $"{_leftOperand} {_currentOperator} {current} =";
            PerformBaseCalc(current);
            _leftOperand = null; _currentOperator = ""; _isNewInput = true;
        });
    }

    private void PerformBaseCalc(double? rightOp = null)
    {
        if (_leftOperand == null) return;
        double right = rightOp ?? double.Parse(Display.Replace(',', '.'), CultureInfo.InvariantCulture);
        try { _leftOperand = _engine.CalculateBase(_leftOperand.Value, right, _currentOperator); Display = Math.Round(_leftOperand.Value, 10).ToString(CultureInfo.InvariantCulture); }
        catch (DivideByZeroException) { Display = "Error"; _leftOperand = null; }
    }

    [RelayCommand]
    public void Scientific(string func) => ExecuteWithHistory(() =>
    {
        if (double.TryParse(Display.Replace(',', '.'), CultureInfo.InvariantCulture, out double val))
        {
            double res = _engine.CalculateScientific(val, func);
            Equation = $"{func}({val})"; Display = double.IsNaN(res) ? "Error" : Math.Round(res, 10).ToString(CultureInfo.InvariantCulture); _isNewInput = true;
        }
    });

    private void RealTimeCurrencyConvert()
    {
        if (!IsCurrencyVisible) return;

        if (string.IsNullOrEmpty(SelectedFrom) || string.IsNullOrEmpty(SelectedTo))
        {
            Equation = "Оберіть валюти для конвертації";
            return;
        }
        
        if (double.TryParse(Display.Replace(',', '.'), CultureInfo.InvariantCulture, out double amount))
        {
            double res = _currencyService.Convert(amount, SelectedFrom, SelectedTo);
            Equation = $"= {Math.Round(res, 2).ToString(CultureInfo.InvariantCulture)} {SelectedTo}";
        }
    }

    // --- НОВІ КОМАНДИ ДЛЯ МЕНЮ ---

    [RelayCommand]
    public void ToggleMenu() => IsMenuOpen = !IsMenuOpen;

    [RelayCommand]
    public async Task SetModeAsync(string mode)
    {
        IsMenuOpen = false; // Закриваємо меню після вибору
        
        IsScientificVisible = false;
        IsCurrencyVisible = false;
        Equation = ""; 
        Display = "0"; 
        _isNewInput = true;

        switch (mode)
        {
            case "Standard":
                ModeButtonText = "Звичайний";
                break;

            case "Scientific":
                IsScientificVisible = true;
                ModeButtonText = "Інженерний";
                break;

            case "Currency":
                IsCurrencyVisible = true;
                ModeButtonText = "Валюти";
                
                if (AvailableCurrencies.Count == 0)
                {
                    Equation = "Завантаження курсів...";
                    await _currencyService.FetchRatesAsync();
                    AvailableCurrencies = new ObservableCollection<string>(_currencyService.GetCurrencies());
                }
                
                RealTimeCurrencyConvert();
                break;
        }
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        var app = Application.Current;
        if (app != null) app.RequestedThemeVariant = app.RequestedThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    }
}