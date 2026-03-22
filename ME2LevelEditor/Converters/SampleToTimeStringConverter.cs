using System;
using System.Globalization;
using System.Windows.Data;

namespace ME2LevelEditor.Converters
{
    public class SampleToTimeStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return "0:00.0 | Sample: 0";

            int sampleId = values[0] is int s ? s : 0;
            int totalSamples = values[1] is int t ? t : 0;
            double durationSeconds = values[2] is double d ? d : 0.0;

            if (totalSamples <= 0)
                return "0:00.0 | Sample: 0";

            double seconds = (double)sampleId / totalSamples * durationSeconds;
            int minutes = (int)(seconds / 60);
            double secs = seconds - minutes * 60;

            return $"{minutes}:{secs:00.0} | Sample: {sampleId}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
