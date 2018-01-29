// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Collections.Generic;
using MetriCam2.Enums;

namespace MetriCam2.Attributes
{
    /// <summary>
    /// MetriCam2 marker attribute. Indicates that an assembly contains implementations of <see cref="MetriCam2.Camera"/>.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <para>
    /// C#<br/>
    /// Put the following line in the camera project's AssemblyInfo.cs:
    /// <code>
    /// [assembly: MetriCam2.Attributes.ContainsCameraImplementations]
    /// </code>
    /// </para>
    /// <para>
    /// C++/CLI<br/>
    /// Put the following line in the camera project's AssemblyInfo.cpp:
    /// <code>
    /// [assembly:MetriCam2::Attributes::ContainsCameraImplementations];
    /// </code>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class ContainsCameraImplementations : System.Attribute { }

    /// <summary>
    /// Used to add a list of (native) dependencies to a MetriCam2 compatible assembly.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <para>
    /// C#<br/>
    /// Put the following line in the camera project's AssemblyInfo.cs:
    /// <code>
    /// [assembly: MetriCam2.Attributes.NativeDependencies("dependency1.dll", "dependency2.dll")]
    /// </code>
    /// </para>
    /// <para>
    /// C++/CLI<br/>
    /// Put the following line in the camera project's AssemblyInfo.cpp:
    /// <code>
    /// [assembly:MetriCam2::Attributes::NativeDependencies("dependency1.dll", "dependency2.dll")];
    /// </code>
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class NativeDependencies : System.Attribute
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public NativeDependencies() : this(string.Empty) { }
        /// <summary>
        /// Constructor with references
        /// </summary>
        /// <param name="refs"></param>
        public NativeDependencies(params string[] refs) { }
    };

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DescriptionAttribute : Attribute
    {
        private string _name;
        private string _desc;

        private void Init(string propertyName, string propertyDescription)
        {
            _name = propertyName;
            _desc = propertyDescription;
        }

        public DescriptionAttribute(string propertyName)
        {
            Init(propertyName, "No description");
        }

        public DescriptionAttribute(string propertyName, string propertyDescription)
        {
            Init(propertyName, propertyDescription);
        }

        public string Name { get => _name; }
        public string Description { get => _desc; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AccessStateAttribute : Attribute
    {
        private ConnectionStates _readable;
        private ConnectionStates _writable;

        public ConnectionStates ReadableWhen { get => _readable; }
        public ConnectionStates WritableWhen { get => _writable; }

        public AccessStateAttribute(ConnectionStates readableWhen)
        {
            _readable = readableWhen;
        }

        public AccessStateAttribute(ConnectionStates readableWhen, ConnectionStates writeableWhen)
        {
            _readable = readableWhen;
            _writable = writeableWhen;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class UnitAttribute : Attribute
    {
        public string Unit { get; private set; }

        public UnitAttribute(string unit)
        {
            Unit = unit;
        }

        public UnitAttribute(Unit unit)
        {
            switch(unit)
            {
                case Enums.Unit.Millimeter:
                    Unit = "mm";
                    return;

                case Enums.Unit.Centimeter:
                    Unit = "cm";
                    return;

                case Enums.Unit.Meter:
                    Unit = "m";
                    return;

                case Enums.Unit.Kilometer:
                    Unit = "km";
                    return;

                case Enums.Unit.Pixel:
                    Unit = "px";
                    return;

                case Enums.Unit.FPS:
                    Unit = "fps";
                    return;

                case Enums.Unit.DegreeCelsius:
                    Unit = "°C";
                    return;
            }
        }
    }

    public class Range<T> where T : IComparable, IConvertible
    {
        public T Minimum;
        public T Maximum;

        public Range(T min, T max)
        {
            if (min.CompareTo(min) > 0)
                throw new ArgumentException("The Maximum needs to exceed the Minimum to be a valid Range");

            Minimum = min;
            Maximum = max;
        }
    }

    public class ConstrainAttribute : Attribute
    {
        public bool DataIsPropertyName { get; protected set; } = false;
        public string StringRepresentationFunc { get; protected set; } = null;
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RangeAttribute : ConstrainAttribute
    {
        public object Range { get; private set; }

        public RangeAttribute(float min, float max)
        {
            Range = new Range<float>(min, max);
        }

        public RangeAttribute(Range<float> range)
        {
            Range = range;
        }

        public RangeAttribute(double min, double max)
        {
            Range = new Range<double>(min, max);
        }

        public RangeAttribute(Range<double> range)
        {
            Range = range;
        }

        public RangeAttribute(int min, int max)
        {
            Range = new Range<int>(min, max);
        }

        public RangeAttribute(Range<int> range)
        {
            Range = range;
        }

        public RangeAttribute(string range)
        {
            Range = range;
            DataIsPropertyName = true;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AllowedValueListAttribute : ConstrainAttribute
    {
        public object AllowedValues { get; private set; }

        public AllowedValueListAttribute(List<float> allowedValues, string toStringFunc = null)
        {
            AllowedValues = allowedValues;
            StringRepresentationFunc = toStringFunc;
        }

        public AllowedValueListAttribute(List<double> allowedValues, string toStringFunc = null)
        {
            AllowedValues = allowedValues;
            StringRepresentationFunc = toStringFunc;
        }

        public AllowedValueListAttribute(List<int> allowedValues)
        {
            AllowedValues = allowedValues;
        }

        public AllowedValueListAttribute(List<string> allowedValues)
        {
            AllowedValues = allowedValues;
        }

        public AllowedValueListAttribute(Type enumType)
        {
            if (!enumType.IsEnum)
                throw new ArgumentException("Type does not represent an enum!");

            AllowedValues = new List<string>(Enum.GetNames(enumType));
        }

        public AllowedValueListAttribute(string propertyName, string toStringFunc = null)
        {
            AllowedValues = propertyName;
            DataIsPropertyName = true;
            StringRepresentationFunc = toStringFunc;
        }
    }
}