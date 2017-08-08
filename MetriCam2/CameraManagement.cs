// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2.Exceptions;
using Metrilus.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace MetriCam2
{
    public class CameraManagement
    {
        #region Types
        public class SelectedCamerasChangedArgs : EventArgs
        {
            public Camera Camera { get; private set; }
            public bool Deselected { get; private set; }

            public SelectedCamerasChangedArgs(Camera camera, bool deselected)
            {
                this.Camera = camera;
                this.Deselected = deselected;
            }
        }
        public delegate void SelectedCamerasChangedHandler(object sender, SelectedCamerasChangedArgs args);
        public event SelectedCamerasChangedHandler SelectedCamerasChanged;
        #endregion

        #region Private Fields
        private const string MetriCam2_Camera_Namespace = "MetriCam2.Cameras.";

        private static Dictionary<string, Type> loadedCameraTypes = new Dictionary<string, Type>();
        private static Dictionary<string, string> loadedCameraTypesDLLPaths = new Dictionary<string, string>();
        private static List<string> inspectedCameraDllNames = new List<string>();
        private static MetriLog log;
        private static object instanceLock = new object();
        private static CameraManagement instance = null;
        /// <summary>
        /// A list of all connectable cameras.
        /// </summary>
        private static List<Camera> connectableCameras;
        #endregion

        #region Public Properties
        /// <summary>
        /// LoggerName of this class in log4net messages.
        /// </summary>
        public static string LoggerName { get { return "MetriCam2.CameraManagement"; } }
        /// <summary>
        /// Determines behaviour of <see cref="GetInstance()"/>.
        /// </summary>
        public static bool ScanForCameraDLLs { get; set; }
        /// <summary>
        /// A list of currently selected cameras.
        /// </summary>
        public List<Camera> SelectedCameras { get; private set; }
        /// <summary>
        /// Instances of all available camera classes.
        /// </summary>
        public List<Camera> AvailableCameras { get; private set; }
        /// <summary>
        /// Names of all available camera types.
        /// </summary>
        public List<string> AvailableCameraTypeNames { get { return new List<string>(loadedCameraTypes.Keys); } }
        #endregion

        #region Constructors
        /// <summary>
        /// Private Constructor. Use GetInstance().
        /// </summary>
        /// <param name="scanForCameraDLLs">Passed through to <see cref="InitializeCameras(bool)"/>.</param>
        private CameraManagement(bool scanForCameraDLLs)
        {
            this.SelectedCameras = new List<Camera>();
            this.AvailableCameras = new List<Camera>();
            InitializeCameras(scanForCameraDLLs);
        }

        /// <summary>
        /// Static constructor.
        /// Registers all camera types from the currently loaded assemblies.
        /// </summary>
        static CameraManagement()
        {
            log = new MetriLog(LoggerName);
            ScanLoadedAssemblies();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Calls <see cref="GetInstance(bool)"/> using the property <see cref="ScanForCameraDLLs"/> as parameter.
        /// </summary>
        /// <returns>The singleton object.</returns>
        /// <seealso cref="ScanForCameraDLLs"/>
        /// <seealso cref="GetInstance(bool)"/>
        public static CameraManagement GetInstance()
        {
            return GetInstance(ScanForCameraDLLs);
        }
        /// <summary>
        /// Creates an instance of CameraManagement (if required) and returns it.
        /// </summary>
        /// <param name="scanForCameraDLLs">Passed through to <see cref="InitializeCameras(bool)"/>.</param>
        /// <returns>The singleton object.</returns>
        public static CameraManagement GetInstance(bool scanForCameraDLLs)
        {
            lock (instanceLock)
            {
                if (null == instance)
                {
                    instance = new CameraManagement(scanForCameraDLLs);
                }
            }
            return instance;
        }

        /// <summary>
        /// Scans an assembly and adds contained camera implementations known list.
        /// </summary>
        /// <param name="filename">Filename of the assembly (relative or absolute).</param>
        public void AddCamerasFromDLL(string filename)
        {
            log.DebugFormat("AddCamerasFromDLL({0})", filename);
            ScanAssembly(filename);
            InitializeCameras(false);
        }
        public void AddCameraByName(string cameraTypeName)
        {
            log.DebugFormat("AddCameraByName({0})", cameraTypeName);
            AvailableCameras.Add(GetCameraInstanceByName(cameraTypeName));
        }

        public void SelectCamera(Camera cam)
        {
            if (null == cam)
            {
                log.Warn("SelectCamera(null)");
                return;
            }
            log.DebugFormat("SelectCamera({0})", cam.Name);

            Camera myCam = null;
            string dummy;
            try
            {
                myCam = CameraManagement.GetCameraInstanceByName(cam.Name, out dummy);
            }
            catch (ArgumentException)
            {
                string cameraTypeName = cam.GetType().ToString();
                log.DebugFormat("    camera with name {0} not found, trying {1}", cam.Name, cameraTypeName);
                try
                {
                    myCam = CameraManagement.GetCameraInstanceByName(cameraTypeName, out dummy);
                }
                catch (ArgumentException)
                {
                    log.WarnFormat("    camera with names {0} or {1} not found", cam.Name, cameraTypeName);
                    throw;
                }
            }

            this.SelectedCameras.Add(myCam);
            if (SelectedCamerasChanged != null)
                SelectedCamerasChanged(this, new SelectedCamerasChangedArgs(myCam, false));
        }

        public void DeselectCamera(Camera cam)
        {
            if (null == cam)
            {
                log.Warn("DeselectCamera(null)");
                return;
            }
            log.DebugFormat("DeselectCamera({0})", cam.Name);
            this.SelectedCameras.Remove(cam);
            if (SelectedCamerasChanged != null)
                SelectedCamerasChanged(this, new SelectedCamerasChangedArgs(cam, true));
        }

        /// <summary>
        /// Instantiates a camera object of a given type (identified by its type name).
        /// </summary>
        /// <param name="name">The type name (the output of typeof([CameraClassName]).ToString()).</param>
        /// <returns>A new instance of the camera class.</returns>
        public static Camera GetCameraInstanceByName(string name)
        {
            string dummy;
            return GetCameraInstanceByName(name, out dummy);
        }

        /// <summary>
        /// Instantiates a camera object of a given type (identified by its type name).
        /// </summary>
        /// <param name="name">The type name (the output of typeof([CameraClassName]).ToString()).</param>
        /// <param name="dllPath">Returns the path of the DLL which contains the camera type.</param>
        /// <returns>A new instance of the camera class.</returns>
        public static Camera GetCameraInstanceByName(string name, out string dllPath)
        {
            log.DebugFormat("GetCameraInstanceByName({0}, out)", name);
            Type cameraType;
            try
            {
                cameraType = loadedCameraTypes[name];
                dllPath = loadedCameraTypesDLLPaths[name];
            }
            catch (KeyNotFoundException)
            {
                string name2 = EnsureMetriCam2CameraPrefix(name);
                log.DebugFormat("    camera with name {0} not found, trying {1}", name, name2);
                try
                {
                    cameraType = loadedCameraTypes[name2];
                    dllPath = loadedCameraTypesDLLPaths[name2];
                }
                catch (KeyNotFoundException)
                {
                    string msg = string.Format("The camera type \"{0}\" is not registered. Please add the correct DLL.", name);
                    log.Warn(msg);
                    throw new ArgumentException(msg);
                }
            }
            ConstructorInfo constructor;
            try
            {
                constructor = cameraType.GetConstructor(new Type[] { });
                if (null == constructor)
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                string msg = string.Format("The default constructor of the camera type \"{0}\" could not be found (probably missing).", name);
                log.Error(msg);
                throw new ArgumentException(msg);
            }

            Camera cam;
            try
            {
                cam = (Camera)constructor.Invoke(null);
            }
            catch (Exception ex)
            {
                string msg = string.Format("The default constructor of the camera type \"{0}\" threw an exception: {1}", name, ex.Message);
                log.Error(msg);
                throw new Exception(msg);
            }

            log.DebugFormat("    camera {0} found", cam.Name);
            return cam;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="Exception">If the file <paramref name="filename"/> is not a MetriCam2 camera DLL.</exception>
        /// <exception cref="NativeDependencyMissingException">If the assembly could not be loaded because of missing a dependency.</exception>
        /// <exception cref="InvalidOperationException">
        /// If the file <paramref name="filename"/> could not be loaded, or 
        /// if a file with the same name has been inspected before, or
        /// if no implementations of <see cref="MetriCam2.Camera"/> were found in the assembly.
        /// </exception>
        /// <param name="filename"></param>
        public static void ScanAssembly(string filename)
        {
            log.DebugFormat("ScanAssembly({0})", filename);
            if (!File.Exists(filename))
            {
                log.ErrorFormat("File '{0}' does not exist.", filename);
                return;
            }

            Assembly assembly = null;

            try
            {
                // Try to load the assembly from the given filename
                assembly = Assembly.LoadFile(filename);
            }
            catch (Exception ex)
            {
                log.DebugFormat("    Loading '{0}' failed: {1}. Finding out the reason for failure...", filename, ex.Message);
                DetermineReasonForLoadFailure(filename); // If a reason is found an appropriate Exception is thrown
            }

            if (null == assembly)
            {
                // assembly is only set at the call to .LoadFile. If we end up here we were not able to determine the reason for failure.
                string msg = string.Format("Assembly could not be loaded (for an unknown reason).");
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }

            if (inspectedCameraDllNames.Contains(assembly.GetName().ToString()))
            {
                string msg = "An assembly with the same name has already been inspected.";
                log.Warn(msg);
                throw new InvalidOperationException(msg);
            }

            if (!InspectAssembly(assembly, filename))
            {
                string msg = string.Format("No implementation of MetriCam2.Camera was found in {0}.", filename);
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }
        }

        /// <summary>
        /// Find all camera implementations in the currently loaded assemblies (DLLs).
        /// </summary>
        public static void ScanLoadedAssemblies()
        {
            log.Debug("CameraManagement: Scanning all loaded assemblies");
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                InspectAssembly(assembly, "memory");
            }
        }

        /// <summary>
        /// Creates instances of all currently loaded camera implementations.
        /// Optionally finds all camera implementations in local and SDK folder first.
        /// </summary>
        /// <param name="scanForCameraDLLs">If enabled, all assemblies (.DLL files) in the local folder and the MetriCam2 SDK path are scanned.</param>
        /// <remarks>The MetriCam2 SDK path is read from the registry.</remarks>
        public void InitializeCameras(bool scanForCameraDLLs)
        {
            log.DebugFormat("InitializeCameras({0})", scanForCameraDLLs);
            if (scanForCameraDLLs)
            {
                log.Debug("Scanning for camera DLLs");
                LoadLocalDirectoryAssemblies();
                LoadRegistryDirectoryAssemblies();
            }

            AvailableCameras.Clear();
            foreach (string cameraTypeName in AvailableCameraTypeNames)
            {
                try
                {
                    Camera cam = GetCameraInstanceByName(cameraTypeName);
                    AvailableCameras.Add(cam);
                }
                catch (Exception ex)
                {
                    log.InfoFormat("Camera with TypeName '{0}' was not added due to an exception: {1}", cameraTypeName, ex.Message);
                }
            }
        }

        /// <summary>
        /// Disable access to all camera types not listed in <paramref name="allowedCameraTypes"/>.
        /// </summary>
        /// <param name="allowedCameraTypes"></param>
        public void RestrictAvailableCameras(string[] allowedCameraTypes)
        {
            if (log.IsDebugEnabled)
            {
                log.DebugFormat("RestrictAvailableCameras with types: {0}", string.Join(", ", allowedCameraTypes));
            }

            // sanitize camera type names
            for (int i = 0; i < allowedCameraTypes.Length; i++)
            {
                allowedCameraTypes[i] = EnsureMetriCam2CameraPrefix(allowedCameraTypes[i]);
            }

            List<Camera> camerasToDeactivate = new List<Camera>();
            foreach (Camera cam in AvailableCameras)
            {
                string camName = EnsureMetriCam2CameraPrefix(cam.Name);

                if (!allowedCameraTypes.Contains(camName))
                {
                    camerasToDeactivate.Add(cam);
                }
            }
            if (log.IsDebugEnabled)
            {
                log.DebugFormat("    These cameras will be deactivated: {0}", string.Join(", ", camerasToDeactivate));
            }

            foreach (var item in camerasToDeactivate)
            {
                // Deselect cameras which are no longer available
                DeselectCamera(item);
                AvailableCameras.Remove(item);
            }
        }

        /// <summary>
        /// Gets all connectable cameras.
        /// </summary>
        /// <param name="rescan">true: force a rescan.</param>
        /// <returns>A list of connectable cameras.</returns>
        public static List<Camera> GetConnectableCameras(bool rescan = false)
        {
            if (rescan || connectableCameras == null)
            {
                ScanForConnectableCameras();
            }
            return connectableCameras;
        }

        /// <summary>
        /// Scans for connectable cameras.
        /// </summary>
        /// <returns>true.</returns>
        /// <remarks>The return value is necessary as this method can be called in a splash screen and has to be able to report its success.</remarks>
        public static bool ScanForConnectableCameras()
        {
            log.Info("Scanning for available camera assemblies...");

            // force new initialization with ScanForCameraDlls true
            GetInstance().InitializeCameras(true);

            List<Camera> cameras = GetInstance().AvailableCameras;

            log.Info(cameras.Count + " cameras available:");
            log.Info(String.Join(" / ", cameras.Select(f => f.Name).ToArray()));
            log.Info("Detecting connectable cameras...");

            connectableCameras = new List<Camera>();

            foreach (var item in cameras)
            {
                try
                {
                    item.Connect();
                    log.Info(item.Name + " available for connection.");
                    item.Disconnect();
                    connectableCameras.Add(item);
                }
                catch
                {
                    /* empty */
                }
            }
            return true;
        }
        #endregion

        #region Private Methods
        private static void DetermineReasonForLoadFailure(string filename)
        {
            try
            {
                // Load the assembly from the given filename, but without dependencies
                Assembly assemblyRefOnly = Assembly.ReflectionOnlyLoadFrom(filename);

                // Early termination based on processor architecture mismatch
                ProcessorArchitecture architecture = assemblyRefOnly.GetName().ProcessorArchitecture;
                if (!IsProcessorArchitectureCompatible(architecture, filename))
                {
                    return;
                }

                // Try to load each referenced assembly
                foreach (var referencedAssemblyName in assemblyRefOnly.GetReferencedAssemblies())
                {
                    try
                    {
                        // Load the referenced assembly, without dependencies
                        Assembly.ReflectionOnlyLoad(referencedAssemblyName.FullName);
                    }
                    catch
                    {
                        string refAssemblyFilename = referencedAssemblyName.Name + ".dll";// This is usually, but not necessarily correct
                        if (referencedAssemblyName.CultureInfo != null && !referencedAssemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture))
                        {
                            refAssemblyFilename = referencedAssemblyName.CultureInfo.ToString() + Path.DirectorySeparatorChar + refAssemblyFilename;
                        }

                        // Try to load referenced assembly from resource
                        if (!TryLoadFromResource(refAssemblyFilename))
                        {
                            TryLoadFromCosturaResource(refAssemblyFilename);
                        }
                    }
                }

                bool isCameraDLL = false;
                List<string> references = new List<string>();
                try
                {
                    foreach (CustomAttributeData data in assemblyRefOnly.GetCustomAttributesData())
                    {
                        string customAttributeName = data.Constructor.DeclaringType.FullName;

                        if (customAttributeName == typeof(MetriCam2.Attributes.ContainsCameraImplementations).FullName)
                        {
                            isCameraDLL = true;
                        }

                        if (customAttributeName == typeof(MetriCam2.Attributes.NativeDependencies).FullName)
                        {
                            foreach (CustomAttributeTypedArgument constructorArgument in data.ConstructorArguments)
                            {
                                if (constructorArgument != null)
                                {
                                    var t = constructorArgument.Value as System.Collections.ObjectModel.ReadOnlyCollection<CustomAttributeTypedArgument>;
                                    if (t != null)
                                    {
                                        foreach (CustomAttributeTypedArgument arg in t)
                                        {
                                            references.Add((string)arg.Value);
                                        }
                                        //check if all references are available
                                    }
                                }
                                break; //we have only one constructor argument
                            }
                        }
                    }
                }
                catch
                {
                    // we don't know if it is a valid camera DLL
                }

                if (!isCameraDLL)
                {
                    string msg = string.Format("{0} is not a valid MetriCam2 camera DLL.", filename);
                    log.Error(msg);
                    throw new Exception(msg);
                }

                List<string> missingDependencies = new List<string>();
                List<string> foundDependencies = new List<string>();
                foreach (string dependency in references)
                {
                    //TODO: Check if camera dependencies are available.
                    //Maybe check also the AppPath or SDK path for the camera DLL
                    IntPtr pDll = LoadLibrary(dependency);
                    if (pDll == IntPtr.Zero)
                    {
                        missingDependencies.Add(dependency);
                    }
                    else
                    {
                        foundDependencies.Add(dependency);
                    }
                }

                if (missingDependencies.Count > 0)
                {
                    StringBuilder infoString = new StringBuilder();
                    infoString.AppendFormat(
                        "We tried to load '{0}'. However, it depends on other DLLs / assemblies of which some could not be found :-(" + Environment.NewLine,
                        filename);

                    if (missingDependencies.Count > 0) //redundant, but kept for readability.
                    {
                        infoString.AppendLine();
                        infoString.AppendLine("The following dependencies could not be found:");
                        foreach (var item in missingDependencies)
                        {
                            infoString.AppendFormat("{0} ({1})" + Environment.NewLine, item, architecture.ToString());
                        }
                    }
                    if (foundDependencies.Count > 0)
                    {
                        infoString.AppendLine();
                        infoString.AppendLine("These dependencies have been found:");
                        foreach (var item in foundDependencies)
                        {
                            infoString.AppendFormat("{0} ({1})" + Environment.NewLine, item, architecture.ToString());
                        }
                    }

                    log.Error(infoString.ToString());
                    throw new NativeDependencyMissingException(infoString.ToString(), missingDependencies.ToArray());
                }
            }
            catch (Exception why)
            {
                string msg = string.Format("Loading assembly failed: {0}", why.Message);
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }
        }

        private static bool TryLoadFromResource(string refAssemblyFilename)
        {
            using (Stream stream = GetManagedEntryAssembly().GetManifestResourceStream(refAssemblyFilename))
            {
                if (stream == null)
                {
                    return false;
                }

                byte[] assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                Assembly.ReflectionOnlyLoad(assemblyRawBytes);
            }

            return true;
        }

        private static bool TryLoadFromCosturaResource(string refAssemblyFilename)
        {
            string refAssemblyFilenameCostura = "costura." + refAssemblyFilename.ToLower() + ".zip";
            using (Stream stream = GetManagedEntryAssembly().GetManifestResourceStream(refAssemblyFilenameCostura))
            {
                if (stream == null)
                {
                    return false;
                }

                //TODO: Costura fody zips the files, so unzipping is required
                System.IO.Compression.DeflateStream unzipper = new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionMode.Decompress);
                MemoryStream decompressed = new MemoryStream();
                unzipper.CopyTo(decompressed);
                Assembly.ReflectionOnlyLoad(decompressed.ToArray());
            }

            return true;
        }

        private static bool IsProcessorArchitectureCompatible(ProcessorArchitecture architecture, string filename)
        {
            switch (architecture)
            {
                case ProcessorArchitecture.Amd64:
                    if (!Environment.Is64BitProcess)
                    {
                        log.WarnFormat("File {0} has ProcessorArchitecture Amd64 while the application is not a 64-bit process.", filename);
                        return false;
                    }
                    break;
                case ProcessorArchitecture.IA64:
                    log.WarnFormat("File {0} has unsupported ProcessorArchitecture IA64.", filename);
                    return false;
                case ProcessorArchitecture.MSIL:
                    // Should work with bot x86 and x64 processes.
                    break;
                case ProcessorArchitecture.None:
                    // Unknown. Try further...
                    break;
                case ProcessorArchitecture.X86:
                    if (Environment.Is64BitProcess)
                    {
                        log.WarnFormat("File {0} has ProcessorArchitecture X86 while the application is a 64-bit process.", filename);
                        return false;
                    }
                    break;
            }

            return true;
        }

        /// <summary>
        /// Make sure that the string starts with <see cref="MetriCam2_Camera_Namespace"/>.
        /// </summary>
        /// <param name="camName"></param>
        /// <returns></returns>
        private static string EnsureMetriCam2CameraPrefix(string camName)
        {
            return MetriCam2_Camera_Namespace + camName.Replace(MetriCam2_Camera_Namespace, "");
        }

        // Used by ScanAssembly and ScanLoadedAssemblies
        private static bool InspectAssembly(Assembly assembly, string dllPath)
        {
            Match m = Regex.Match(assembly.FullName, "^(.*?),");
            string assemblyName = m.Groups[1].Value;

            if (assembly.GetCustomAttributes(typeof(MetriCam2.Attributes.ContainsCameraImplementations), false).Length < 1)
            {
                log.DebugFormat("Skipping {0} from {1}: No MetriCam2 marker attribute [{2}]", assemblyName, dllPath, assembly.FullName);
                return false;
            }

            log.InfoFormat("Inspecting {0} from {1} [{2}]", assemblyName, dllPath, assembly.FullName);

            bool success = false;

            Type[] types = assembly.GetTypes();
            foreach (Type t in types)
            {
                string typeID = t.ToString();

                if (t.BaseType != typeof(Camera) || t.IsAbstract)
                {
                    continue;
                }

                try
                {
                    loadedCameraTypes.Add(typeID, t);
                    loadedCameraTypesDLLPaths.Add(typeID, dllPath);
                    log.InfoFormat("  Added camera type {0} from {1}", typeID, dllPath);
                    success = true;
                }
                catch (ArgumentException)
                {
                    // key already exists
                }
            }

            // If a MetriCam camera implementation was found we will not look at any assembly with the same name.
            // Otherwise, we might look at another assembly with the same name in a different folder.
            if (success)
            {
                inspectedCameraDllNames.Add(assembly.GetName().ToString());
            }

            return success;
        }

        private void LoadRegistryDirectoryAssemblies()
        {
            RegistryKey rk = Registry.LocalMachine; // HKLM
            string[] subkeys = new string[] { "SOFTWARE", "Metrilus GmbH" };

            try
            {
                foreach (var subkey in subkeys)
                {
                    rk = rk.OpenSubKey(subkey, false);
                }
            }
            catch
            { /* empty */ }

            if (null == rk)
            {
                log.WarnFormat(@"Registry key HKLM\{0}\ not found", string.Join(@"\", subkeys));
                return;
            }

            try
            {
                string sdkPath = (String)rk.GetValue("MetriCam2 SDK");
                if (sdkPath == null)
                    return;

                string[] dllList = Directory.GetFiles((String)sdkPath, "MetriCam2.Cameras.*.dll");
                TryLoadAssemblies(dllList, "registry");
            }
            catch
            { /* empty */ }
        }

        private void LoadLocalDirectoryAssemblies()
        {
            string[] dllList = Directory.GetFiles(Directory.GetCurrentDirectory(), "MetriCam2.Cameras.*.dll");
            TryLoadAssemblies(dllList, "local");
        }

        private void TryLoadAssemblies(string[] dllList, string location)
        {
            foreach (string dll in dllList)
            {
                // The list may contain files ending with e.g. ".dll_" (see Directory.GetFiles docs)
                if (Path.GetExtension(dll) != ".dll")
                {
                    continue;
                }

                TryLoadAssembly(dll);
            }
        }

        private void TryLoadAssembly(string dll)
        {
            if (inspectedCameraDllNames.Contains(Path.GetFileName(dll)))
            {
                return;
            }

            try
            {
                ScanAssembly(dll);
            }
            catch
            {
                /* ignore DLLs which could not be loaded */
            }
            inspectedCameraDllNames.Add(Path.GetFileName(dll));
        }

        /// <summary>
        /// Returns the managed entry assembly, even if the application is transformed to a native exe.
        /// </summary>
        /// <returns>The managed entry assembly.</returns>
        private static Assembly GetManagedEntryAssembly()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly != null)
            {
                return entryAssembly;
            }

            //if there is a native entry point
            StackTrace stackTrace = new StackTrace();
            StackFrame[] frames = stackTrace.GetFrames();
            for (int entryIdx = frames.Length - 1; entryIdx >= 0; entryIdx--)
            {
                Assembly assembly = frames[entryIdx].GetMethod().Module.Assembly;
                string name = assembly.GetName().Name;
                if (name != "mscorlib" && name != "_")
                {
                    entryAssembly = assembly;
                    break;
                }
            }
            return entryAssembly;
        }

        /// <summary>
        /// Loads a native (unmanaged) library.
        /// </summary>
        /// <param name="dllToLoad"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);
        #endregion
    }
}
