using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MuseumAccountingSystem.Helpers
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;

            if (status == "Выдан")
                return Brushes.Red;
            else if (status == "В наличии")
                return Brushes.Green;
            else if (status == "Возвращен")
                return Brushes.Gray;
            else
                return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}