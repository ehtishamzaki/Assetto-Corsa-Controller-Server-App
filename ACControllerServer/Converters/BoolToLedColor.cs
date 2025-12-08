using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace ACControllerServer.Converters
{
    public class BoolToLedColor : IValueConverter
    {

        private static SolidColorBrush SuccessColor = Brushes.LimeGreen;

        private static SolidColorBrush ErrorColor = Brushes.Red; 

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return SuccessColor;
            else
                return ErrorColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
