using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using MetriCam2.Enums;
using MetriCam2.Attributes;
using Metrilus.Util;

namespace MetriCam2
{
    /// <summary>
    /// Basic parameter descriptor.
    /// </summary>
    /// <seealso cref="ParamDesc&lt;T&gt;"/>
    /// <seealso cref="ListParamDesc&lt;T&gt;"/>
    /// <seealso cref="RangeParamDesc&lt;T&gt;"/>
    /// <seealso cref="GetParameter"/>
    /// <seealso cref="GetParameters"/>
    /// <seealso cref="SetParameter"/>
    /// <seealso cref="SetParameters"/>
    public abstract class ParamDesc
    {
        #region Constants
        /// <summary>Suffix to identify the property with the parameter descriptor.</summary>
        public const string DescriptorSuffix = "Desc";
        /// <summary>Prefix to identify a possible Auto* parameter.</summary>
        private const string AutoPrefix = "Auto";
        #endregion

        #region Public Properties
        /// <summary>
        /// Type of the parameter.
        /// </summary>
        /// <remarks>
        /// Common types are: bool, int, float, string, enum/list
        /// </remarks>
        public Type Type { get; set; }
        /// <summary>Name of the parameter.</summary>
        public string Name { get; internal set; }
        /// <summary>Value of the parameter.</summary>
        /// <remarks>May be null if the parameter is not readable.</remarks>
        public object Value { get; set; }
        /* GUI */
        /// <summary>Group name.</summary>
        /// <remarks>Currently unused. May be used by GUI applications to group parameters.</remarks>
        public string GroupName { get; set; }
        /// <summary>Description of the parameter.</summary>
        /// <remarks>May be displayed by GUI applications, e.g. in form of a tool tip, to help users.</remarks>
        public string Description { get; set; }
        /// <summary>Unit of the parameter value.</summary>
        /// <example>For example: "mm", "px", "%", "1/s"</example>
        public string Unit { get; set; }
        /* Flags */
        /// <summary>Indicates if there is a corresponding Auto* parameter.</summary>
        /// <remarks>
        /// This property is set internally in <see cref="GetParameter"/>.
        /// Rename to HasAutoParameter?
        /// </remarks>
        public bool SupportsAutoMode { get; internal set; }
        /// <summary>Indicates if the parameter is currently readable.</summary>
        /// <remarks>Readability may change due to Connect/Disconnect. The ParamDesc will not be updated.</remarks>
        public bool IsReadable { get; internal set; }
        /// <summary>Indicates if the parameter is currently writable.</summary>
        /// <remarks>Writability may change due to Connect/Disconnect. The ParamDesc will not be updated.</remarks>
        public bool IsWritable { get; internal set; }
        /// <summary>Defines when the parameter is readable.</summary>
        /// <remarks>Will be set only in the camera wrapper implementation.</remarks>
        public ConnectionStates ReadableWhen { internal get; set; }
        /// <summary>Defines when the parameter is writable.</summary>
        /// <remarks>Will be set only in the camera wrapper implementation.</remarks>
        public ConnectionStates WritableWhen { internal get; set; }
        /* Dependency resolution */
        // public Setting dependsOn / overrides
        // public Setting setAfter
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor. Does nothing.
        /// </summary>
        protected ParamDesc() { }

        /// <summary>
        /// Copy constructor. Copies all properties from <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The <see cref="ParamDesc"/> object to be copied.</param>
        protected ParamDesc(ParamDesc other)
        {
            this.Type = other.Type;
            this.Name = other.Name;
            this.Value = other.Value;
            /* GUI */
            this.GroupName = other.GroupName;
            this.Description = other.Description;
            this.Unit = other.Unit;
            /* Flags */
            this.SupportsAutoMode = other.SupportsAutoMode;
            this.IsReadable = other.IsReadable;
            this.IsWritable = other.IsWritable;
            this.ReadableWhen = other.ReadableWhen;
            this.WritableWhen = other.WritableWhen;
        }

