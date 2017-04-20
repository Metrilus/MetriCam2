using System;

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
}