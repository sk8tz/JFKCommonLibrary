﻿using System;
using System.Windows;
using System.Windows.Data;

namespace JFKCommonLibrary.WPF.Converters
{
    public class ListIntToVisibilityConverter : ValueConverter
    {
        public string VisibleValue { get; set; }

        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var wrt = VisibleValue.Split(',');

            foreach (string s in wrt)
            {
                if (System.Convert.ToInt32(value) == System.Convert.ToInt32(s))
                    return Visibility.Visible;
            }
            return Visibility.Hidden;            
        }

        public override object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
