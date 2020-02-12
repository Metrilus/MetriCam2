using System;
using System.ComponentModel;
using System.Reflection;

namespace MetriCam2.Cameras
{
    /// <summary>
    /// ifm O3D3xx Trigger Modes
    /// </summary>
    public enum O3D3xxTriggerMode
    {
        FreeRun = 1,
        ProcessInterface = 2,
        PositiveEdge = 3,
        NegatigeEdge = 4,
        PositiveAndNegativeEdge = 5,
    }

    public enum O3D3xxIntegrationMode
    {
        [Description("low")]
        SingleIntegrationTime = 0,
        [Description("moderate")]
        TwoIntegrationTimes = 1,
        [Description("high")]
        ThreeIntegrationTimes = 2,
    }

    public enum O3D3xxBackgroundDistanceMode
    {
        [Description("lessthan5m")]
        LessThan5 = 0,
        [Description("upto30m")]
        UpTo30 = 1,
        [Description("morethan30m")]
        MoreThan30 = 2,
    }

    public static class EnumUtils
    {
        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }

        public static object GetEnum(Type type, string description)
        {
            Array allValues = Enum.GetValues(type);
            foreach (Enum value in allValues)
            {
                if(description == GetDescription(value))
                {
                    return Enum.Parse(type, value.ToString());
                }
            }

            return null;
        }
    }
}
