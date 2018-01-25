﻿// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Collections.Generic;

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
        private Camera.ConnectionStates _readable;
        private Camera.ConnectionStates _writable;

        private void Init(string propertyName, string propertyDescription)
        {
            _name = propertyName;
            _desc = propertyDescription;
        }

        public DescriptionAttribute(string propertyName, string propertyDescription)
        {
            Init(propertyName, propertyDescription);
        }

        public DescriptionAttribute(string propertyName, string propertyDescription, Camera.ConnectionStates readableWhen)
        {
            Init(propertyName, propertyDescription);
            _readable = readableWhen;
        }

        public DescriptionAttribute(string propertyName, string propertyDescription, Camera.ConnectionStates readableWhen, Camera.ConnectionStates writableWhen)
        {
            Init(propertyName, propertyDescription);
            _readable = readableWhen;
            _writable = writableWhen;
        }

        public string Name { get => _name; }
        public string Description { get => _desc; }
        public Camera.ConnectionStates ReadableWhen { get => _readable; }
        public Camera.ConnectionStates WritableWhen { get => _writable; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class UnitAttribute : Attribute
    {
        public string Unit { get; private set; }

        public UnitAttribute(string unit)
        {
            Unit = unit;
        }
    }

    public class Range<T> where T : IComparable, IConvertible
    {
        public T Minimum;
        public T Maximum;

        public Range(T min, T max)
        {
            if (min.CompareTo(min) >= 0)
                throw new ArgumentException("The Maximum needs to exceed the Minimum to be a valid Range");

            Minimum = min;
            Maximum = max;
        }
    }

    public class ConstrainAttribute : Attribute
    {
        public Type DataType { get; protected set; }
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
            DataType = typeof(float);
        }

        public RangeAttribute(Range<float> range)
        {
            Range = range;
            DataType = typeof(float);
        }

        public RangeAttribute(double min, double max)
        {
            Range = new Range<double>(min, max);
            DataType = typeof(double);
        }

        public RangeAttribute(Range<double> range)
        {
            Range = range;
            DataType = typeof(double);
        }

        public RangeAttribute(int min, int max)
        {
            Range = new Range<int>(min, max);
            DataType = typeof(int);
        }

        public RangeAttribute(Range<int> range)
        {
            Range = range;
            DataType = typeof(int);
        }

        public RangeAttribute(string range, Type type)
        {
            Range = range;
            DataType = type;
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
            DataType = typeof(float);
            StringRepresentationFunc = toStringFunc;
        }

        public AllowedValueListAttribute(List<double> allowedValues, string toStringFunc = null)
        {
            AllowedValues = allowedValues;
            DataType = typeof(double);
            StringRepresentationFunc = toStringFunc;
        }

        public AllowedValueListAttribute(List<int> allowedValues)
        {
            AllowedValues = allowedValues;
            DataType = typeof(int);
        }

        public AllowedValueListAttribute(List<string> allowedValues)
        {
            AllowedValues = allowedValues;
            DataType = typeof(string);
        }

        public AllowedValueListAttribute(Type enumType)
        {
            if (!enumType.IsEnum)
                throw new ArgumentException("Type does not represent an enum!");

            AllowedValues = new List<string>(Enum.GetNames(enumType));
            DataType = enumType;
        }

        public AllowedValueListAttribute(string propertyName, Type type, string toStringFunc = null)
        {
            AllowedValues = propertyName;
            DataType = type;
            DataIsPropertyName = true;
            StringRepresentationFunc = toStringFunc;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SimpleTypeAttribute : Attribute
    {
        public Type PropertyType { get; private set; }

        public SimpleTypeAttribute(Type t)
        {
            PropertyType = t;
        }
    }
}