using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Metrilus.Util;

namespace MetriCam2
{
    public class TypeConversion
    {
        public static string GetAsGoodString(object value)
        {
            return GetAsGoodString(value, value.GetType());
        }

        public static string GetAsGoodString(object value, Type valueType)
        {
            string valueAsString;
            if (valueType == typeof(float))
            {
                valueAsString = ((float)value).ToString("R", CultureInfo.InvariantCulture);
            }
            else if (valueType == typeof(double))
            {
                valueAsString = ((double)value).ToString("R", CultureInfo.InvariantCulture);
            }
            else if (valueType == typeof(Point2i))
            {
                valueAsString = string.Format("{0}x{1}", ((Point2i)value).X, ((Point2i)value).Y);
            }
            else
            {
                valueAsString = value.ToString();
            }
            return valueAsString;
        }

        public static Point2i ResolutionToPoint2i(string s)
        {
            string[] stringValue = s.Split('x');
            return new Point2i(int.Parse(stringValue[0]), int.Parse(stringValue[1]));
        }

        public static string Point2iToResolution(Point2i p)
        {
            return string.Format("{0}x{1}", p.X, p.Y);
        }
    }
}
