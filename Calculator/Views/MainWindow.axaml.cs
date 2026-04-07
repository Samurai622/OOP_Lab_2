using Avalonia.Controls;
using Calculator.ViewModels;

namespace Calculator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}