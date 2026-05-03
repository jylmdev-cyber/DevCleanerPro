using System;
using System.Linq;
using System.Windows;
using Wpf.Ui.Appearance;

namespace DevCleanerPro.Helpers;

public static class ThemeManagerHelper
{
    public static void ApplyTheme(string themeString)
    {
        ApplicationTheme appTheme = themeString switch
        {
            "Light" => ApplicationTheme.Light,
            "Dark" => ApplicationTheme.Dark,
            _ => ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light
        };

        // 1. Aplicar el tema base de WPF-UI (controles nativos)
        ApplicationThemeManager.Apply(appTheme);

        // 2. Aplicar nuestro diccionario de colores personalizados
        string dictName = appTheme == ApplicationTheme.Light ? "LightTheme" : "DarkTheme";
        var dictUri = new Uri($"/Resources/Themes/{dictName}.xaml", UriKind.Relative);

        var existingDict = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Resources/Themes/"));

        if (existingDict != null)
        {
            var index = Application.Current.Resources.MergedDictionaries.IndexOf(existingDict);
            Application.Current.Resources.MergedDictionaries[index] = new ResourceDictionary { Source = dictUri };
        }
        else
        {
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = dictUri });
        }
    }
}
