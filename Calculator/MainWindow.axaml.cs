using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Calculator.Services;

namespace Calculator;

public partial class MainWindow : Window
{
    private readonly CalculatorEngine _engine = new();
    private readonly HistoryManager _history = new();

    // Виправлено: культурне початкове значення
    private string _display = "0";
    private string _equation = "";
    private double? _leftOperand = null;
    private string _currentOperator = "";
    private bool _isNewInput = true;
    private bool _isScientificMode = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    private CalculatorState GetCurrentState()
    {
        return new CalculatorState(_display, _equation, _leftOperand, _currentOperator, _isNewInput);
    }

    private void RestoreState(CalculatorState state)
    {
        _display = state.Display;
        _equation = state.Equation;
        _leftOperand = state.LeftOperand;
        _currentOperator = state.CurrentOperator;
        _isNewInput = state.IsNewInput;

        UpdateUI();
    }

    private void ExecuteWithHistory(Action action)
    {
        var cmd = new StateCommand(action, GetCurrentState, RestoreState);
        _history.ExecuteCommand(cmd);
        UpdateUI();
    }

    private void UpdateUI()
    {
        DisplayTextBlock.Text = _display;
        EquationTextBlock.Text = _equation;
    }

    private void OnUndoClick(object? sender, RoutedEventArgs e)
    {
        _history.Undo();
        UpdateUI();
    }

    private void OnRedoClick(object? sender, RoutedEventArgs e)
    {
        _history.Redo();
        UpdateUI();
    }

    private void OnDigitClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content != null)
        {
            string digit = btn.Content.ToString()!;
            
            ExecuteWithHistory(() =>
            {
                if (_display == "Error" || _display == "Ділення на нуль!") _display = "0";

                if (_isNewInput || _display == "0")
                {
                    if (digit == "00" && _display == "0") return;
                    _display = digit;
                    _isNewInput = false;
                }
                else
                {
                    _display += digit;
                }
            });
        }
    }

    private void OnDotClick(object? sender, RoutedEventArgs e)
    {
        ExecuteWithHistory(() =>
        {
            if (_isNewInput)
            {
                _display = "0.";
                _isNewInput = false;
            }
            else if (!_display.Contains('.'))
            {
                _display += ".";
            }
        });
    }

    private void OnBackspaceClick(object? sender, RoutedEventArgs e)
    {
        ExecuteWithHistory(() =>
        {
            if (_isNewInput || _display == "Error" || _display == "Ділення на нуль!") return;
            _display = _display.Length > 1 ? _display[..^1] : "0";
            if (_display == "0") _isNewInput = true;
        });
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        ExecuteWithHistory(() =>
        {
            // Виправлено: культурне очищення
            _display = "0";
            _equation = "";
            _leftOperand = null;
            _currentOperator = "";
            _isNewInput = true;
        });
    }

    private void OnOperatorClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content != null)
        {
            string op = btn.Content.ToString()!;

            ExecuteWithHistory(() =>
            {
                if (_display == "Error" || _display == "Ділення на нуль!") return;

                if (_leftOperand != null && !_isNewInput)
                {
                    PerformBaseCalculation();
                }

                if (double.TryParse(_display.Replace(',', '.'), CultureInfo.InvariantCulture, out double current))
                {
                    _leftOperand = current;
                    _currentOperator = op;
                    _equation = $"{current} {op}";
                    _isNewInput = true;
                }
            });
        }
    }

    private void Window_KeyDown(object sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Divide)
        {
            var fakeButton = new Button { Content = "÷" };
            OnOperatorClick(fakeButton, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void OnCalculateClick(object? sender, RoutedEventArgs e)
    {
        ExecuteWithHistory(() =>
        {
            if (_leftOperand == null || string.IsNullOrEmpty(_currentOperator)) return;
            
            double current = double.Parse(_display.Replace(',', '.'), CultureInfo.InvariantCulture);
            _equation = $"{_leftOperand} {_currentOperator} {current} =";
            
            PerformBaseCalculation(current);
            
            _leftOperand = null;
            _currentOperator = "";
            _isNewInput = true;
        });
    }

    private void PerformBaseCalculation(double? rightOp = null)
    {
        if (_leftOperand == null) return;
        double right = rightOp ?? double.Parse(_display.Replace(',', '.'), CultureInfo.InvariantCulture);

        try
        {
            double result = _engine.CalculateBase(_leftOperand.Value, right, _currentOperator);
            _display = Math.Round(result, 10).ToString(CultureInfo.InvariantCulture);
            _leftOperand = result;
        }
        catch (DivideByZeroException)
        {
            // Виправлено: адекватне повідомлення про помилку
            _display = "Ділення на нуль!";
            _leftOperand = null;
        }
    }

    private void OnScientificClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter != null)
        {
            string func = btn.CommandParameter.ToString()!;

            ExecuteWithHistory(() =>
            {
                if (double.TryParse(_display.Replace(',', '.'), CultureInfo.InvariantCulture, out double val))
                {
                    double res = _engine.CalculateScientific(val, func);
                    _equation = $"{func}({val})";
                    _display = double.IsNaN(res) ? "Error" : Math.Round(res, 10).ToString(CultureInfo.InvariantCulture);
                    _isNewInput = true;
                }
            });
        }
    }

    private void OnPiEClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter != null)
        {
            string value = btn.CommandParameter.ToString()!;
            ExecuteWithHistory(() =>
            {
                _display = value;
                _isNewInput = false;
            });
        }
    }

    private void OnToggleModeClick(object? sender, RoutedEventArgs e)
    {
        _isScientificMode = !_isScientificMode;
        ModeButton.Content = _isScientificMode ? "Інженерний" : "Звичайний";
        ScientificPanel.IsVisible = _isScientificMode;
    }

    private void OnToggleThemeClick(object? sender, RoutedEventArgs e)
    {
        var app = Application.Current;
        if (app != null)
        {
            app.RequestedThemeVariant = app.RequestedThemeVariant == ThemeVariant.Dark 
                ? ThemeVariant.Light 
                : ThemeVariant.Dark;
        }
    }
}