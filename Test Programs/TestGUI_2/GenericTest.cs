using MetriCam2;
using Metrilus.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace TestGUI_2
{
    /// <summary>
    /// This class can be used to test some general behaviors of MetriCam2 cameras.
    /// </summary>
    class GenericTest
    {
        #region Types
        /// <summary>
        /// Function that should return a new Camera object.
        /// Use this if you have a special constructor for your camera.
        /// </summary>
        /// <returns>new object of camera type</returns>
        public delegate Camera GetNewCameraDelegateType();
        #endregion

        #region Private Variables
        private string[] channels;
        private string serialNumber;
        private List<string> output;
        private bool useLogFile;
        private string logFile;
        private Type camType;
        private GetNewCameraDelegateType getNewCameraDelegate = null;
        // see below (utilities)
        private delegate bool BoolDelegate();
        #endregion

        #region Properties
        /// <summary>
        /// If useLogFile was set, this property contains the file name of the written logfile.
        /// </summary>
        /// <remarks>Call DoTests() and PrintOutput() first.</remarks>
        public string LogFile
        {
            get { return logFile; }
        }
        /// <summary>
        /// Gets the output in form of a List<string>.
        /// </summary>
        /// <remarks>Call DoTests() first.</remarks>
        public List<string> Output
        {
            get { return output; }
        }
        /// <summary>
        /// This allows to specify a function pointer to get a new Object of a camera.
        /// Use this if you *not* want to use the default constructor for creating objects of cameras.
        /// </summary>
        /// <remarks>Set this before calling DoTests().</remarks>
        public GetNewCameraDelegateType GetNewCameraDelegate
        {
            get { return getNewCameraDelegate; }
            set { getNewCameraDelegate = value; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Simple constructor -> use the other one.
        /// </summary>
        /// <param name="type">type of camera to test</param>
        /// <param name="useLog">write output into logfile?</param>
        public GenericTest(Type type, bool useLog = true)
        {
            camType      = type;
            channels     = null;
            serialNumber = null;
            output       = new List<string>();
            useLogFile   = useLog;

            if (!type.BaseType.Equals(typeof(Camera)))
            {
                throw new ArgumentException("The given type should be a MetriCam2 Camera!");
            }
        }
        /// <summary>
        /// Initializes the tests.
        /// </summary>
        /// <param name="type">type of camera to test</param>
        /// <param name="channels">channels to use</param>
        /// <param name="serialNumber">serial number of camera</param>
        /// <param name="useLog">write output into logfile?</param>
        public GenericTest(Type type, string serialNumber, bool useLog = true)
        {
            camType           = type;
            this.serialNumber = serialNumber;
            output            = new List<string>();
            useLogFile        = useLog;

            if (!type.BaseType.Equals(typeof(Camera)))
            {
                throw new ArgumentException("The given type should be a MetriCam2 Camera!");
            }
        }
        #endregion

        #region Testing Methods
        /// <summary>
        /// Do the generic tests. Output will be written to STDOUT or a temporary logfile (see constructor).
        /// Use LogFile property to get the log file path if used.
        /// 
        /// Example:
        ///   GenericTest test = new GenericTest(typeof(PrimeSense), "foobar", true);
        ///   test.DoTests();
        ///   test.PrintOutput();
        ///   string logfile = test.LogFile;
        ///   // read the logfile
        ///   // or use Output property to get the output as string list
        /// </summary>
        public void DoTests()
        {
            uint tests = 0, errors = 0;

            // rotate output
            output = null;
            output = new List<string>();

            AddOutput("==================================================");
            AddOutput("Starting Tests for {0} now.", camType.ToString()   );
            AddOutput("==================================================");

            TEST("Testing simple connect.", ref tests, ref errors, () => TestConnect());

            TEST("Testing connect while connected.", ref tests, ref errors, () => TestConnectWhileConnected());

            TEST("Testing Reconnect.", ref tests, ref errors, () => TestReconnect());

            TEST("Testing connect by correct serial number.", ref tests, ref errors, () => TestConnectByCorrectSerialNumber());

            TEST("Testing connect by incorrect serial number.", ref tests, ref errors, () => TestConnectByInCorrectSerialNumber());

            TEST("Testing updating of serial number.", ref tests, ref errors, () => TestUpdateSerialNumber());

            TEST("Testing Update().", ref tests, ref errors, () => TestSingleUpdate());

            TEST("Testing CalcChannel().", ref tests, ref errors, () => TestCalcChannels());

            TEST("Testing getting/setting random properties.", ref tests, ref errors, () => TestPropertiesRandom());

            AddOutput("==================================================");
            AddOutput("All tests done: {0}/{1} were successful.", tests - errors, tests);
            AddOutput("==================================================");
        }
        /// <summary>
        /// Simple connect test.
        /// </summary>
        /// <returns>true on success, else false</returns>
        private bool TestConnect()
        {
            Camera cam = GetNewCamera();
            bool res = false;

            try
            {
                cam.Connect();
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(MetriCam2.Exceptions.ConnectionFailedException))
                {
                    AddError("Connect failed and the Exception type is not ConnectionFailedException. The type is: " + ex.GetType().ToString() +
                             ". The connect failed, because: " + ex.Message);
                }
                else
                {
                    AddError("Connect failed: " + ex.Message);
                }
                return false;
            }

            if (!cam.IsConnected)
            {
                AddError("Connect were successful, but the IsConnected property was not updated.");
                goto err;
            }

            try
            {
                cam.Disconnect();
            }
            catch (Exception ex)
            {
                AddError("Disconnect failed: " + ex.Message);
                return false;
            }

            if (cam.IsConnected)
            {
                AddError("Disconnect were successful, but the IsConnected property was not updated.");
                return false;
            }

            return true;
err:
            try { cam.Disconnect(); } catch { AddError("Disconnect failed."); res = false; }
            return res;
        }
        /// <summary>
        /// Connect while connected should fail.
        /// </summary>
        /// <returns>true on success, else false</returns>
        private bool TestConnectWhileConnected()
        {
            Camera cam = GetNewCamera();

            try { cam.Connect(); } catch { AddError("First connect failed."); return false; }

            try
            {
                cam.Connect();
                AddError("Double Connect was successful.");
                try { cam.Disconnect(); } catch { AddError("Disconnect failed."); return false; }
                return false;
            }
            catch
            {
                // exception occurred -> good
            }

            try { cam.Disconnect(); } catch { AddError("Disconnect failed."); return false; }

            return true;
        }
        /// <summary>
        /// Simple reconnect test.
        /// </summary>
        /// <returns>true on success, else false</returns>
        private bool TestReconnect()
        {
            Camera cam = GetNewCamera();

            try
            {
                cam.Connect();
                cam.Disconnect();
                cam.Connect();
                cam.Disconnect();
            }
            catch (Exception ex)
            {
                AddError("Reconnect test failed: " + ex.Message);
                return false;
            }

            return true;
        }
        /// <summary>
        /// Connects and verifies, that the serial number will be updated by Connect().
        /// </summary>
        /// <returns>true on success, else false</returns>
        private bool TestUpdateSerialNumber()
        {
            Camera cam = GetNewCamera();

            try { cam.Connect(); } catch { AddError("Connect failed."); return false; }

            string tmp = cam.SerialNumber;

            if (tmp == null || tmp.Equals(""))
            {
                AddError("Connect does not update the serial number.");
                try { cam.Disconnect(); } catch { AddError("Disconnect failed."); return false; }
                return false;
            }

            try { cam.Disconnect(); } catch { AddError("Disconnect failed."); return false; }
            return true;
        }
        /// <summary>
        /// Connects by correct serial number.
        /// </summary>
        /// <returns>true on success, else false</returns>
        private bool TestConnectByCorrectSerialNumber()
        {
            Camera cam = GetNewCamera();

            if (serialNumber == null)
            {
                AddError("No Serial Number given!");
                return false;
            }

            cam.SerialNumber = serialNumber;
            try
            {
                cam.Connect(); 
                if (!cam.SerialNumber.Equals(serialNumber))
                {
                    AddError("Camera has wrong serial number.");
                    try { cam.Disconnect(); } catch { AddError("Disconnect failed."); return false; }
                    return false;
                }
            }
            catch (Exception ex)
            {
                AddError("Connect by serial number failed: " + ex.Message);
                return false;
            }

            try { cam.Disconnect(); } catch { AddError("Disconnect failed."); return false; }
            return true;
        }
        /// <summary>
        /// Connects by incorrect serial number and hopefully it does not work.
        /// </summary>
        /// <returns>true on success, else false</returns>
        private bool TestConnectByInCorrectSerialNumber()
        {
            Camera cam = GetNewCamera();

            cam.SerialNumber = "0xdeadbeef";
            try
            {
                cam.Connect();
                try { cam.Disconnect(); } catch { AddError("Disconnect failed."); return false; }
                AddError("Connect by wrong serial number was successful.");
                return false;
            }
            catch
            {
                return true;
            }
        }
        /// <summary>
        /// Calls Update() and tries to get the data.
        /// </summary>
        /// <returns>true on success, else false</returns>
        private bool TestSingleUpdate()
        {
            Camera cam = GetNewCamera();
            bool res = false;

            try { cam.Connect(); } catch { AddError("Connect failed."); return false; }

            int frame = cam.FrameNumber;
            long time = cam.TimeStamp;

            try
            {
                cam.Update();
            }
            catch (Exception ex)
            {
                AddError("Update failed: " + ex.Message);
                goto err;
            }

            // verify frame and timestemp
            if (frame != cam.FrameNumber - 1)
            {
                AddError("Frame number was not updated in Update().");
                goto err;
            }
            if (time == cam.TimeStamp)
            {
                AddError("Time stamp was not updated in Update().");
                goto err;
            }

            res = true;
err:
            try { cam.Disconnect(); } catch { AddError("Disconnect failed."); res = false; }
            return res;
        }
        /// <summary>
        /// Selects every channel and tries to get an image.
        /// </summary>
        /// <returns>true on success, else false</returns>
        private bool TestCalcChannels()
        {
            Camera cam = GetNewCamera();
            channels = GetChannel(cam);
            bool res = false;

            if (channels == null || channels.Length == 0)
            {
                AddError("Camera of type \"" + camType.ToString() + "\" has no channel.");
                return false;
            }

            try { cam.Connect(); } catch { AddError("Connect failed."); return false; }

            /*
             * Disable all active channel first. This is needed, 'cause some cameras
             * cannot have some channel enabled at the same time.
             * So only one channel will be active at the same time.
             */
            foreach (ChannelRegistry.ChannelDescriptor activeChannel in cam.ActiveChannels.ToArray())
            {
                try
                {
                    cam.DeactivateChannel(activeChannel.Name);
                }
                catch (Exception ex)
                {
                    AddError("Deactivate channel \"" + activeChannel.Name + "\" failed: " + ex.Message);
                    goto err;
                }
            }

            // iterate through channels
            foreach (string channel in channels)
            {
                CameraImage img = null;
                try
                {
                    if (!cam.IsChannelActive(channel))
                    {
                        cam.ActivateChannel(channel);
                    }
                }
                catch (Exception ex)
                {
                    AddError("Activate channel \"" + channel + "\" failed: " + ex.Message);
                    goto err;
                }
                cam.SelectChannel(channel);
                try
                {
                    cam.Update();
                }
                catch (Exception ex)
                {
                    AddError("Update failed: " + ex.Message);
                    goto err;
                }
                try
                {
                    img = cam.CalcSelectedChannel();
                }
                catch (Exception ex)
                {
                    AddError("CalcChannel failed: " + ex.Message);
                    goto err;
                }
                if (img == null)
                {
                    AddError("Resulting image for channel \"" + channel + "\" is NULL.");
                    goto err;
                }
                if (img.FrameNumber == -1)
                {
                    AddError("Framenumber was not updated.");
                    goto err;
                }
                if (img.TimeStamp == 0)
                {
                    AddError("Timestamp was not updated.");
                    goto err;
                }
                try
                {
                    cam.DeactivateChannel(channel);
                }
                catch (Exception ex)
                {
                    AddError("Deactivate channel \"" + channel + "\" failed: " + ex.Message);
                    goto err;
                }
            }

            res = true;
err:
            try { cam.Disconnect(); } catch { AddError("Disconnect failed."); res = false; }
            return res;
        }
        /// <summary>
        /// Gets and sets some properties using random values.
        /// </summary>
        /// <returns>true on success, else false</returns>
        private bool TestPropertiesRandom()
        {
            Camera cam               = GetNewCamera();
            PropertyInfo[] propInfos = camType.GetProperties();
            Random rnd               = new Random(DateTime.Now.Millisecond);
            bool res                 = true;

            try { cam.Connect(); } catch { AddError("Connect failed."); return false; }

            foreach (PropertyInfo i in propInfos)
            {
                // skip MetriCam2 properties
                if (i.Name == "Name"          || i.Name == "SerialNumber" || 
                    i.Name == "CameraIcon"    || i.Name == "FrameNumber"  ||
                    i.Name == "TimeStamp"     || i.Name == "IsConnected"  ||
                    i.Name == "UpdateTimeout" || i.Name == "NumChannels")
                {
                    continue;
                }

                // read/write available?
                if (!i.CanRead && !i.CanWrite)
                {
                    continue;
                }

                // only get test
                if (i.CanRead && !i.CanWrite)
                {
                    try
                    {
                        i.GetValue(cam, null);
                    }
                    catch (Exception ex)
                    {
                        AddError("Getting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }
                    continue;
                }

                // only set test
                if (!i.CanRead && i.CanWrite)
                {
                    try
                    {
                        i.SetValue(cam, i.PropertyType == typeof(bool) ? (Object)Convert.ToBoolean(rnd.Next()) : (Object)rnd.Next(), null);
                    }
                    catch (Exception ex)
                    {
                        AddError("Setting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }
                    continue;
                }

                // get -> set -> get
                if (i.PropertyType == typeof(double) || i.PropertyType == typeof(float))
                {
                    double oldValue = 0, value;
                    value = (double)rnd.NextDouble();

                    // first get
                    try
                    {
                        oldValue = (double)i.GetValue(cam, null);
                    }
                    catch (Exception ex)
                    {
                        AddError("Getting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }

                    // set
                    try
                    {
                        i.SetValue(cam, value, null);
                    }
                    catch (Exception ex)
                    {
                        AddError("Setting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }

                    // last get
                    try
                    {
                        value = (double)i.GetValue(cam, null);
                        if (Math.Abs(value - oldValue) > Double.Epsilon)
                        {
                            throw new Exception("Setting property \"" + i.Name + "\" failed: Property did not return previously set value.");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddError("Getting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }
                }
                else if (i.PropertyType == typeof(int) || i.PropertyType == typeof(short) || i.PropertyType == typeof(long))
                {
                    long oldValue = 0, value;
                    value = (long)rnd.Next(-1000, 1000);

                    // first get
                    try
                    {
                        oldValue = (long)i.GetValue(cam, null);
                    }
                    catch (Exception ex)
                    {
                        AddError("Getting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }

                    // set
                    try
                    {
                        i.SetValue(cam, value, null);
                    }
                    catch (Exception ex)
                    {
                        AddError("Setting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }

                    // last get
                    try
                    {
                        value = (long)i.GetValue(cam, null);
                        if (value != oldValue)
                        {
                            throw new Exception("Setting property \"" + i.Name + "\" failed: Property did not return previously set value.");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddError("Getting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }
                }
                else if (i.PropertyType == typeof(uint) || i.PropertyType == typeof(ushort) || i.PropertyType == typeof(ulong))
                {
                    ulong oldValue = 0, value;
                    value = (ulong)rnd.Next(0, 1000);

                    // first get
                    try
                    {
                        oldValue = (ulong)i.GetValue(cam, null);
                    }
                    catch (Exception ex)
                    {
                        AddError("Getting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }

                    // set
                    try
                    {
                        i.SetValue(cam, value, null);
                    }
                    catch (Exception ex)
                    {
                        AddError("Setting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }

                    // last get
                    try
                    {
                        value = (ulong)i.GetValue(cam, null);
                        if (value != oldValue)
                        {
                            throw new Exception("Setting property \"" + i.Name + "\" failed: Property did not return previously set value.");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddError("Getting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }
                }
                else if (i.PropertyType == typeof(bool))
                {
                    bool oldValue = true, value = true;

                    // first get
                    try
                    {
                        oldValue = (bool)i.GetValue(cam, null);
                    }
                    catch (Exception ex)
                    {
                        AddError("Getting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }

                    // set
                    try
                    {
                        i.SetValue(cam, value, null);
                    }
                    catch (Exception ex)
                    {
                        AddError("Setting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }

                    // last get
                    try
                    {
                        value = (bool)i.GetValue(cam, null);
                        if (value != oldValue)
                        {
                            throw new Exception("Setting property \"" + i.Name + "\" failed: Property did not return previously set value.");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddError("Getting property \"" + i.Name + "\" failed: " + ex.Message);
                        res = false;
                    }
                }
            }

            try { cam.Disconnect(); } catch { AddError("Disconnect failed."); return false; }

            return res;
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Adds an output message.
        /// </summary>
        /// <param name="msg">message to add, can be a windows format string</param>
        /// <param name="vargs">parameters for the format string, is not necessary</param>
        private void AddOutput(String msg, params Object[] vargs)
        {
            string line = string.Format(msg + "\r\n", vargs); // windows line endings... -> use dos2unix for viewing it on linux machines
            output.Add(line);
        }
        /// <summary>
        /// Adds an error message to the output.
        /// </summary>
        /// <param name="msg">error message</param>
        private void AddError(String msg)
        {
            StackFrame callStack = new StackFrame(1, true);
            string method        = callStack.GetMethod().Name;
            int line             = callStack.GetFileLineNumber();
            string error         = string.Format("[ERROR {0}:{1}]: {2}\r\n", method, line, msg);

            output.Add(error);
        }
        /// <summary>
        /// Creates a object for the given camera type.
        /// </summary>
        /// <returns>object of camType</returns>
        private Camera GetNewCamera()
        {
            Camera cam = null;

            if (getNewCameraDelegate != null)
            {
                cam = getNewCameraDelegate();
            }
            else
            {
                ConstructorInfo si = camType.GetConstructor(new Type[] { });
                cam = (Camera)si.Invoke(null);
            }

            if (cam == null)
            {
                throw new InvalidOperationException("Cannot create an instance of the given camera type \"" + camType.Name + "\".");
            }

            return cam;
        }
        /// <summary>
        /// This methods gets all available channels for a camera.
        /// </summary>
        /// <param name="cam">Object of camera</param>
        /// <returns>Array of available channels</returns>
        private string[] GetChannel(Camera cam)
        {
            string[] res = new string[cam.Channels.Count];
            uint i = 0;

            foreach (ChannelRegistry.ChannelDescriptor d in cam.Channels)
            {
                res[i++] = d.Name;
            }

            return res;
        }
        /*
         * Unfortunately C# has so support for macros :-(.
         * 
         * Useful macros would be like:
         * #define TRY(expr) do { try { expr } catch { AddError(#expr " failed."); return false; } } while (0)
         * #define TEST(name, desc) do { AddOutput(#desc); res = Test ## name(); if (!res) { ++errors; } } while (0)
         * 
         * However it can use something like function pointers, so i ended up using this methods for
         * making testing a little bit easier. Of course it's not good as the macros above, but may save some time.
         */
        /// <summary>
        /// Executes a test, increases errors if failed.
        /// </summary>
        /// <param name="descr">description of the test</param>
        /// <param name="tests">amount of tests already done -> will be incremented</param>
        /// <param name="errors">amount of errors so far -> will be incremented if test fails</param>
        /// <param name="b">test function to execute</param>
        private void TEST(string descr, ref uint tests, ref uint errors, BoolDelegate b)
        {
            bool res;

            ++tests;
            AddOutput(descr);
            res = b();
            if (!res)
            {
                ++errors;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Prints the output or writes the output in a temporary log file.
        /// </summary>
        /// <remarks>Use LogFile property to get the name of the created log file.</remarks>
        public void PrintOutput()
        {
            if (output == null)
            {
                return;
            }

            if (useLogFile)
            {
                string temp = Path.GetTempFileName();
                logFile = temp;
                StreamWriter file = new StreamWriter(temp, true);
                foreach (string line in output)
                {
                    file.Write(line);
                }
                file.Close();
            }
            else
            {
                logFile = "stdout";
                foreach (string line in output)
                {
                    System.Console.Write(line);
                }
            }
        }
        #endregion
    }
}