        public static ParamDesc Create(
            Type type, 
            string name, 
            string description, 
            string unit,
            object value,
            ConnectionStates readableWhen,
            ConnectionStates writabelWhen)
        {
            ParamDesc desc;

            if (type == typeof(int))
                desc = new ParamDesc<int>();
            else if (type == typeof(float))
                desc = new ParamDesc<float>();
            else if (type == typeof(double))
                desc = new ParamDesc<double>();
            else if (type == typeof(byte))
                desc = new ParamDesc<byte>();
            else if (type == typeof(short))
                desc = new ParamDesc<short>();
            else if (type == typeof(long))
                desc = new ParamDesc<long>();
            else if (type == typeof(uint))
                desc = new ParamDesc<uint>();
            else if (type == typeof(ulong))
                desc = new ParamDesc<ulong>();
            else if (type == typeof(ushort))
                desc = new ParamDesc<ushort>();
            else if (type == typeof(bool))
                desc = new ParamDesc<bool>();
            else if (type == typeof(string))
                desc = new ParamDesc<string>();
            else if (type == typeof(Point3f))
                desc = new ParamDesc<Point3f>();
            else
                throw new ArgumentException(string.Format("Type {0} not supported", type.ToString()));

            desc.init(type, name, description, unit, value, readableWhen, writabelWhen);
            return desc;
        }

        public static ParamDesc CreateRange(
            Type type,
            object range,
            string name,
            string description,
            string unit,
            object value,
            ConnectionStates readableWhen,
            ConnectionStates writabelWhen)
        {
            ParamDesc desc;

            if (type == typeof(int))
                desc = createRange<int>(range);
            else if (type == typeof(float))
                desc = createRange<float>(range);
            else if (type == typeof(double))
                desc = createRange<double>(range);
            else if (type == typeof(byte))
                desc = createRange<byte>(range);
            else if (type == typeof(short))
                desc = createRange<short>(range);
            else if (type == typeof(long))
                desc = createRange<long>(range);
            else if (type == typeof(uint))
                desc = createRange<uint>(range);
            else if (type == typeof(ulong))
                desc = createRange<ulong>(range);
            else if (type == typeof(ushort))
                desc = createRange<ushort>(range);
            else
                throw new ArgumentException(string.Format("Type {0} not supported", type.ToString()));

            desc.init(type, name, description, unit, value, readableWhen, writabelWhen);
            return desc;
        }

        private static RangeParamDesc<T> createRange<T>(object range) where T : IComparable, IConvertible
        {
            Range<T> typeRange = (Range<T>)range;
            return new RangeParamDesc<T>(typeRange.Minimum, typeRange.Maximum);
        }

        public static ParamDesc CreateList(
            Type type,
            object list,
            string name,
            string description,
            string unit,
            object value,
            ConnectionStates readableWhen,
            ConnectionStates writabelWhen)
        {
            ParamDesc desc;

            if (type == typeof(int))
                desc = new ListParamDesc<int>((List<int>)list);
            else if (type == typeof(float))
                desc = new ListParamDesc<float>((List<float>)list);
            else if (type == typeof(double))
            {
                List<float> floatList = ((List<double>)list).Select<double, float>(i => (float)i).ToList();
                desc = new ListParamDesc<double>(floatList);
            }
            else if (type.IsEnum)
                desc = new ListParamDesc<string>(type);
            else if (type == typeof(Point2i))
            {
                List<string> stringList = ((List<Point2i>)list).Select<Point2i, string>(p => TypeConversion.Point2iToResolution(p)).ToList();
                desc = new ListParamDesc<Point2i>(stringList);
            }
            else
                throw new ArgumentException(string.Format("Type {0} not supported", type.ToString()));

            desc.init(type, name, description, unit, value, readableWhen, writabelWhen);
            return desc;
        }

