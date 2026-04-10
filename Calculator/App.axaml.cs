using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Calculator.Views;
using Calculator.ViewModels;
using Calculator.Services;

namespace Calculator;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Важливо! Змінюємо мод завершення, щоб програма не закрилася
            // після того, як ми закриємо вікно вибору стилю.
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

            var styleViewModel = new StyleSelectionViewModel();
            var styleWindow = new StyleSelectionWindow
            {
                DataContext = styleViewModel
            };

            // Коли користувач натиснув "Запустити"
            styleViewModel.OnLaunchRequested += (selectedStyleUri) =>
            {
                // Застосовуємо обрану тему глобально
                ThemeManager.ApplyTheme(selectedStyleUri);

                // Створюємо і показуємо основний калькулятор
                var mainWindow = new MainWindow();
                mainWindow.Show();
                
                // Перепризначаємо MainWindow і закриваємо стартове вікно
                desktop.MainWindow = mainWindow;
                styleWindow.Close();
            };

            desktop.MainWindow = styleWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}