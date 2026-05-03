using System;
using System.Linq;
using System.Windows;

namespace DevCleanerPro.Helpers;

public static class LanguageManager
{
    public static void ChangeLanguage(string cultureCode)
    {
        // El código esperado es "es-ES" o "en-US". Si no es "en-US", forzamos "es-ES" por defecto.
        string dictName = cultureCode == "en-US" ? "en-US" : "es-ES";
        var dictUri = new Uri($"/Resources/Languages/{dictName}.xaml", UriKind.Relative);

        // Buscar si ya existe un diccionario de idioma cargado (tienen "/Resources/Languages/" en su URI)
        var existingDict = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Resources/Languages/"));

        if (existingDict != null)
        {
            // Reemplazar el diccionario existente
            var index = Application.Current.Resources.MergedDictionaries.IndexOf(existingDict);
            Application.Current.Resources.MergedDictionaries[index] = new ResourceDictionary { Source = dictUri };
        }
        else
        {
            // Agregar el diccionario si no existe
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = dictUri });
        }
    }
}
