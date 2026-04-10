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

        // Підписуємось на команду з ViewModel
        vm.OnReturnToStyleSelectionRequested += () =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // 1. Створюємо вікно вибору стилю
                var styleViewModel = new StyleSelectionViewModel();
                var styleWindow = new StyleSelectionWindow
                {
                    DataContext = styleViewModel
                };

                // 2. Вказуємо йому, що робити, коли стиль оберуть знову
                styleViewModel.OnLaunchRequested += (selectedStyleUri) =>
                {
                    ThemeManager.ApplyTheme(selectedStyleUri);
                    var newMainWindow = new MainWindow();
                    newMainWindow.Show();
                    desktop.MainWindow = newMainWindow;
                    styleWindow.Close();
                };

                // 3. Показуємо вікно вибору стилю
                styleWindow.Show();
                desktop.MainWindow = styleWindow;
                
                // 4. Закриваємо поточне вікно калькулятора
                this.Close();
            }
        };
    }
}