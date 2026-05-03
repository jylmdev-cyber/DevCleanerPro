using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DevCleanerPro.Helpers;

namespace DevCleanerPro.Converters;

/// <summary>
/// Convierte bytes (long) a formato legible: "1.24 GB", "340 MB", etc.
/// Uso en XAML: Binding="{Binding Size, Converter={StaticResource FileSizeConverter}}"
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes) return FileSizeFormatter.Format(bytes);
        if (value is double d) return FileSizeFormatter.Format((long)d);
        if (value is int i) return FileSizeFormatter.Format(i);
        if (value is string s && long.TryParse(s, out var parsed)) return FileSizeFormatter.Format(parsed);
        return value?.ToString() ?? "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Asigna un color de Brush basado en el tamaño del archivo.
/// - Menor a 10 MB → DevTextDisabled (gris)
/// - 10-100 MB → DevAccentYellow (amarillo)
/// - Mayor a 100 MB → DevAccentRed (rojo)
/// </summary>
public class FileSizeColorConverter : IValueConverter
{
    private static readonly SolidColorBrush SmallBrush = new(Color.FromRgb(0x8b, 0x94, 0x9e));   // gris
    private static readonly SolidColorBrush MediumBrush = new(Color.FromRgb(0xd2, 0x99, 0x22));  // amarillo
    private static readonly SolidColorBrush LargeBrush = new(Color.FromRgb(0xf8, 0x51, 0x49));   // rojo

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long bytes = value switch
        {
            long l => l,
            double d => (long)d,
            int i => i,
            _ => 0
        };
        const long mb10 = 10L * 1024 * 1024;
        const long mb100 = 100L * 1024 * 1024;
        return bytes >= mb100 ? LargeBrush : bytes >= mb10 ? MediumBrush : SmallBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Convierte true/false a Visibility.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}

/// <summary>
/// Invierte un booleano (para mostrar empty state cuando Items.Count == 0).
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count) return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (value is bool b) return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