        private void init(
            Type type,
            string name,
            string description,
            string unit,
            object value,
            ConnectionStates readableWhen,
            ConnectionStates writabelWhen)
        {
            this.Type = type;
            this.Name = name;
            this.Value = value;
            this.Description = description;
            this.ReadableWhen = readableWhen;
            this.WritableWhen = writabelWhen;
            this.Unit = unit;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Decide if the ParamDesc is an Auto*-parameter.
        /// </summary>
        /// <returns></returns>
        /// <remarks>The test is only based on the name.</remarks>
        public bool IsAutoParameter()
        {
            return ParamDesc.IsAutoParameterName(this.Name);
        }
        /// <summary>
        /// Decide if a parameter name is an Auto*-parameter.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <returns></returns>
        /// <remarks>The test is only based on the name.</remarks>
        public static bool IsAutoParameterName(string paramName)
        {
            return paramName.StartsWith(AutoPrefix);
        }

        /// <summary>
        /// Get the Auto*-parameter name for a the ParamDesc.
        /// </summary>
        /// <returns></returns>
        /// <remarks>If the ParamDesc is already an Auto*-parameter then its own name is returned.</remarks>
        public string GetAutoParameterName()
        {
            return ParamDesc.GetAutoParameterName(this.Name);
        }
        /// <summary>
        /// Get the Auto*-parameter name for a given base parameter name.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <returns></returns>
        /// <remarks>If <paramref name="paramName"/> is already an Auto*-parameter name the unmodified name is returned.</remarks>
        public static string GetAutoParameterName(string paramName)
        {
            if (IsAutoParameterName(paramName))
            {
                return paramName;
            }
            return ParamDesc.AutoPrefix + paramName;
        }

        /// <summary>
        /// Get the base parameter name for the ParamDesc.
        /// </summary>
        /// <returns></returns>
        /// <remarks>If the ParamDesc is not an Auto*-parameter then its own name is returned.</remarks>
        public string GetBaseParameterName()
        {
            return ParamDesc.GetBaseParameterName(this.Name);
        }
        /// <summary>
        /// Get the base parameter name for a given Auto*-parameter name.
        /// </summary>
        /// <param name="paramName">The parameter name.</param>
        /// <returns></returns>
        /// <remarks>If <paramref name="paramName"/> is not an Auto*-parameter name the unmodified name is returned.</remarks>
        public static string GetBaseParameterName(string paramName)
        {
            if (!IsAutoParameterName(paramName))
            {
                return paramName;
            }
            return paramName.Substring(ParamDesc.AutoPrefix.Length);
        }
        /// <summary>
        /// Provides a human-readable representation of the parameter descriptor.
        /// </summary>
        /// <returns>String representation of the parameter descriptor.</returns>
        public override string ToString()
        {
            object value = (null == this.Value)
                ? "n/a"
                : this.Value;
            List<char> flagsList = new List<char>();
            if (this.SupportsAutoMode)
            {
                flagsList.Add('A');
            }
            string flags = flagsList.Count == 0
                ? "-"
                : string.Join(",", flagsList);


            return String.Format("{0,-9} = {1}\t{2,-7}\t[{3}]",
                this.Name,
                value,
                this.Type.Name,
                flags
                );
        }
        /// <summary>
        /// Provides a human-readable representation of a list of parameter descriptors.
        /// </summary>
        /// <returns>String representation of the parameter descriptors.</returns>
        public static string ToString(List<ParamDesc> list)
        {
            /* TODO:
             * - sort alphabetically, or by group
             * - make tabular layout (find longest string in each column, etc.)
             */
            StringBuilder sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.AppendLine(item.ToString());
            }
            return sb.ToString();
        }
        #endregion

