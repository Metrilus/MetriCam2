using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("MetriCam 2: RealSense wrapper")]
[assembly: AssemblyDescription("MetriCam 2 wrapper for RealSense-based cameras")]
[assembly: MetriCam2.Attributes.ContainsCameraImplementations]
[assembly: MetriCam2.Attributes.NativeDependencies("libpxcclr.cs.dll", "libpxccpp2c.dll")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("6c5fc85f-a613-45cc-9fa1-31f5a2373c0e")]
