 
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ACControllerServer.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public string TrueColor { get; set; } = "#2ed573";
        public string FalseColor { get; set; } = "#ff4757";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isTrue = value is bool b && b;
            return (Color)ColorConverter.ConvertFromString(isTrue ? TrueColor : FalseColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}