        #region Factory Methods to Build Specific Param Descs in C++/CLI
        /// <summary>
        /// Create an int range parameter descriptor.
        /// </summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <remarks>This is a work-around for a compiler bug in VS2012 C++/CLI.</remarks>
        /// <returns>Created range parameter descriptor.</returns>
        public static ParamDesc<int> BuildRangeParamDesc(int min, int max)
        {
            return new RangeParamDesc<int>(min, max);
        }

        /// <summary>
        /// Get the min and max values of an int range parameter descriptor.
        /// </summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <remarks>This is a work-around for a compiler bug in VS2012 C++/CLI.</remarks>
        public static void GetRangeParamDescMinMax(ParamDesc<int> paramDesc, out int min, out int max)
        {
            if (!(paramDesc is RangeParamDesc<int>))
            {
                throw new ArgumentException("Argument paramDesc has the wrong type. Must be RangeParamDesc<int>.");
            }

            RangeParamDesc<int> rpd = (RangeParamDesc<int>)paramDesc;
            min = rpd.Min;
            max = rpd.Max;
        }

        /// <summary>
        /// Create a uint range parameter descriptor.
        /// </summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <remarks>This is a work-around for a compiler bug in VS2012 C++/CLI.</remarks>
        /// <returns>Created range parameter descriptor.</returns>
        public static ParamDesc<uint> BuildRangeParamDesc(uint min, uint max)
        {
            return new RangeParamDesc<uint>(min, max);
        }

        /// <summary>
        /// Get the min and max values of a uint range parameter descriptor.
        /// </summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <remarks>This is a work-around for a compiler bug in VS2012 C++/CLI.</remarks>
        public static void GetRangeParamDescMinMax(ParamDesc<uint> paramDesc, out uint min, out uint max)
        {
            if (!(paramDesc is RangeParamDesc<uint>))
            {
                throw new ArgumentException("Argument paramDesc has the wrong type. Must be RangeParamDesc<uint>.");
            }

            RangeParamDesc<uint> rpd = (RangeParamDesc<uint>)paramDesc;
            min = rpd.Min;
            max = rpd.Max;
        }

        /// <summary>
        /// Create a float range parameter descriptor.
        /// </summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <remarks>This is a work-around for a compiler bug in VS2012 C++/CLI.</remarks>
        /// <returns>Created range parameter descriptor.</returns>
        public static ParamDesc<float> BuildRangeParamDesc(float min, float max)
        {
            return new RangeParamDesc<float>(min, max);
        }

        /// <summary>
        /// Get the min and max values of a float range parameter descriptor.
        /// </summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <remarks>This is a work-around for a compiler bug in VS2012 C++/CLI.</remarks>
        public static void GetRangeParamDescMinMax(ParamDesc<float> paramDesc, out float min, out float max)
        {
            if (!(paramDesc is RangeParamDesc<float>))
            {
                throw new ArgumentException("Argument paramDesc has the wrong type. Must be RangeParamDesc<float>.");
            }

            RangeParamDesc<float> rpd = (RangeParamDesc<float>)paramDesc;
            min = rpd.Min;
            max = rpd.Max;
        }

        /// <summary>
        /// Create an int list parameter descriptor.
        /// </summary>
        /// <param name="allowedValues">List of allowed values.</param>
        /// <param name="format">Formatting of the numbers.</param>
        /// <remarks>This is a work-around for a compiler bug in VS2012 C++/CLI.</remarks>
        /// <returns>Created list parameter descriptor.</returns>
        public static ParamDesc<int> BuildListParamDesc(List<int> allowedValues, string format = null)
        {
            return new ListParamDesc<int>(allowedValues, format);
        }

        /// <summary>
        /// Create a float list parameter descriptor.
        /// </summary>
        /// <param name="allowedValues">List of allowed values.</param>
        /// <param name="format">Formatting of the numbers.</param>
        /// <remarks>This is a work-around for a compiler bug in VS2012 C++/CLI.</remarks>
        /// <returns>Created list parameter descriptor.</returns>
        public static ParamDesc<float> BuildListParamDesc(List<float> allowedValues, string format = null)
        {
            return new ListParamDesc<float>(allowedValues, format);
        }

