using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ME2LevelEditor.Models;

namespace ME2LevelEditor.Converters
{
    public class IntensityToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush LowBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ecc71"));
        private static readonly SolidColorBrush NormalBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4a90d9"));
        private static readonly SolidColorBrush HighBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e94560"));
        private static readonly SolidColorBrush ExtremeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9b59b6"));

        static IntensityToBrushConverter()
        {
            LowBrush.Freeze();
            NormalBrush.Freeze();
            HighBrush.Freeze();
            ExtremeBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IntensityLevel level)
            {
                switch (level)
                {
                    case IntensityLevel.Low: return LowBrush;
                    case IntensityLevel.Normal: return NormalBrush;
                    case IntensityLevel.High: return HighBrush;
                    case IntensityLevel.Extreme: return ExtremeBrush;
                }
            }

            return NormalBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
