using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    
    // Властивості для відображення розбитого тексту та курсора
    [ObservableProperty] private string _displayLeft = "0";
    [ObservableProperty] private string _displayRight = "";    
    [ObservableProperty] private int _caretPosition = 1;
    [ObservableProperty] private double _cursorOpacity = 1.0;
    
    // Видимість режимів
    [ObservableProperty] private bool _isMainDisplayVisible = true;
    [ObservableProperty] private bool _isStandardVisible = true;
    [ObservableProperty] private bool _isScientificVisible = false;
    [ObservableProperty] private bool _isCurrencyVisible = false;
    [ObservableProperty] private bool _isProgrammerVisible = false;
    [ObservableProperty] private bool _isDateCalcVisible = false;
    
    [ObservableProperty] private string _modeButtonText = "Звичайний";

    [ObservableProperty] private ObservableCollection<string> _availableCurrencies = new();
    [ObservableProperty] private string? _selectedFrom;
    [ObservableProperty] private string? _selectedTo;
    [ObservableProperty] private bool _isMenuOpen = false;
    [ObservableProperty] private bool _Six_sevenVisible = false;

    private bool _isNewInput = true;
    
    [ObservableProperty] private bool _isSecondaryMathVisible = false;
    [ObservableProperty] private bool _isProgrammerSecondaryVisible = false;
    
    private double? _lastRightOperand = null;
    private string _lastOperator = "";

    [ObservableProperty] private bool _hasMemory = false;
    [ObservableProperty] private double _memoryValue = 0;

    [ObservableProperty] private string _angleModeText = "DEG"; 
    [ObservableProperty] private bool _isHyperbolic = false;
    [ObservableProperty] private bool _isExponentialMode = false;

    [ObservableProperty] private int _currentBase = 10;
    
    [ObservableProperty] private string _hexDisplay = "0";
    [ObservableProperty] private string _decDisplay = "0";
    [ObservableProperty] private string _octDisplay = "0";
    [ObservableProperty] private string _binDisplay = "0";

    [ObservableProperty] private bool _isHexActive = false;
    [ObservableProperty] private bool _isDecActive = true;
    [ObservableProperty] private bool _isOctActive = false;
    [ObservableProperty] private bool _isBinActive = false;

    [ObservableProperty] private bool _isOctEnabled = true;
    [ObservableProperty] private bool _isDecEnabled = true;
    [ObservableProperty] private bool _isBinEnabled = true;

    // --- Властивості Обчислення Дат ---
    [ObservableProperty] private int _dateCalcModeIndex = 0;
    [ObservableProperty] private bool _isDateDifferenceMode = true;

    [ObservableProperty] private DateTime? _fromDate = DateTime.Now;
    [ObservableProperty] private DateTime? _toDate = DateTime.Now;
    [ObservableProperty] private string _dateDiffResult = "Та сама дата";
    [ObservableProperty] private string _dateDiffTotalDays = "0 днів";

    [ObservableProperty] private DateTime? _startDate = DateTime.Now;
    [ObservableProperty] private bool _isAddModeDate = true;
    [ObservableProperty] private decimal? _addSubYears = 0;
    [ObservableProperty] private decimal? _addSubMonths = 0;
    [ObservableProperty] private decimal? _addSubDays = 0;
    [ObservableProperty] private string _addSubResultDate = "";

    // --- Модальний календар ---
    [ObservableProperty] private bool _isCalendarOverlayVisible;
    [ObservableProperty] private DateTime? _calendarOverlayDate = DateTime.Now;
    [ObservableProperty] private DateTime _calendarOverlayDisplayDate = DateTime.Now;
    private string _calendarTarget = "";

    // --- Текстові поля для ручного введення ---
    private string _fromDateText = "";
    public string FromDateText
    {
        get => _fromDateText;
        set
        {
            _fromDateText = value;
            if (TryParseDate(value, out var d)) { FromDate = d; HasFromDateError = false; }
            else if (value?.Length == 10) { HasFromDateError = true; FromDateValidation = "Невірний формат дати"; OnPropertyChanged(nameof(FromDateValidation)); }
            OnPropertyChanged();
        }
    }

    private string _toDateText = "";
    public string ToDateText
    {
        get => _toDateText;
        set
        {
            _toDateText = value;
            if (TryParseDate(value, out var d)) { ToDate = d; HasToDateError = false; }
            else if (value?.Length == 10) { HasToDateError = true; ToDateValidation = "Невірний формат дати"; OnPropertyChanged(nameof(ToDateValidation)); }
            OnPropertyChanged();
        }
    }

    private string _startDateText = "";
    public string StartDateText
    {
        get => _startDateText;
        set
        {
            _startDateText = value;
            if (TryParseDate(value, out var d)) { StartDate = d; HasStartDateError = false; }
            else if (value?.Length == 10) { HasStartDateError = true; StartDateValidation = "Невірний формат дати"; OnPropertyChanged(nameof(StartDateValidation)); }
            OnPropertyChanged();
        }
    }

    // --- Блокування секцій ---
    public bool IsFromDateSet => FromDate.HasValue;
    public bool IsStartDateSet => StartDate.HasValue;
    public bool IsCalculatorKeyboardActive => !IsDateCalcVisible;
    public bool IsNotDateCalc => !IsDateCalcVisible;
    public double ToDateSectionOpacity => IsFromDateSet ? 1.0 : 0.45;
    public double AddSubSectionOpacity => IsStartDateSet ? 1.0 : 0.45;

    // --- Валідація ---
    private bool _hasFromDateError;
    public bool HasFromDateError
    {
        get => _hasFromDateError;
        set { _hasFromDateError = value; OnPropertyChanged(); }
    }
    public string FromDateValidation { get; set; } = "";

    private bool _hasToDateError;
    public bool HasToDateError
    {
        get => _hasToDateError;
        set { _hasToDateError = value; OnPropertyChanged(); }
    }
    public string ToDateValidation { get; set; } = "";

    private bool _hasStartDateError;
    public bool HasStartDateError
    {
        get => _hasStartDateError;
        set { _hasStartDateError = value; OnPropertyChanged(); }
    }
    public string StartDateValidation { get; set; } = "";

    // --- Хелпер парсингу ---
    private static bool TryParseDate(string? s, out DateTime? result)
    {
        result = null;
        if (s?.Length != 10) return false;
        if (DateTime.TryParseExact(s, "dd.MM.yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var d))
        {
            result = d;
            return true;
        }
        return false;
    }

    public MainWindowViewModel()
    {
        var cursorTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        cursorTimer.Tick += (s, e) =>
        {
            CursorOpacity = CursorOpacity == 1.0 ? 0.0 : 1.0;
        };
        cursorTimer.Start();
        UpdateSplitDisplay();
        UpdateDateDifference();
        UpdateAddSubDate();
    }

    // --- Команди Модального Календаря ---
    [RelayCommand]
    public void OpenCalendar(string target)
    {
        _calendarTarget = target;
        DateTime initialDate = DateTime.Now;
        
        switch (target)
        {
            case "From":
                if (FromDate.HasValue) initialDate = FromDate.Value;
                break;
            case "To":
                if (ToDate.HasValue) initialDate = ToDate.Value;
                break;
            case "Start":
                if (StartDate.HasValue) initialDate = StartDate.Value;
                break;
        }
        
        CalendarOverlayDate = initialDate;
        CalendarOverlayDisplayDate = initialDate;
        IsCalendarOverlayVisible = true;
    }

    [RelayCommand]
    public void ConfirmCalendar()
    {
        if (CalendarOverlayDate.HasValue)
        {
            switch (_calendarTarget)
            {
                case "From": FromDate = CalendarOverlayDate; break;
                case "To": ToDate = CalendarOverlayDate; break;
                case "Start": StartDate = CalendarOverlayDate; break;
            }
        }
        IsCalendarOverlayVisible = false;
    }

    [RelayCommand]
    public void CancelCalendar()
    {
        IsCalendarOverlayVisible = false;
    }

    // --- Тригери Зміни Дат ---
    partial void OnDateCalcModeIndexChanged(int value)
    {
        IsDateDifferenceMode = value == 0;
        if (IsDateDifferenceMode) UpdateDateDifference();
        else UpdateAddSubDate();
    }
    partial void OnFromDateChanged(DateTime? value)
    {
        _fromDateText = value?.ToString("dd.MM.yyyy") ?? "";
        OnPropertyChanged(nameof(FromDateText));
        OnPropertyChanged(nameof(IsFromDateSet));
        OnPropertyChanged(nameof(ToDateSectionOpacity));
        UpdateDateDifference();
    }
    partial void OnToDateChanged(DateTime? value)
    {
        _toDateText = value?.ToString("dd.MM.yyyy") ?? "";
        OnPropertyChanged(nameof(ToDateText));
        UpdateDateDifference();
    }
    partial void OnStartDateChanged(DateTime? value)
    {
        _startDateText = value?.ToString("dd.MM.yyyy") ?? "";
        OnPropertyChanged(nameof(StartDateText));
        OnPropertyChanged(nameof(IsStartDateSet));
        OnPropertyChanged(nameof(AddSubSectionOpacity));
        UpdateAddSubDate();
    }
    partial void OnIsAddModeDateChanged(bool value) => UpdateAddSubDate();
    partial void OnAddSubYearsChanged(decimal? value) => UpdateAddSubDate();
    partial void OnAddSubMonthsChanged(decimal? value) => UpdateAddSubDate();
    partial void OnAddSubDaysChanged(decimal? value) => UpdateAddSubDate();

    private string FormatDeclension(int num, string f1, string f2, string f5)
    {
        int n = Math.Abs(num) % 100;
        int n1 = n % 10;
        if (n >= 11 && n <= 19) return $"{num} {f5}";
        if (n1 == 1) return $"{num} {f1}";
        if (n1 >= 2 && n1 <= 4) return $"{num} {f2}";
        return $"{num} {f5}";
    }

    private void UpdateDateDifference()
    {
        if (!IsDateCalcVisible || !IsDateDifferenceMode) return;
        if (!FromDate.HasValue || !ToDate.HasValue) 
        {
            DateDiffResult = "-";
            DateDiffTotalDays = "";
            return;
        }

        var diff = ToDate.Value.Date - FromDate.Value.Date;
        int totalDays = Math.Abs((int)diff.TotalDays);
        
        DateTime minDate = FromDate.Value.Date < ToDate.Value.Date ? FromDate.Value.Date : ToDate.Value.Date;
        DateTime maxDate = FromDate.Value.Date < ToDate.Value.Date ? ToDate.Value.Date : FromDate.Value.Date;
        
        int years = maxDate.Year - minDate.Year;
        int months = maxDate.Month - minDate.Month;
        int days = maxDate.Day - minDate.Day;

        if (days < 0) {
            months--;
            days += DateTime.DaysInMonth(minDate.AddMonths(months).Year, minDate.AddMonths(months).Month);
        }
        if (months < 0) {
            years--;
            months += 12;
        }

        int weeks = days / 7;
        int remainingDays = days % 7;

        List<string> parts = new();
        if (years > 0) parts.Add(FormatDeclension(years, "рік", "роки", "років"));
        if (months > 0) parts.Add(FormatDeclension(months, "місяць", "місяці", "місяців"));
        if (weeks > 0) parts.Add(FormatDeclension(weeks, "тиждень", "тижні", "тижнів"));
        if (remainingDays > 0) parts.Add(FormatDeclension(remainingDays, "день", "дні", "днів"));

        if (totalDays == 0) 
        {
            DateDiffResult = "Та сама дата";
            DateDiffTotalDays = "0 дн.";
        }
        else 
        {
            DateDiffResult = string.Join(", ", parts);
            DateDiffTotalDays = FormatDeclension(totalDays, "день", "дні", "днів");
        }
    }

    private void UpdateAddSubDate()
    {
        if (!IsDateCalcVisible || IsDateDifferenceMode || !StartDate.HasValue) return;
        
        try {
            int mult = IsAddModeDate ? 1 : -1;
            DateTime res = StartDate.Value.Date
                .AddYears((int)(AddSubYears ?? 0) * mult)
                .AddMonths((int)(AddSubMonths ?? 0) * mult)
                .AddDays((int)(AddSubDays ?? 0) * mult);
                
            AddSubResultDate = res.ToString("D", new CultureInfo("uk-UA"));
        } catch {
            AddSubResultDate = "Помилка дати (вихід за межі)";
        }
    }

    private void UpdateCommandStates()
    {
        DigitCommand.NotifyCanExecuteChanged();
        BackspaceCommand.NotifyCanExecuteChanged();
        ClearCommand.NotifyCanExecuteChanged();
        OperatorCommand.NotifyCanExecuteChanged();
        CalculateCommand.NotifyCanExecuteChanged();
        MoveCaretCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFromChanged(string? value) => RealTimeCurrencyConvert();
    partial void OnSelectedToChanged(string? value) => RealTimeCurrencyConvert();
    partial void OnDisplayChanged(string value) 
    {
        RealTimeCurrencyConvert();
        UpdateProgrammerDisplays();
        UpdateSplitDisplay();
    }
    partial void OnCaretPositionChanged(int value)
    {
        CursorOpacity = 1.0; 
        UpdateSplitDisplay();
    }

    private void UpdateSplitDisplay()
    {
        if (CaretPosition < 0) CaretPosition = 0;
        if (CaretPosition > Display.Length) CaretPosition = Display.Length;

        DisplayLeft = Display.Substring(0, CaretPosition);
        DisplayRight = Display.Substring(CaretPosition);
    }

    private CalculatorState GetState() => new(Display, Equation, null, "", _isNewInput);
    private void RestoreState(CalculatorState state)
    {
        Display = state.Display; Equation = state.Equation; _isNewInput = state.IsNewInput;
        CaretPosition = Display.Length;
    }
    private void ExecuteWithHistory(Action action) => _history.ExecuteCommand(new StateCommand(action, GetState, RestoreState));

    [RelayCommand] public void Undo() => _history.Undo();
    [RelayCommand] public void Redo() => _history.Redo();
    [RelayCommand] public void ToggleSecondaryMath() => IsSecondaryMathVisible = !IsSecondaryMathVisible;
    [RelayCommand] public void ToggleProgrammerSecondary() => IsProgrammerSecondaryVisible = !IsProgrammerSecondaryVisible;

    [RelayCommand(CanExecute = nameof(IsNotDateCalc))]
    public void MoveCaret(string direction)
    {
        if (int.TryParse(direction, out int dir))
        {
            int newPos = CaretPosition + dir;
            if (newPos >= 0 && newPos <= Display.Length)
                CaretPosition = newPos;
        }
    }

    [RelayCommand] public void ToggleAngleMode()
    {
        if (AngleModeText == "DEG") AngleModeText = "RAD";
        else if (AngleModeText == "RAD") AngleModeText = "GRAD";
        else AngleModeText = "DEG";
    }
    [RelayCommand] public void ToggleHyp() => IsHyperbolic = !IsHyperbolic;
    [RelayCommand] public void ToggleFE()
    {
        IsExponentialMode = !IsExponentialMode;
        if (double.TryParse(Display, CultureInfo.InvariantCulture, out double val)) Display = FormatResult(val);
    }

    [RelayCommand]
    public void SetBase(string baseStr)
    {
        if (!int.TryParse(baseStr, out int newBase)) return;
        try
        {
            long val = (long)EvaluateTokens(Tokenize(Display));
            CurrentBase = newBase;
            UpdateBasesState();
            Display = Convert.ToString(val, CurrentBase).ToUpper();
            _isNewInput = true;
            CaretPosition = Display.Length;
        }
        catch
        {
            CurrentBase = newBase;
            UpdateBasesState();
        }
    }

    private void UpdateBasesState()
    {
        IsHexActive = CurrentBase == 16;
        IsDecActive = CurrentBase == 10;
        IsOctActive = CurrentBase == 8;
        IsBinActive = CurrentBase == 2;

        IsOctEnabled = CurrentBase >= 8; 
        IsDecEnabled = CurrentBase >= 10;
        IsBinEnabled = true;
    }

    private void UpdateProgrammerDisplays()
    {
        if (!IsProgrammerVisible || Display == "Error") return;
        
        if (string.IsNullOrWhiteSpace(Display))
        {
            HexDisplay = "0"; DecDisplay = "0"; OctDisplay = "0"; BinDisplay = "0";
            return;
        }
        
        try
        {
            long val = (long)EvaluateTokens(Tokenize(Display));
            HexDisplay = Convert.ToString(val, 16).ToUpper();
            DecDisplay = Convert.ToString(val, 10);
            OctDisplay = Convert.ToString(val, 8);
            
            string bin = Convert.ToString(val, 2);
            BinDisplay = bin.PadLeft(Math.Max(1, bin.Length), '0');
        }
        catch { }
    }

    [RelayCommand]
    public void Memory(string action)
    {
        if (Display == "Error") return;
        double currentVal = 0;
        try { currentVal = EvaluateTokens(Tokenize(Display)); } catch { return; }

        switch (action)
        {
            case "MC": MemoryValue = 0; HasMemory = false; break;
            case "MR": if (HasMemory) { Display = FormatResult(MemoryValue); _isNewInput = true; CaretPosition = Display.Length; } break;
            case "MS": MemoryValue = currentVal; HasMemory = true; _isNewInput = true; break;
            case "M+": MemoryValue += currentVal; HasMemory = true; _isNewInput = true; break;
            case "M-": MemoryValue -= currentVal; HasMemory = true; _isNewInput = true; break;
        }
    }

    private string FormatResult(double result)
    {
        if (double.IsNaN(result) || double.IsInfinity(result)) return "Error";
        
        if (IsProgrammerVisible) return Convert.ToString((long)result, CurrentBase).ToUpper();
        if (IsExponentialMode) return result.ToString("E5", CultureInfo.InvariantCulture);
        
        return Math.Round(result, 10).ToString(CultureInfo.InvariantCulture); 
    }

    private bool TryParseNumber(string token, out double result)
    {
        if (IsProgrammerVisible)
        {
            try {
                bool isNeg = token.StartsWith("-");
                string clean = isNeg ? token.Substring(1) : token;
                long val = Convert.ToInt64(clean, CurrentBase);
                if (isNeg) val = -val;
                result = val;
                return true;
            } catch {
                result = 0; return false;
            }
        }
        return double.TryParse(token, CultureInfo.InvariantCulture, out result);
    }

    [RelayCommand(CanExecute = nameof(IsNotDateCalc))]
    public void Digit(string digit)
    {
        if (IsCurrencyVisible && (string.IsNullOrEmpty(SelectedFrom) || string.IsNullOrEmpty(SelectedTo)))
        {
            Equation = "Спочатку оберіть валюти!"; return;
        }

        if (IsProgrammerVisible)
        {
            if (CurrentBase == 2 && !Regex.IsMatch(digit, "^[01()]$")) return;
            if (CurrentBase == 8 && !Regex.IsMatch(digit, "^[0-7()]$")) return;
            if (CurrentBase == 10 && !Regex.IsMatch(digit, "^[0-9()]$")) return;
            if (CurrentBase == 16 && !Regex.IsMatch(digit, "^[0-9A-F()]$")) return;
        }
        else if (Regex.IsMatch(digit, "^[A-F]$")) return;

        ExecuteWithHistory(() =>
        {
            if (Display == "Error") { Display = IsProgrammerVisible ? "" : "0"; CaretPosition = Display.Length; }

            if (CaretPosition < 0) CaretPosition = 0;
            if (CaretPosition > Display.Length) CaretPosition = Display.Length;

            if (_isNewInput)
            {
                Display = digit == "00" ? "0" : digit;
                CaretPosition = Display.Length;
                _isNewInput = false;
            }
            else
            {
                if (IsProgrammerVisible)
                {
                    Display = Display.Insert(CaretPosition, digit);
                    CaretPosition += digit.Length;
                }
                else
                {
                    if (Display == "0" && digit != "(" && digit != ")")
                    {
                        if (digit == "0" || digit == "00")
                        {
                            Display = "0";
                            CaretPosition = 1;
                        }
                        else
                        {
                            Display = digit;
                            CaretPosition = digit.Length;
                        }
                    }
                    else
                    {
                        Display = Display.Insert(CaretPosition, digit);
                        CaretPosition += digit.Length;
                    }
                }
            }
        });
    }

    [RelayCommand]
    public void Dot()
    {
        if (IsProgrammerVisible) return;
        ExecuteWithHistory(() =>
        {
            if (_isNewInput) 
            { 
                Display = "0."; 
                CaretPosition = 2; 
                _isNewInput = false; 
                return; 
            }
            
            if (CaretPosition < 0) CaretPosition = 0;
            if (CaretPosition > Display.Length) CaretPosition = Display.Length;

            string leftPart = Display.Substring(0, CaretPosition);
            string rightPart = Display.Substring(CaretPosition);
            var tokens = Tokenize(leftPart);
            string currentNumber = tokens.Count > 0 ? tokens.Last() : "";

            if (!currentNumber.Contains(".")) 
            {
                bool needsZero = leftPart.Length == 0 || !char.IsDigit(leftPart.Last());

                if (needsZero)
                {
                    Display = leftPart + "0." + rightPart;
                    CaretPosition += 2;
                }
                else
                {
                    Display = leftPart + "." + rightPart;
                    CaretPosition += 1;
                }
            }
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotDateCalc))]
    public void Backspace() => ExecuteWithHistory(() =>
    {
        if (_isNewInput || Display == "Error" || CaretPosition <= 0) return;

        var textOps = new[] { "mod", "yroot", "logy", "<<", ">>", "AND", "OR", "XOR", "RoL", "RoR" };
        
        string leftPart = Display.Substring(0, CaretPosition);
        string rightPart = Display.Substring(CaretPosition);

        string? opToDelete = textOps.FirstOrDefault(o => leftPart.EndsWith(o));

        if (opToDelete != null)
        {
            leftPart = leftPart.Substring(0, leftPart.Length - opToDelete.Length);
            CaretPosition -= opToDelete.Length;
        }
        else
        {
            leftPart = leftPart.Substring(0, leftPart.Length - 1);
            CaretPosition -= 1;
        }

        Display = leftPart + rightPart;

        if (Display == "" || Display == "-") 
        { 
            Display = IsProgrammerVisible ? "" : "0"; 
            _isNewInput = true; 
            CaretPosition = Display.Length;
        }
    });

    [RelayCommand(CanExecute = nameof(IsNotDateCalc))]
    public void Clear() 
    {
        if (Six_sevenVisible)
        {
            Six_sevenVisible = false;
            return;
        }

        ExecuteWithHistory(() =>
        {
            Display = IsProgrammerVisible ? "" : "0"; 
            Equation = ""; _isNewInput = true;
            CaretPosition = Display.Length;
            _lastOperator = ""; _lastRightOperand = null;
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotDateCalc))]
    public void Operator(string op)
    {
        ExecuteWithHistory(() =>
        {
            if (Display == "Error") return;

            if (Display == "") 
            {
                Display = "0";
                CaretPosition = 1;
            }

            _isNewInput = false;

            if (CaretPosition < 0) CaretPosition = 0;
            if (CaretPosition > Display.Length) CaretPosition = Display.Length;

            string leftPart = Display.Substring(0, CaretPosition);
            string rightPart = Display.Substring(CaretPosition);

            var allOps = new[] { "+", "-", "×", "÷", "^", "mod", "yroot", "logy", "<<", ">>", "AND", "OR", "XOR", "RoL", "RoR" };

            string? prevOp = allOps.FirstOrDefault(o => leftPart.EndsWith(o));
            
            if (prevOp != null) leftPart = leftPart.Substring(0, leftPart.Length - prevOp.Length) + op;
            else leftPart += op;

            Display = leftPart + rightPart; 
            CaretPosition = leftPart.Length;
        });
    }

    [RelayCommand(CanExecute = nameof(IsNotDateCalc))]
    public void Calculate()
    {
        if (IsCurrencyVisible) return;
        ExecuteWithHistory(() =>
        {
            if (Display == "Error") return;

            if (_isNewInput && !string.IsNullOrEmpty(_lastOperator) && _lastRightOperand != null)
            {
                string opForDisplay = _lastOperator.Replace("*", "×").Replace("/", "÷");
                string rightOpStr = IsProgrammerVisible 
                    ? Convert.ToString((long)_lastRightOperand.Value, CurrentBase).ToUpper() 
                    : _lastRightOperand.Value.ToString(CultureInfo.InvariantCulture);
                
                Equation = $"{Display}{opForDisplay}{rightOpStr}";
                try
                {
                    double result = EvaluateTokens(Tokenize(Equation));

                    if(result == 67) Six_sevenVisible = true;

                    Display = FormatResult(result);
                    CaretPosition = Display.Length;
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
                    if (new[]{"*","/","+","-","^","mod","yroot","logy","<<",">>", "AND", "OR", "XOR", "RoL", "RoR"}.Contains(possibleOp) && 
                        TryParseNumber(tokens.Last(), out double right))
                    {
                        _lastOperator = possibleOp;
                        _lastRightOperand = right;
                    }
                }

                double result = EvaluateTokens(tokens);
                if(result == 67) Six_sevenVisible = true;
                Display = FormatResult(result);
                CaretPosition = Display.Length;
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
            if (Display.StartsWith("-")) 
            {
                Display = Display.Substring(1);
                if (CaretPosition > 0) CaretPosition--;
            }
            else if (Display != "0" && Display != "") 
            {
                Display = "-" + Display;
                CaretPosition++;
            }
            return;
        }

        try
        {
            double val = EvaluateTokens(Tokenize(Display));
            double res = 0;
            double angleFactor = AngleModeText == "DEG" ? Math.PI / 180.0 : (AngleModeText == "GRAD" ? Math.PI / 200.0 : 1.0);

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
                case "not": res = (double)(~(long)val); break; 
                
                case "sin": res = IsHyperbolic ? Math.Sinh(val)/2 : Math.Sin(val * angleFactor); break;
                case "cos": res = IsHyperbolic ? Math.Cosh(val)/2 : Math.Cos(val * angleFactor); break;
                case "tan": res = IsHyperbolic ? Math.Tanh(val) : Math.Tan(val * angleFactor); break;
                case "asin": res = IsHyperbolic ? Math.Asinh(val) : Math.Asin(val) / angleFactor; break; 
                case "acos": res = IsHyperbolic ? Math.Acosh(val) : Math.Acos(val) / angleFactor; break; 
                case "atan": res = IsHyperbolic ? Math.Atanh(val) : Math.Atan(val) / angleFactor; break; 
                
                default: res = _engine.CalculateScientific(val, func); break;
            }

            string funcPrefix = IsHyperbolic ? "h" : "";
            string funcName = func == "cube" ? "cube" : func == "cbrt" ? "cbrt" : func == "asin" ? $"asin{funcPrefix}" : func == "acos" ? $"acos{funcPrefix}" : func == "atan" ? $"atan{funcPrefix}" : func == "pow2" ? "2^" : func;
            if (new[] { "sin", "cos", "tan" }.Contains(func)) funcName = func + funcPrefix;

            Equation = $"{funcName}({Display})";
            Display = double.IsNaN(res) || double.IsInfinity(res) ? "Error" : FormatResult(res);
            CaretPosition = Display.Length;
            _isNewInput = true;
        }
        catch { Display = "Error"; }
    });

    private List<string> Tokenize(string expr)
    {
        expr = expr.Replace(" ", "").Replace("×", "*").Replace("÷", "/").Replace(",", ".");
        var rawTokens = Regex.Split(expr, @"(<<|>>|RoL|RoR|AND|OR|XOR|[\*\/\+\-\^\(\)]|mod|yroot|logy)").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        var tokens = new List<string>();
        for (int i = 0; i < rawTokens.Count; i++)
        {
            if (rawTokens[i] == "-" && (i == 0 || new[]{"*","/","+","-","^","mod","yroot","logy","<<",">>", "RoL", "RoR", "AND", "OR", "XOR", "("}.Contains(tokens.LastOrDefault())))
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
            
            t.Insert(openIdx, IsProgrammerVisible ? Convert.ToString((long)subRes, CurrentBase).ToUpper() : subRes.ToString(CultureInfo.InvariantCulture));
        }

        if (t.Count % 2 == 0 && t.Count > 0 && new[]{"*","/","+","-","^","mod","yroot","logy","<<",">>", "RoL", "RoR", "AND", "OR", "XOR"}.Contains(t.Last())) 
            t.RemoveAt(t.Count - 1); 

        ProcessOperations(t, new[] { "<<", ">>", "RoL", "RoR" });
        ProcessOperations(t, new[] { "^", "yroot", "logy" });
        ProcessOperations(t, new[] { "*", "/", "mod" });
        ProcessOperations(t, new[] { "+", "-" });
        ProcessOperations(t, new[] { "AND" });
        ProcessOperations(t, new[] { "XOR" });
        ProcessOperations(t, new[] { "OR" });

        if (t.Count > 0 && TryParseNumber(t[0], out double result)) return result;
        return 0;
    }

    private void ProcessOperations(List<string> tokens, string[] ops)
    {
        for (int i = 1; i < tokens.Count - 1; i += 2)
        {
            if (Array.IndexOf(ops, tokens[i]) >= 0)
            {
                if (TryParseNumber(tokens[i - 1], out double left) && TryParseNumber(tokens[i + 1], out double right))
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
                        case "<<": res = (double)((long)left << (int)right); break;
                        case ">>": res = (double)((long)left >> (int)right); break;
                        case "AND": res = (double)((long)left & (long)right); break;
                        case "OR": res = (double)((long)left | (long)right); break;
                        case "XOR": res = (double)((long)left ^ (long)right); break;
                        case "RoL": 
                            int shiftL = (int)right % 64;
                            if (shiftL < 0) shiftL += 64; 
                            ulong uLeftL = (ulong)(long)left;
                            res = shiftL == 0 ? left : (double)(long)((uLeftL << shiftL) | (uLeftL >> (64 - shiftL)));
                            break;
                        case "RoR":
                            int shiftR = (int)right % 64;
                            if (shiftR < 0) shiftR += 64;
                            ulong uLeftR = (ulong)(long)left;
                            res = shiftR == 0 ? left : (double)(long)((uLeftR >> shiftR) | (uLeftR << (64 - shiftR)));
                            break;
                    }
                    
                    tokens[i - 1] = IsProgrammerVisible ? Convert.ToString((long)res, CurrentBase).ToUpper() : res.ToString(CultureInfo.InvariantCulture);
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
        IsStandardVisible = false; IsScientificVisible = false; IsCurrencyVisible = false; IsProgrammerVisible = false; IsDateCalcVisible = false;
        IsMainDisplayVisible = mode != "DateCalc";
        
        Equation = ""; 
        Display = mode == "Programmer" ? "" : "0"; 
        _isNewInput = true;
        CaretPosition = Display.Length;

        switch (mode)
        {
            case "Standard": IsStandardVisible = true; ModeButtonText = "Звичайний"; break;
            case "Scientific": IsScientificVisible = true; ModeButtonText = "Інженерний"; break;
            case "Programmer": 
                IsProgrammerVisible = true; 
                ModeButtonText = "Програміст"; 
                CurrentBase = 10;
                IsProgrammerSecondaryVisible = false; 
                UpdateBasesState();
                UpdateProgrammerDisplays();
                break;
            case "Currency":
                IsCurrencyVisible = true;
                IsStandardVisible = true; 
                ModeButtonText = "Валюти";
                if (AvailableCurrencies.Count == 0)
                {
                    Equation = "Завантаження..."; await _currencyService.FetchRatesAsync();
                    AvailableCurrencies = new ObservableCollection<string>(_currencyService.GetCurrencies());
                }
                RealTimeCurrencyConvert(); break;
            case "DateCalc":
                IsDateCalcVisible = true;
                OnPropertyChanged(nameof(IsCalculatorKeyboardActive));
                ModeButtonText = "Обчислення дат";
                UpdateDateDifference();
                break;
        }

        OnPropertyChanged(nameof(IsNotDateCalc));
        UpdateCommandStates();
    }

    [RelayCommand] public void ToggleTheme()
    {
        var app = Application.Current;
        if (app != null) app.RequestedThemeVariant = app.RequestedThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    }
}