        /// <summary>
        /// Create a list parameter descriptor from an enum.
        /// </summary>
        /// <param name="enumType">Type parameter.</param>
        /// <remarks>This is a work-around for a compiler bug in VS2012 C++/CLI.</remarks>
        /// <returns>Created list parameter descriptor.</returns>
        public static ParamDesc<string> BuildListParamDesc(Type enumType)
        {
            return new ListParamDesc<string>(enumType);
        }

        /// <summary>
        /// Create a list parameter descriptor from a list of strings.
        /// </summary>
        /// <param name="allowedValues">List of allowed values.</param>
        /// <remarks>This is a work-around for a compiler bug in VS2012 C++/CLI.</remarks>
        /// <returns>Created list parameter descriptor.</returns>
        public static ParamDesc<string> BuildListParamDesc(List<string> allowedValues)
        {
            return new ListParamDesc<string>(allowedValues);
        }
        #endregion
    }
    /// <summary>
    /// Identifies a parameter descriptor which can validate values.
    /// </summary>
    public interface IParamDescWithValidator
    {
        /// <summary>
        /// Validation method for parameter values.
        /// </summary>
        /// <param name="value">Parameter value to be tested.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is valid, <c>false</c> otherwise.</returns>
        bool IsValid(object value);
    }

    /// <summary>
    /// Identifies a parameter descriptor which has a list of allowed values.
    /// </summary>
    /// <seealso cref="ListParamDesc&lt;T&gt;"/>
    /// <seealso cref="IRangeParamDesc&lt;T&gt;"/>
    public interface IListParamDesc : IParamDescWithValidator
    {
        /// <summary>List of allowed values for this parameter.</summary>
        List<string> AllowedValues { get; /*internal set;*/ }
        /// <summary>
        /// Validation method for string parameters.
        /// </summary>
        /// <param name="value">Parameter value to be tested.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is in AllowedValues, <c>false</c> otherwise.</returns>
        bool IsValid(string value);
    }

    /// <summary>
    /// Identifies a parameter descriptor which has a range of allowed values.
    /// </summary>
    /// <remarks>This is just a marker interface used for polymorphism.</remarks>
    /// <seealso cref="RangeParamDesc&lt;T&gt;"/>
    /// <seealso cref="IListParamDesc"/>
    public interface IRangeParamDesc : IParamDescWithValidator { }

    /// <summary>
    /// Identifies a generic parameter descriptor which has an inclusive range of allowed values.
    /// </summary>
    /// <typeparam name="T">The type of the parameter's value.</typeparam>
    /// <seealso cref="RangeParamDesc&lt;T&gt;"/>
    /// <seealso cref="IListParamDesc"/>
    public interface IRangeParamDesc<T> : IRangeParamDesc
    {
        /// <summary>Inclusive minimum of the valid range.</summary>
        T Min { get; /*set;*/ }
        /// <summary>Inclusive maximum of the valid range.</summary>
        T Max { get; /*set;*/ }
        /// <summary>
        /// Validation method for parameter values.
        /// </summary>
        /// <param name="value">Parameter value to be tested.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is valid, <c>false</c> otherwise.</returns>
        bool IsValid(T value);
    }

