using System;
using System.Globalization;
using System.Windows.Data;
using ME2LevelEditor.ViewModels;

namespace ME2LevelEditor.Converters
{
    public class ActiveToolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ActiveTool current && parameter is ActiveTool target)
                return current == target;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is ActiveTool tool)
                return tool;
            return Binding.DoNothing;
        }
    }
}
