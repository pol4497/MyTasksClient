using System.Globalization;

namespace MyTasksClient.Infrastructure;

/// <summary>Turns true into false and anything else into true, so a view can show "the opposite of" a flag.</summary>
public sealed class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;
}