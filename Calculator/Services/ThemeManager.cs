using System;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;

namespace Calculator.Services;

public static class ThemeManager
{
    public static void ApplyTheme(string sourceUri)
    {
        var app = Application.Current;
        if (app != null)
        {
            app.Resources.MergedDictionaries.Clear();
            app.Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://Calculator/App.axaml"))
            {
                Source = new Uri(sourceUri)
            });
        }
    }
}