using System;
using System.Globalization;
using System.Windows.Data;

namespace B1TuneUp.Modules.ProcessStepsUi
{
    public class ProcessStepStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool done && done) return "Completo";
            return "Pendiente";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class MandatoryFlagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool mandatory && mandatory) return "Sí";
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
