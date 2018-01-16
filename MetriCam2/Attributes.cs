// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.ComponentModel.DataAnnotations;
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
    sealed public class FloatListAttribute : ValidationAttribute
    {
        private List<float> _allowedValues;

        public FloatListAttribute(List<float> allowedValues)
        {
            _allowedValues = allowedValues;
        }

        public override bool IsValid(object value)
        {
            if (value is float)
            {
                return _allowedValues.Contains((float)value);
            }
            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    sealed public class IntListAttribute : ValidationAttribute
    {
        private List<int> _allowedValues;

        public IntListAttribute(List<int> allowedValues)
        {
            _allowedValues = allowedValues;
        }

        public override bool IsValid(object value)
        {
            if (value is float)
            {
                return _allowedValues.Contains((int)value);
            }
            return false;
        }
    }
}