    /// <summary>
    /// Implementation of a basic generic parameter descriptor.
    /// </summary>
    /// <typeparam name="T">The type of the parameter's value.</typeparam>
    /// <remarks>The only difference to <see cref="ParamDesc"/> is that Value is typed.</remarks>
    /// <seealso cref="ParamDesc"/>
    /// <seealso cref="ListParamDesc&lt;T&gt;"/>
    /// <seealso cref="RangeParamDesc&lt;T&gt;"/>
    /// <seealso cref="GetParameter"/>
    /// <seealso cref="GetParameters"/>
    /// <seealso cref="SetParameter"/>
    /// <seealso cref="SetParameters"/>
    public class ParamDesc<T> : ParamDesc
    {
        #region Constructors
        /// <summary>
        /// Default c'tor.
        /// 
        /// Only calls base c'tor.
        /// </summary>
        public ParamDesc()
            : base()
        { }
        /// <summary>
        /// Copy c'tor. Copies all properties from <paramref name="other"/> and sets type according to own type parameter.
        /// </summary>
        /// <param name="other">The <see cref="ParamDesc"/> object to be copied.</param>
        public ParamDesc(ParamDesc other)
            : base(other)
        {
            this.Type = typeof(T);
        }
        #endregion

        #region Public Properties
        /// <summary>Value of the parameter (typed).</summary>
        /// <remarks>May be null if the parameter is not readable.</remarks>
        public new T Value
        {
            get
            {
                var tmp = base.Value;
                return (null == tmp)
                    ? default(T)
                    : (T)tmp;
            }
            set { base.Value = value; }
        }
        #endregion
    }

    /// <summary>
    /// A generic parameter descriptor which has a list of allowed values.
    /// 
    /// Implementation of IListParamDesc.
    /// </summary>
    /// <typeparam name="T">The type of the parameter's value.</typeparam>
    /// <seealso cref="IListParamDesc"/>
    /// <seealso cref="ParamDesc"/>
    /// <seealso cref="ParamDesc&lt;T&gt;"/>
    /// <seealso cref="RangeParamDesc&lt;T&gt;"/>
    /// <seealso cref="GetParameter"/>
    /// <seealso cref="GetParameters"/>
    /// <seealso cref="SetParameter"/>
    /// <seealso cref="SetParameters"/>
    public class ListParamDesc<T> : ParamDesc<T>, IListParamDesc
    {
        #region IListParamDesc interface
        /// <summary>List of allowed values for this parameter.</summary>
        public List<string> AllowedValues { get; internal set; }
        /// <summary>
        /// Validation method for string parameters.
        /// </summary>
        /// <param name="value">Parameter value to be tested.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is in AllowedValues, <c>false</c> otherwise.</returns>
        public bool IsValid(string value)
        {
            if (null == AllowedValues || AllowedValues.Count == 0)
            {
                return false;
            }
            return AllowedValues.Contains(value);
        }
        #endregion

        #region Constructors
        /// <summary>Default constructor.</summary>
        /// <remarks>If using C++/CLI and VS2012 or lower, use the proper variant of ParamDesc.BuildListParamDesc to construct the desired object.</remarks>
        public ListParamDesc() : base() { }

        /// <summary>
        /// Constructor from Enum type.
        /// 
        /// This constructor gets <c>AllowedValues</c> from the passed System.Enum type.
        /// Use this for restricted parameters (i.e. with underlying enums and the like).
        /// </summary>
        /// <param name="enumType">Type parameter.</param>
        /// <exception cref="ArgumentException">If <paramref name="enumType"/> is not an Enum.</exception>
        /// <remarks>If using C++/CLI and VS2012 or lower, use <see cref="ParamDesc.BuildListParamDesc(Type)"/> to construct the desired object.</remarks>
        public ListParamDesc(Type enumType)
            : base()
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException();
            }

