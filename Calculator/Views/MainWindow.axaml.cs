using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Calculator.ViewModels;
using Calculator.Services;
using Avalonia.Threading;

namespace Calculator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        var vm = new MainWindowViewModel();
        DataContext = vm;
        vm.OnReturnToStyleSelectionRequested += () =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var styleViewModel = new StyleSelectionViewModel();
                var styleWindow = new StyleSelectionWindow
                {
                    DataContext = styleViewModel
                };

                styleViewModel.OnLaunchRequested += (selectedStyleUri) =>
                {
                    ThemeManager.ApplyTheme(selectedStyleUri);
                    var newMainWindow = new MainWindow();
                    newMainWindow.Show();
                    desktop.MainWindow = newMainWindow;
                    styleWindow.Close();
                };

                styleWindow.Show();
                desktop.MainWindow = styleWindow;
                
                this.Close();
            }
        };
    }
}