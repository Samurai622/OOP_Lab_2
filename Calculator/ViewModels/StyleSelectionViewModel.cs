using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Calculator.Models;

namespace Calculator.ViewModels;

public partial class StyleSelectionViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ThemeItem> _availableThemes;
    [ObservableProperty] private ThemeItem? _selectedTheme;

    // Подія для сповіщення App.axaml.cs про необхідність запуску калькулятора
    public Action<string>? OnLaunchRequested;

    public StyleSelectionViewModel()
    {
        AvailableThemes = new ObservableCollection<ThemeItem>
        {
            new ThemeItem { Name = "Класичний (Pixel)", SourceUri = "avares://Calculator/Themes/PixelTheme.axaml" },
            new ThemeItem { Name = "Неоновий (Neon)", SourceUri = "avares://Calculator/Themes/NeonTheme.axaml" },
            new ThemeItem { Name = "Термінал Хакера", SourceUri = "avares://Calculator/Themes/HackerTheme.axaml" }
        };
        
        SelectedTheme = AvailableThemes[0];
    }

    [RelayCommand]
    public void LaunchCalculator()
    {
        if (SelectedTheme != null)
        {
            OnLaunchRequested?.Invoke(SelectedTheme.SourceUri);
        }
    }
}