            this.AllowedValues = new List<string>(Enum.GetNames(enumType));
        }

        /// <summary>
        /// Constructor from list of values.
        /// 
        /// This constructor gets <c>AllowedValues</c> as a parameter.
        /// </summary>
        /// <param name="allowedValues">List of allowed values.</param>
        /// <remarks>If using C++/CLI and VS2012 or lower, use <see cref="ParamDesc.BuildListParamDesc(List&lt;string&gt;)"/> to construct the desired object.</remarks>
        public ListParamDesc(List<string> allowedValues)
            : base()
        {
            this.AllowedValues = new List<string>(allowedValues);
        }

        /// <summary>
        /// Constructor from list of values.
        /// 
        /// This constructor gets <c>AllowedValues</c> as a parameter.
        /// </summary>
        /// <param name="allowedValues">List of allowed values.</param>
        /// <param name="format">Formatting of the float numbers.</param>
        /// <remarks>If using C++/CLI and VS2012 or lower, use <see cref="ParamDesc.BuildListParamDesc(List&lt;float&gt;, string)"/> to construct the desired object.</remarks>
        public ListParamDesc(List<float> allowedValues, string format = null)
            : base()
        {
            this.AllowedValues = new List<string>(allowedValues.Count);
            if (null == format)
            {
                foreach (var item in allowedValues)
                {
                    AllowedValues.Add(item.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            else
            {
                foreach (var item in allowedValues)
                {
                    AllowedValues.Add(item.ToString(format, System.Globalization.CultureInfo.InvariantCulture));
                }
            }
        }

        /// <summary>
        /// Constructor from list of values.
        /// 
        /// This constructor gets <c>AllowedValues</c> as a parameter.
        /// </summary>
        /// <param name="allowedValues">List of allowed values.</param>
        /// <param name="format">Formatting of the float numbers.</param>
        /// <remarks>If using C++/CLI and VS2012 or lower, use <see cref="ParamDesc.BuildListParamDesc(List&lt;int&gt;, string)"/> to construct the desired object.</remarks>
        public ListParamDesc(List<int> allowedValues, string format = null)
            : base()
        {
            this.AllowedValues = new List<string>(allowedValues.Count);
            if (null == format)
            {
                foreach (var item in allowedValues)
                {
                    AllowedValues.Add(item.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            else
            {
                foreach (var item in allowedValues)
                {
                    AllowedValues.Add(item.ToString(format, System.Globalization.CultureInfo.InvariantCulture));
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Validation method for parameter values.
        /// </summary>
        /// <param name="value">Parameter value to be tested.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is valid, <c>false</c> otherwise.</returns>
        /// <exception cref="InvalidCastException">The parameter type is not compatible with this ParamDesc.</exception>
        public bool IsValid(object value)
        {
            Type valueType = value.GetType();

            // If parameter and value are enum types, then they also must be of the same enum type
            if (this.Type.IsEnum && valueType.IsEnum)
            {
                return valueType == this.Type;
            }

            string valueAsString = TypeConversion.GetAsGoodString(value);
            T castedValue = default(T);

            bool isTypeConvertible = false;
            if (value is string)
            {
                isTypeConvertible = true; // We assume that strings are always convertible
            }
            else
            {
                try
                {
                    castedValue = (T)Convert.ChangeType(value, this.Type, CultureInfo.InvariantCulture);
                    isTypeConvertible = (null != castedValue);
                }
                catch (ArgumentNullException)
                { /* empty */ }
                catch (FormatException)
                { /* empty */ }
                catch (InvalidCastException)
                { /* empty */ }
                catch (OverflowException)
                { /* empty */ }
            }

            if (!isTypeConvertible)
            {
                throw new InvalidCastException("Cast failed and returned null.");
            }

            // Compare casted value against all allowed values
            valueAsString = valueAsString.ToLower();
            foreach (var item in AllowedValues)
            {
                if (value is string)
                {
                    if (item.ToLower() == valueAsString)
                    {
                        return true;
                    }
                }
                else
                {
                    T castedItem = (T)Convert.ChangeType(item, this.Type, CultureInfo.InvariantCulture);
                    if (castedItem.Equals(castedValue))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        /// <summary>
        /// Provides a human-readable representation of the parameter descriptor.
        /// </summary>
        /// <returns>String representation of the parameter descriptor.</returns>
        public override string ToString()
        {
            string allowedValuesAsString;

            if (AllowedValues == null || AllowedValues.Count < 1)
            {
                allowedValuesAsString = "N/A";
            }
            else
            {
                allowedValuesAsString = string.Join(",", AllowedValues);
            }

            return base.ToString() + "\t{" + allowedValuesAsString + "}";
        }
        #endregion
    }

    /// <summary>
    /// A generic parameter descriptor which has a range of allowed values.
    /// 
    /// Implementation of <see cref="IRangeParamDesc&lt;T&gt;"/>.
    /// </summary>
    /// <typeparam name="T">The type of the parameter's value. Must have a default Comparer.</typeparam>
    /// <seealso cref="IRangeParamDesc&lt;T&gt;"/>
    /// <seealso cref="ParamDesc"/>
    /// <seealso cref="ParamDesc&lt;T&gt;"/>
    /// <seealso cref="ListParamDesc&lt;T&gt;"/>
    /// <seealso cref="GetParameter"/>
    /// <seealso cref="GetParameters"/>
    /// <seealso cref="SetParameter"/>
    /// <seealso cref="SetParameters"/>
    public class RangeParamDesc<T> : ParamDesc<T>, IRangeParamDesc<T>
    {
        #region IRangeParamDesc interface
        /// <summary>Inclusive minimum of the valid range.</summary>
        public T Min { get; internal set; }
        /// <summary>Inclusive maximum of the valid range.</summary>
        public T Max { get; internal set; }
        /// <summary>
        /// Validation method for parameter values.
        /// </summary>
        /// <param name="value">Parameter value to be tested.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is valid, <c>false</c> otherwise.</returns>
        public bool IsValid(T value)
        {
            T castVal = (T)value;
            Comparer<T> comp = Comparer<T>.Default;

            if (comp.Compare(castVal, Min) < 0  // x < y
            ||  comp.Compare(castVal, Max) > 0) // x > y
                return false;

            return true;
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor with min and max parameters.
        /// </summary>
        /// <remarks>If using C++/CLI and VS2012 or lower, use ParamDesc.BuildRangeParamDesc of the proper type to construct the desired object.</remarks>
        public RangeParamDesc(T min, T max)
            : base()
        {
            this.Min = min;
            this.Max = max;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Validation method for parameter values.
        /// </summary>
        /// <param name="value">Parameter value to be tested.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is valid, <c>false</c> otherwise.</returns>
        public bool IsValid(object value)
        {
            return IsValid((T)value);
        }
        /// <summary>
        /// Provides a human-readable representation of the parameter descriptor.
        /// </summary>
        /// <returns>String representation of the parameter descriptor.</returns>
        public override string ToString()
        {
            string suffix = "";
            bool isValid = true;
            if (Min == null && Max == null)
            {
                isValid = false;
            }
            if (Min.Equals(Max))
            {
                isValid = false;
            }

            suffix = isValid
                ? Min + "-" + Max
                : "N/A";

            return base.ToString() + "\t[" + suffix + "]"; ;
        }
        #endregion
    }

    /// <summary>
    /// A parameter descriptor for the specification of multiple filenames.
    /// </summary>     
    /// <seealso cref="ParamDesc"/>
    /// <seealso cref="IParamDescWithValidator"/>
    /// <seealso cref="GetParameter"/>
    /// <seealso cref="GetParameters"/>
    /// <seealso cref="SetParameter"/>
    /// <seealso cref="SetParameters"/>
    public class MultiFileParamDesc : ParamDesc, IParamDescWithValidator
    {
        #region Constructors
        /// <summary>Default constructor.</summary>
        public MultiFileParamDesc() : base() { }
        #endregion

        #region Public Methods
        /// <summary>
        /// Validation method for parameter values.
        /// </summary>
        /// <param name="value">Parameter value to be tested.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is valid, <c>false</c> otherwise.</returns>
        public bool IsValid(object value)
        {
            if (!(value is List<string>))
            {
                return false;
            }

            foreach (string path in (List<String>)value)
            {
                if (!File.Exists(path))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
    }
}
