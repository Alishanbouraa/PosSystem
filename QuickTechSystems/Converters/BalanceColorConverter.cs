using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace QuickTechSystems.WPF.Converters
{
    public class BalanceColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal balance)
            {
                return balance < 0
                    ? new SolidColorBrush(Colors.Green)
                    : balance > 0
                        ? new SolidColorBrush(Colors.Red)
                        : new SolidColorBrush(Color.FromRgb(102, 102, 102));
            }
            return new SolidColorBrush(Color.FromRgb(102, 102, 102));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}