﻿// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2.Exceptions;
using Metrilus.Logging;
using Metrilus.Util;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using MetriCam2.Attributes;
using MetriCam2.Enums;

namespace MetriCam2
{
    /// <summary>
    /// Camera base class.
    /// </summary>
    /// <remarks>
    /// <list>
    /// <item>Implements basic camera properties and methods.</item>
    /// <item>Provides convenience around camera methods.</item>
    /// <item>Raises connection / disconnection events.</item>
    /// </list>
    /// </remarks>
    public abstract class Camera
    {
        #region Constants
        /// <summary>
        /// Speed of light in air
        /// </summary>
        public const float SpeedOfLight = 299705518.0f;
        #endregion

        #region Types
        /// <summary>Event handler delegate for connection and disconnection events.</summary>
        /// <param name="sender">Camera object which raised the event.</param>
        /// <seealso cref="OnConnecting"/>
        /// <seealso cref="OnConnected"/>
        /// <seealso cref="OnDisconnecting"/>
        /// <seealso cref="OnDisconnected"/>
        public delegate void ConnectionHandler(Camera sender);
        #endregion

        #region Events
        /// <summary>Raised before a camera will be connected.</summary>
        public event ConnectionHandler OnConnecting;
        /// <summary>Raised after a camera was connected.</summary>
        public event ConnectionHandler OnConnected;
        /// <summary>Raised before a camera will be disconnected.</summary>
        public event ConnectionHandler OnDisconnecting;
        /// <summary>Raised after a camera was disconnected.</summary>
        public event ConnectionHandler OnDisconnected;
        #endregion

        #region Private Fields
        /// <summary>
        /// Cache for intrinsic calibrations.
        /// </summary>
        private Dictionary<string, IProjectiveTransformation> intrinsicsCache = new Dictionary<string, IProjectiveTransformation>();
        /// <summary>
        /// Cache for extrinsic calibrations.
        /// </summary>
        private Dictionary<string, RigidBodyTransformation> extrinsicsCache = new Dictionary<string, RigidBodyTransformation>();

        private int id;
        private static int lastId = -1;
        #endregion

        #region Protected Fields
        /// <summary>
        /// Lock for this camera instance.
        /// </summary>
        protected object cameraLock = new object();
        /// <summary>
        /// Enhanced automatic thread-safety (at the cost of a bit of performance).
        /// </summary>
        /// <remarks>
        /// If enabled, <see cref="cameraLock"/> is entered by the base class before <see cref="CalcChannelImpl"/> is called, and before <see cref="SetParameter"/> does actually change anything.
        /// This also affects <see cref="SetParameters"/> and <see cref="TrySetParameter"/>.
        /// Enabling <see cref="enableImplicitThreadSafety"/> provides thread-safety between those methods and those listed hereafter.
        /// It does not affect <see cref="Connect"/>, <see cref="Disconnect"/>, <see cref="Update"/>, <see cref="ActivateChannel"/>, or <see cref="DeactivateChannel"/> which are always called within <see cref="cameraLock"/>.
        /// </remarks>
        protected bool enableImplicitThreadSafety = false;
        /// <summary>
        /// Currently selected channel.
        /// </summary>
        protected string selectedChannel = "";
        /// <summary>
        /// Serial number of the camera.
        /// </summary>
        protected string serialNumber = "";
        /// <summary>
        /// The logger.
        /// </summary>
        protected static MetriLog log = new MetriLog("MetriCam2.Camera");
        /// <summary>
        /// The camera's model name.
        /// </summary>
        /// <remarks>Default is empty string. The actual model will usually be known after <see cref="Connect"/>.</remarks>
        protected string modelName = "";
        /// <summary>
        /// Flag which remembers if <see cref="Update"/> was ever called.
        /// </summary>
        /// <remarks>This flag is checked in <see cref="CalcChannel"/> to catch this common error in end user software.</remarks>
        private bool hasUpdateBeenCalled = false;
        private static string calibrationPathRegistry = null;
        #endregion

        #region Public Properties
        /// <summary>
        /// Timeout for every call to the camera driver's Update, in ms.
        /// </summary>
        /// <remarks>
        /// Used to avoid blocking of the camera driver to freeze the application.
        /// Not supported by all cameras.
        /// </remarks>
        public static int UpdateTimeout { get; set; }
        /// <summary>
        /// Name of the selected channel.
        /// </summary>
        public string SelectedChannel { get { return selectedChannel; } }
        /// <summary>
        /// List of available channels (supported by this camera instance).
        /// </summary>
        /// <remarks>
        /// If an implementing class supports multiple camera models, this list contains the union of all supported channels before the first camera was connected.
        /// After the first connect this list contains the channels supported by that camera.
        /// </remarks>
        public List<ChannelRegistry.ChannelDescriptor> Channels { get; protected set; }
        /// <summary>List of active channels.</summary>
        /// <remarks>
        /// Only active channels are fetched during an <see cref="Update"/>.
        /// Use <see cref="ActivateChannel"/> and <see cref="DeactivateChannel"/> to alter this list.
        /// </remarks>
        public List<ChannelRegistry.ChannelDescriptor> ActiveChannels { get; protected set; }

        /// <summary>
        /// Number of supported channels.
        /// </summary>
        /// <seealso cref="Channels"/>
        public int NumChannels
        {
            get { return Channels.Count; }
        }

        /// <summary>Camera name.</summary>
        /// <remarks>By default this is Vendor Model</remarks>
        public virtual string Name
        {
            get { return (Vendor + " " + Model).Trim(); }
        }

        /// <summary>Name of camera vendor.</summary>
        public virtual string Vendor
        {
            get { return GetType().Name; }
        }

        /// <summary>Name of camera model.</summary>
        public virtual string Model
        {
            get { return modelName; }
        }

        /// <summary>Serial number of the camera.</summary>
        /// <remarks>
        /// If SerialNumber was set while disconnected, <see cref="Connect"/> should try to connect to that camera.
        /// Setting SerialNumber while connected should be ignored or result in an InvalidOperationException.
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the SerialNumber is set while connected.</exception>
        public virtual string SerialNumber
        {
            get { return serialNumber; }
            set
            {
                if (IsConnected)
                {
                    ExceptionBuilder.Throw(typeof(InvalidOperationException), this, "error_changeParameterConnected", "Serial Number");
                }
                serialNumber = value;
            }
        }

        /// <summary>Is the camera currently connected?.</summary>
        public virtual bool IsConnected { get; protected set; }

        /// <summary>Number of the current frame.</summary>
        /// <remarks>Starts at 0 at connect and is incremented with each call to <see cref="Update"/>.</remarks>
        public int FrameNumber { get; protected set; }

        /// <summary>TimeStamp of the current frame.</summary>
        /// <remarks>
        /// By default the TimeStamp is set to DateTime.UtcNow.Ticks at the beginning of <see cref="Update"/>.
        /// Individual cameras may override this behaviour and e.g. use a timestamp delivered by the camera itself.
        /// </remarks>
        public long TimeStamp { get; protected set; }

#if !NETSTANDARD2_0
        /// <summary>
        /// Provides an icon that represents the camera.
        /// </summary>
        public virtual System.Drawing.Icon CameraIcon { get { return Properties.Resources.DefaultIcon; } }
#endif
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Camera()
            : this(modelName: "")
        { /* empty */ }

        /// <summary>
        /// Constructor with ModelName parameter.
        /// </summary>
        /// <param name="modelName">Model of the Camera implementation.</param>
        /// <remarks>If a Camera implementation supports multiple camera models, use the default constructor and set the <see cref="modelName"/> field in the <see cref="ConnectImpl"/>.</remarks>
        public Camera(string modelName)
        {
            lock (cameraLock)
            {
                id = ++lastId;
            }

            Channels = new List<ChannelRegistry.ChannelDescriptor>();
            ActiveChannels = new List<ChannelRegistry.ChannelDescriptor>();

            LoadAllAvailableChannels();

            SerialNumber = "";
            this.modelName = modelName;

            IsConnected = false;
            FrameNumber = 0;

            UpdateTimeout = 1000;

            log.DebugFormat("Camera of type {0} created (id = {1})", Name, id);
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        /// <remarks>Disconnects the camera.</remarks>
        ~Camera()
        {
            Disconnect(false);

            log.DebugFormat("Camera of type {0} destroyed (id = {1})", Name, id);
        }
        #endregion

        #region Connect and Disconnect
        /// <summary>
        /// Enumerates all currently available (i.e. attached to the computer) devices.
        /// </summary>
        /// <param name="use_static_method">Dummy parameter to indicate unwanted usage.</param>
        /// <remarks>Implementing classes should implement a static version of this method.</remarks>
        /// <exception cref="NotImplementedException">Use the static version of this method.</exception>
        public List<Camera> EnumerateDevices(bool use_static_method)
        {
            throw new NotImplementedException("Use the static version of this method.");
        }

        /// <summary>
        /// Connects the camera.
        /// </summary>
        /// <remarks>
        /// The camera-specific <see cref="ConnectImpl"/> is always called inside the camera lock.
        /// Raises <see cref="OnConnecting"/> and <see cref="OnConnected"/> events.
        /// Activates channels
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the instance is already connected.</exception>
        public void Connect()
        {
            log.DebugFormat("Connecting camera {0}.", Name);

            if (this.IsConnected)
            {
                ExceptionBuilder.Throw(typeof(InvalidOperationException), this, "error_connectionFailed");
            }

            this.FrameNumber = -1;
            this.TimeStamp = -1;

            if (OnConnecting != null)
            {
                OnConnecting(this);
            }

            lock (cameraLock)
            {
                ConnectImpl();
            }

            this.IsConnected = true;

            if (ActiveChannels.Count > 0)
            {
                if (log.IsDebugEnabled)
                {
                    List<string> tmpChannels = new List<string>();
                    foreach (var item in ActiveChannels)
                    {
                        tmpChannels.Add(item.Name);
                    }
                    log.DebugFormat("Activating channels '{0}'.", string.Join("'; '", tmpChannels));
                }
                foreach (ChannelRegistry.ChannelDescriptor c in ActiveChannels)
                {
                    ActivateChannelImpl(c.Name);
                }

                // Select first active channel
                if (string.IsNullOrWhiteSpace(SelectedChannel))
                {
                    SelectChannel(ActiveChannels[0].Name);
                }
            }

            if (OnConnected != null)
            {
                OnConnected(this);
            }

            log.DebugFormat("Camera {0} connected.", Name);
        }

        /// <summary>
        /// Disconnects the camera.
        /// </summary>
        /// <param name="useLockedCall">Whether to lock the call to <see cref="DisconnectImpl"/>. Default: <c>true</c>. In Dispose or Destructor, use <c>false</c>.</param>
        /// <remarks>Calls camera-specific <see cref="DisconnectImpl"/> (optionally inside of the camera lock). Raises disconnection events.</remarks>
        /// <seealso cref="DisconnectImpl"/>
        /// <seealso cref="OnDisconnecting"/>
        /// <seealso cref="OnDisconnected"/>
        public void Disconnect(bool useLockedCall = true)
        {
            if (!IsConnected)
            {
                return;
            }

            log.Debug("Disconnecting camera.");

            if (OnDisconnecting != null)
            {
                OnDisconnecting(this);
            }

            this.IsConnected = false;

            if (useLockedCall)
            {
                lock (cameraLock)
                {
                    DisconnectImpl();
                }
            }
            else
            {
                DisconnectImpl();
            }

            if (OnDisconnected != null)
            {
                OnDisconnected(this);
            }

            log.Debug("Camera disconnected.");
        }

        /// <summary>
        /// Updates data buffers of all active channels with data of current frame.
        /// </summary>
        /// <remarks>Calls camera-specific <see cref="UpdateImpl"/> inside of the camera lock.</remarks>
        /// <exception cref="InvalidOperationException">If the instance is not connected to a camera.</exception>
        public void Update()
        {
            if (!IsConnected)
            {
                ExceptionBuilder.Throw(typeof(InvalidOperationException), this, "error_cameraNotConnected");
            }

            lock (cameraLock)
            {
                this.hasUpdateBeenCalled = true;
                this.FrameNumber++;
                this.TimeStamp = DateTime.UtcNow.Ticks;

                UpdateImpl();
            }
        }

        /// <summary>
        /// Activate a channel.
        /// </summary>
        /// <param name="channelName">Channel name.</param>
        /// <remarks>Calls camera-specific <see cref="ActivateChannelImpl"/> inside of the camera lock.</remarks>
        /// <exception cref="ArgumentException">When trying to activate a channel which had not been registered before.</exception>
        public void ActivateChannel(string channelName)
        {
            log.DebugFormat("Trying to activate channel {0}.", channelName);

            // Check is channelName is actually a registered channel
            if (!HasChannel(channelName))
            {
                throw new ArgumentException(string.Format("{0}: Cannot activate '{1}'. It is not a registered channel.", Name, channelName));
            }


            lock (cameraLock)
            {
                if (!IsChannelActive(channelName))
                {
                    ActivateChannelImpl(channelName);
                    AddToActiveChannels(channelName);
                    log.InfoFormat("Activated channel {0}.", channelName);
                }
            }
        }
        /// <summary>
        /// Deactivate a channel to save time in <see cref="Update"/>.
        /// </summary>
        /// <param name="channelName">Channel name.</param>
        /// <remarks>Calls camera-specific <see cref="DeactivateChannelImpl"/> inside of the camera lock.</remarks>
        public void DeactivateChannel(string channelName)
        {
            log.DebugFormat("Trying to deactivate channel {0}.", channelName);

            lock (cameraLock)
            {
                // deselected channel if it will be deactivated.
                if (SelectedChannel == channelName)
                {
                    selectedChannel = "";
                }

                if (IsChannelActive(channelName))
                {
                    DeactivateChannelImpl(channelName);
                    ActiveChannels.Remove(GetChannelDescriptor(channelName));
                    log.InfoFormat("Deactivated channel {0}.", channelName);
                }
            }
        }
        #endregion

        #region Calibration File Loading
        /// <summary>
        /// Loads intrinsic parameters for the Camera.
        /// </summary>
        /// <remarks>This method is cached.</remarks>
        /// <param name="channelName">Channel for which the intrinsics should be loaded.</param>
        /// <returns>An IProjectiveTransformation object.</returns>
        /// <exception cref="InvalidOperationException">When called while the camera is not connected.</exception>
        /// <exception cref="FileNotFoundException">When no calibration file was found for the specified channel.</exception>
        public virtual IProjectiveTransformation GetIntrinsics(string channelName)
        {
            if (!IsConnected)
            {
                ExceptionBuilder.Throw(typeof(InvalidOperationException), this, "error_cameraNotConnected");
                return null;
            }

            if (intrinsicsCache.ContainsKey(channelName) && intrinsicsCache[channelName] != null)
            {
                log.DebugFormat("Found intrinsic calibration for channel {0} in cache.", channelName);
                return intrinsicsCache[channelName];
            }

            bool isDefault;
            IProjectiveTransformation pt = LoadPT(channelName, out isDefault);
            if (null != pt)
            {
                intrinsicsCache[channelName] = pt;
                return pt;
            }
            
            throw new FileNotFoundException(
                String.Format(
                    "{0}: No valid calibration file for channel '{1}' available." + Environment.NewLine
                    + "We have been looking for a file named '{2}' in the working folder, in '{3}', and as embedded resource.",
                    Name,
                    channelName,
                    ChannelIdentifier(channelName) + ".pt",
                    GetCalibrationPathFromRegistry()
                    ));
        }
        /// <summary>
        /// Loads a transformation from one channel to another.
        /// </summary>
        /// <remarks>This method is cached.</remarks>
        /// <param name="channelFromName"></param>
        /// <param name="channelToName"></param>
        /// <returns>A RigidBodyTransformation between the two channels.</returns>
        /// <exception cref="InvalidOperationException">When called while the camera is not connected.</exception>
        /// <exception cref="FileNotFoundException">When no transformation between the two channels was found.</exception>
        public virtual RigidBodyTransformation GetExtrinsics(string channelFromName, string channelToName)
        {
            if (!IsConnected)
            {
                ExceptionBuilder.Throw(typeof(InvalidOperationException), this, "error_cameraNotConnected");
                return null;
            }

            if (channelFromName == channelToName)
            {
                return new RigidBodyTransformation(RotationMatrix.Identity, new Point3f(0, 0, 0));
            }

            RigidBodyTransformation rbt = null;

            string comboFwd = GetRBTName(channelFromName, channelToName);
            string comboInverse = GetRBTName(channelToName, channelFromName);

            if (extrinsicsCache.ContainsKey(comboFwd) && extrinsicsCache[comboFwd] != null)
            {
                rbt = extrinsicsCache[comboFwd];
            }
            else if (extrinsicsCache.ContainsKey(comboInverse) && extrinsicsCache[comboInverse] != null)
            {
                rbt = RigidBodyTransformation.GetInverted(extrinsicsCache[comboInverse]);
            }
            if (null != rbt)
            {
                log.DebugFormat("Found extrinsic calibration for channels {0} and {1} in cache.", channelFromName, channelToName);
                return rbt;
            }

            RigidBodyTransformation rbtInverse = null;

            rbt = LoadRBT(comboFwd);
            if (null != rbt)
            {
                log.DebugFormat("Found forward extrinsics from {0} to {1}.", channelFromName, channelToName);
                rbtInverse = RigidBodyTransformation.GetInverted(rbt);
            }
            else
            {
                rbtInverse = LoadRBT(comboInverse);
                if (null != rbtInverse)
                {
                    log.DebugFormat("Found inverse extrinsics from {0} to {1}.", channelFromName, channelToName);
                    rbt = RigidBodyTransformation.GetInverted(rbtInverse);
                }
            }

            if (null != rbt)
            {
                extrinsicsCache[comboFwd] = rbt;
                extrinsicsCache[comboInverse] = rbtInverse;

                log.DebugFormat("Loaded extrinsic calibration for channels {0} and {1} from file.", channelFromName, channelToName);

                return rbt;
            }

            throw new FileNotFoundException(
                String.Format(
                    "{0}: No valid calibration file between channels '{1}' and '{2}' available." + Environment.NewLine
                    + "We have been looking for files named '{3}' or '{4}' in the working folder, in '{5}', and as embedded resource.",
                    Name,
                    channelFromName, channelToName,
                    comboFwd, comboInverse,
                    GetCalibrationPathFromRegistry()
                    ));
        }
        #endregion

        #region Data Acquisition
        /// <summary>Tests if a channel is currently active.</summary>
        /// <param name="channelName">Channel name.</param>
        public bool IsChannelActive(string channelName)
        {
            ChannelRegistry.ChannelDescriptor cd = null;
            try
            {
                cd = GetChannelDescriptor(channelName);
            }
            catch { /* empty */ }

            if (null == cd)
            {
                // Unsupported channel -> cannot be active
                return false;
            }

            return ActiveChannels.Contains(cd);
        }
        /// <summary>Select a channel which can later be fetched with <see cref="CalcSelectedChannel"/>.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <exception cref="InvalidOperationException">when the channel specified by <paramref name="channelName"/> is currently not active.</exception>
        public void SelectChannel(string channelName)
        {
            if (!IsChannelActive(channelName))
            {
                throw new InvalidOperationException(string.Format("You may not select a channel which is not active (here: '{0}').", channelName));// TODO: use ExceptionBuilder
            }

            log.InfoFormat("Selecting channel '{0}'", channelName);
            selectedChannel = channelName;
        }

        /// <summary>Computes (image) data for the channel selected by <see cref="SelectChannel"/>.</summary>
        /// <returns>(Image) Data.</returns>
        /// <remarks>This is just a shorthand for <see cref="CalcChannel"/>.</remarks>
        public CameraImage CalcSelectedChannel()
        {
            return CalcChannel(selectedChannel);
        }
        /// <summary>
        /// Checks if channel name is valid and calls <see cref="CalcChannelImpl"/>.
        /// </summary>
        /// <param name="channelName">Registered channel name.</param>
        /// <returns>Current frame for this channel.</returns>
        /// <remarks>Calls camera-specific <see cref="CalcChannelImpl"/>.</remarks>
        /// <exception cref="ArgumentException">If the specified channel is not active.</exception>
        /// <seealso cref="enableImplicitThreadSafety"/>
        public CameraImage CalcChannel(string channelName)
        {
            // TODO: Deactivated because of bug: see ticket #0003324.
            //if (calcChannelCache.ContainsKey(channelName))
            //{
            //    return calcChannelCache[channelName];
            //}

            if (!hasUpdateBeenCalled)
            {
                ExceptionBuilder.Throw(typeof(InvalidOperationException), this, "error_updateMustBeCalledBeforeCalcChannel");
                return null;
            }
            CameraImage img;
            if (enableImplicitThreadSafety)
            {
                lock (cameraLock)
                {
                    if (!IsChannelActive(channelName))
                    {
                        //ExceptionBuilder.Throw(typeof(ArgumentException), this, "error_inactiveChannelName", channelName);
                        return null;
                    }

                    img = CalcChannelImpl(channelName);
                }
            }
            else
            {
                if (!IsChannelActive(channelName))
                {
                    ExceptionBuilder.Throw(typeof(ArgumentException), this, "error_inactiveChannelName", channelName);
                    return null;
                }

                img = CalcChannelImpl(channelName);
            }

            if (img != null)
            {
                if (img.FrameNumber < 1)
                {
                    img.FrameNumber = this.FrameNumber;
                }
                if (img.TimeStamp < 1)
                {
                    img.TimeStamp = this.TimeStamp;
                }
                if (string.IsNullOrWhiteSpace(img.ChannelName))
                {
                    img.ChannelName = channelName;
                }
            }

            return img;
        }
        #endregion

        #region Parameter Settings
        #region Public Interface
        /// <summary>
        /// Sets the camera parameter <paramref name="name"/> to <paramref name="value"/>.
        /// </summary>
        /// <param name="name">Name of the camera parameter.</param>
        /// <param name="value">New value of the parameter.</param>
        /// <remarks>
        /// Various checks are performed before the parameter is actually set:
        /// - if the parameter is supported,
        /// - if the parameter is writable,
        /// - if value has the correct type,
        /// - if value is acceptable (i.e. within a defined range or member of an <see cref="Enum"/>).
        /// SetParameter throws exceptions on failure.
        /// </remarks>
        /// <exception cref="ParameterNotSupportedException">If no parameter by that <paramref name="name"/> exists.</exception>
        /// <exception cref="ArgumentException">If <paramref name="value"/> is invalid for the parameter identified by <paramref name="name"/>.</exception>
        /// <exception cref="InvalidOperationException">If parameter <paramref name="name"/> does not exist or is not writable.</exception>
        /// <seealso cref="GetParameter"/>
        /// <seealso cref="GetParameters"/>
        /// <seealso cref="SetParameters"/>
        /// <seealso cref="ParamDesc"/>
        /// <seealso cref="enableImplicitThreadSafety"/>
        public void SetParameter(string name, object value)
        {
            if (enableImplicitThreadSafety)
            {
                lock (cameraLock)
                {
                    SetParameterWithoutLock(name, value);
                }
            }
            else
            {
                SetParameterWithoutLock(name, value);
            }
        }

        /// <summary>
        /// Sets the camera parameter <paramref name="name"/> to <paramref name="value"/>.
        /// </summary>
        /// <param name="name">Name of the camera parameter.</param>
        /// <param name="value">New value of the parameter.</param>
        /// <remarks>
        /// This is a thin wrapper around <see cref="SetParameter"/> which catches a possible exception.
        /// Various checks are performed before the parameter is actually set:
        /// - if the parameter is supported,
        /// - if the parameter is writable,
        /// - if value has the correct type,
        /// - if value is acceptable (i.e. within a defined range or member of an <see cref="Enum"/>).
        /// </remarks>
        /// <seealso cref="SetParameter"/>
        /// <seealso cref="GetParameter"/>
        /// <seealso cref="GetParameters"/>
        /// <seealso cref="SetParameters"/>
        /// <seealso cref="ParamDesc"/>
        public bool TrySetParameter(string name, object value)
        {
            try
            {
                SetParameter(name, value);
                return true;
            }
            catch (ArgumentException)
            { /* empty */ }
            catch (InvalidOperationException)
            { /* empty */ }
            catch (ParameterNotSupportedException)
            { /* empty */ }

            return false;
        }

        /// <summary>
        /// Set many camera parameters at once.
        /// </summary>
        /// <param name="NamesValues">name => value pairs.</param>
        /// <seealso cref="SetParameter"/>
        /// <remarks>
        /// Only basic dependency resolving is performed in a 3-stage model:
        ///   1) Set all Auto* params which are to be enabled.
        ///      Remove them and their underlying params from the Dictionary.
        ///   2) Set all regular params.
        ///      Remove them and their Auto* params from the Dictionary.
        ///   3) Set all Auto* params which are to be disabled.
        ///      Remove them and their underlying params from the Dictionary.
        ///   4) PROFIT !!!
        /// 
        /// If setting any of the parameters fails, the others will still be set.
        /// Errors are counted but not returned.
        /// 
        /// Some cameras, e.g. the uEye, have more complex dependencies between parameters. E.g. pixel clock, frame rate, and exposure.
        /// Should MC2 be able to handle that, or leave it up to the user?
        /// Other ideas for dependency resolution:
        /// - if a parameter is to be set, and its corresponding Auto* parameter is to be disabled, then only the parameter should be set.
        /// - setAfter / dependsOn properties in ParamDesc
        /// - priorities in ParamDesc
        /// </remarks>
        /// <seealso cref="GetParameter"/>
        /// <seealso cref="GetParameters"/>
        /// <seealso cref="SetParameter"/>
        /// <seealso cref="ParamDesc"/>
        public void SetParameters(Dictionary<string, object> NamesValues)
        {
            bool success = true;
            int numErrors = 0;
            int numRemaining = NamesValues.Count;

            // Find all Auto* parameters
            List<string> regularParameters = new List<string>();
            List<string> autoParametersEnabled = new List<string>();
            List<string> autoParametersDisabled = new List<string>();
            foreach (var item in NamesValues)
            {
                string paramName = item.Key;
                if (!ParamDesc.IsAutoParameterName(paramName))
                {
                    regularParameters.Add(paramName);
                    continue;
                }

                bool val = false;

                if (item.Value is bool)
                {
                    val = (bool)item.Value;
                }
                else if (item.Value is string)
                {
                    val = bool.Parse((string)item.Value);
                }
                else
                {
                    throw new ArgumentException("Values of Auto* properties must have bool or string type.");
                }

                if (val == true)
                {
                    autoParametersEnabled.Add(paramName);
                }
                else
                {
                    autoParametersDisabled.Add(paramName);
                }
            }

            // STAGE 1: Set all Auto* parameters which should be enabled
            foreach (var name in autoParametersEnabled)
            {
                try
                {
                    SetParameter(name, NamesValues[name]);
                    NamesValues.Remove(name);
                    numRemaining--;

                    // remove underlying parameter
                    string baseName = ParamDesc.GetBaseParameterName(name);
                    if (regularParameters.Remove(baseName))
                    {
                        log.DebugFormat("Skipping parameter {0} because {1} was enabled.", baseName, name);
                        numRemaining--;
                    }
                }
                catch (Exception)
                {
                    numErrors++;
                }
            }

            // STAGE 2: Set all Auto* parameters which should be disabled
            foreach (var name in autoParametersDisabled)
            {
                try
                {
                    SetParameter(name, NamesValues[name]);
                    numRemaining--;
                }
                catch (Exception)
                {
                    numErrors++;
                }
            }

            // STAGE 2: Set all regular parameters
            foreach (var name in regularParameters)
            {
                try
                {
                    SetParameter(name, NamesValues[name]);
                    numRemaining--;
                }
                catch (Exception ex)
                {
                    log.Error("An error occurred: " + ex.Message);
                    numErrors++;
                }
            }

            success = (0 == numErrors && 0 == numRemaining);

            if (!success)
            {
                log.ErrorFormat("{0} parameter(s) caused errors. {1} parameter(s) were not set at all.", numErrors, numRemaining);
            }
        }

        /// <summary>
        /// Get a parameter descriptor and, if available, the parameter's value.
        /// </summary>
        /// <remarks><see cref="ParamDesc.IsReadable"/> and <see cref="ParamDesc.IsWritable"/> are set according to the current camera state.</remarks>
        /// <param name="name">The parameter's name.</param>
        /// <returns>
        /// If <paramref name="name"/> is a property name to which a corresponding <see cref="ParamDesc"/> exists, then that <see cref="ParamDesc"/> is returned.
        /// If <paramref name="name"/> is a property of the base <see cref="Camera"/> type which has no matching <see cref="ParamDesc"/> then null is returned.
        /// All other cases throw an exception.
        /// </returns>
        /// <exception cref="ParameterNotSupportedException">If no parameter by that name exists.</exception>
        /// <exception cref="ArgumentException">Thrown if a parameter descriptor is publicly visible.</exception>
        /// <exception cref="InvalidOperationException">Thrown if an exception has been thrown in the parameter descriptor's Getter. See InnerException for details.</exception>
        /// <seealso cref="GetParameters"/>
        /// <seealso cref="SetParameter"/>
        /// <seealso cref="SetParameters"/>
        /// <seealso cref="ParamDesc"/>
        public virtual ParamDesc GetParameter(string name)
        {
            log.DebugFormat("GetParameter({0})", name);

            string msgNotSupported = String.Format("{0} does not support parameter {1}.", Name, name);
            PropertyInfo pi = this.GetType().GetProperty(name);
            Type propertyType = pi.PropertyType;
            object propertyValue = pi.GetValue(this, null);

            if (null == pi)
            {
                log.DebugFormat("    {0} (property not found)", msgNotSupported);
                throw new ParameterNotSupportedException(msgNotSupported);
            }

            ParamDesc desc = null;

            Attribute[] attributes = Attribute.GetCustomAttributes(pi);
            if (attributes.IsNullOrEmpty())
                return desc;

            Attributes.DescriptionAttribute description = GetAttribute(attributes, typeof(Attributes.DescriptionAttribute)) as Attributes.DescriptionAttribute;
            if (null == description)
                return desc;

            AccessStateAttribute accessState = GetAttribute(attributes, typeof(AccessStateAttribute)) as AccessStateAttribute;
            UnitAttribute unit = GetAttribute(attributes, typeof(UnitAttribute)) as UnitAttribute;

            if (null != GetAttribute(attributes, typeof(RangeAttribute)))
            {
                RangeAttribute range = GetAttribute(attributes, typeof(RangeAttribute)) as RangeAttribute;
                desc = ParamDesc.CreateRange(
                    propertyType,
                    range.Range,
                    description.Name,
                    description.Description,
                    GetUnit(unit),
                    propertyValue,
                    IsReadable(accessState),
                    IsWritable(accessState));
            }
            else if(null != GetAttribute(attributes, typeof(AllowedValueListAttribute)))
            {

            }
            else
            {
                desc = ParamDesc.Create(
                    propertyType, 
                    description.Name, 
                    description.Description,
                    GetUnit(unit),
                    propertyValue,
                    IsReadable(accessState),
                    IsWritable(accessState));
            }

            return desc;
        }

        /// <summary>
        /// Get a parameter descriptor and, if available, the parameter's value.
        /// </summary>
        /// <remarks>
        /// This is a thin wrapper around <see cref="GetParameter"/> which catches a possible exception and returns null in the error case.
        /// <see cref="ParamDesc.IsReadable"/> and <see cref="ParamDesc.IsWritable"/> are set according to the current camera state.
        /// </remarks>
        /// <param name="name">The parameter's name.</param>
        /// <returns>The <see cref="ParamDesc"/> for the parameter, or null if no parameter by that name exists.</returns>
        /// <seealso cref="GetParameter"/>
        /// <seealso cref="GetParameters"/>
        /// <seealso cref="SetParameter"/>
        /// <seealso cref="SetParameters"/>
        /// <seealso cref="ParamDesc"/>
        public ParamDesc TryGetParameter(string name)
        {
            try
            {
                return GetParameter(name);
            }
            catch (ParameterNotSupportedException)
            {
                return null;
            }
        }

        /// <summary>
        /// Get all parameter descriptors and, if available, the parameters' values.
        /// </summary>
        /// <remarks><see cref="ParamDesc.IsReadable"/> and <see cref="ParamDesc.IsWritable"/> are set according to the current camera state.</remarks>
        /// <returns>A list containing the <see cref="ParamDesc"/>s for all parameters.</returns>
        /// <seealso cref="GetParameter"/>
        /// <seealso cref="SetParameter"/>
        /// <seealso cref="SetParameters"/>
        /// <seealso cref="ParamDesc"/>
        public virtual List<ParamDesc> GetParameters()
        {
            log.Debug("GetParameters");

            List<ParamDesc> res = new List<ParamDesc>();
            Type myType = this.GetType();
            PropertyInfo[] properties = myType.GetProperties();
            foreach (var prop in properties)
            {
                ParamDesc desc = null;
                //ParamDesc desc = GetParameterDescriptor(prop);
                try
                {
                    desc = GetParameter(prop.Name);
                }
                catch (ParameterNotSupportedException)
                {
                    continue;
                }
                if (null != desc)
                {
                    res.Add(desc);
                }
            }

            /* TODO:
             * - sort by desc.Name
             * - group by desc.GroupName
             */

            return res;
        }

        /// <summary>
        /// Loads parameters from a file.
        /// The user has to apply them to the camera using <see cref="SetParameters"/>.
        /// </summary>
        /// <param name="filename">The filename of the parameter file.</param>
        /// <returns>The loaded parameters.</returns>
        public Dictionary<string, object> LoadParameters(string filename)
        {
            // Deserialize
            JsonSerializer serializer = new JsonSerializer();
            serializer.TypeNameHandling = TypeNameHandling.Auto;

            Dictionary<string, ParamDesc> configX;
            using (StreamReader sr = new StreamReader(filename))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                configX = serializer.Deserialize<Dictionary<string, ParamDesc>>(reader);
            }

            Dictionary<string, object> config = new Dictionary<string, object>();
            foreach (var item in configX)
            {
                config[item.Key] = (object)item.Value.Value;
            }

            return config;
        }

        /// <summary>
        /// Get parameters from the camera and stores them in a file.
        /// </summary>
        /// <param name="filename">The filename of the parameter file.</param>
        public void SaveParameters(string filename)
        {
            // Get config from camera
            List<ParamDesc> parameters = GetParameters();
            Dictionary<string, ParamDesc> config = new Dictionary<string, ParamDesc>();
            foreach (var item in parameters)
            {
                config[item.Name] = item;
            }

            // Serialize
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.Formatting = Formatting.Indented;
            serializer.TypeNameHandling = TypeNameHandling.Auto;

            using (StreamWriter sw = new StreamWriter(filename))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, config);
            }
        }
        #endregion

        #region Private Helper Methods
        private static bool CanConvert(Type type, object obj)
        {
            var typeConverter = TypeDescriptor.GetConverter(type);
            if (typeConverter != null && typeConverter.CanConvertFrom(obj.GetType()))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Vaildates whether a parameter
        /// - has the proper type
        /// - has an allowed value (or is within the allowed range)
        /// </summary>
        /// <param name="desc">The <see cref="ParamDesc"/> against which to validate the value.</param>
        /// <param name="value">The value to validate.</param>
        /// <returns>True if <paramref name="value"/> can be converted to the type of <paramref name="desc"/> and fulfills its range/list/etc. criteria. False otherwise.</returns>
        /// <remarks>If <paramref name="value"/>'s type is not equal to the <paramref name="desc"/>'s type, but is convertable, then <paramref name="value"/> will be updated.</remarks>
        private bool ValidateParameterValue(ParamDesc desc, ref object value)
        {
            Type valueType = value.GetType();
            // Check if value has the same type as the parameter
            bool isTypeConvertible = false;
            if (valueType == desc.Type)
            {
                isTypeConvertible = true;
            }
            else
            {
                // Try some special cases
                string valueAsString = TypeConversion.GetAsGoodString(value, valueType);

                if (desc.Type == typeof(byte))
                {
                    byte tmp;
                    isTypeConvertible = byte.TryParse(valueAsString, out tmp);
                    if (isTypeConvertible)
                    {
                        value = tmp;
                    }
                }
                else if (desc.Type == typeof(short))
                {
                    short tmp;
                    isTypeConvertible = short.TryParse(valueAsString, out tmp);
                    if (isTypeConvertible)
                    {
                        value = tmp;
                    }
                }
                else if (desc.Type == typeof(int))
                {
                    int tmp;
                    isTypeConvertible = int.TryParse(valueAsString, out tmp);
                    if (isTypeConvertible)
                    {
                        value = tmp;
                    }
                }
                else if (desc.Type == typeof(uint))
                {
                    uint tmp;
                    isTypeConvertible = uint.TryParse(valueAsString, out tmp);
                    if (isTypeConvertible)
                    {
                        value = tmp;
                    }
                }
                else if (desc.Type == typeof(long))
                {
                    long tmp;
                    isTypeConvertible = long.TryParse(valueAsString, out tmp);
                    if (isTypeConvertible)
                    {
                        value = tmp;
                    }
                }
                else if (desc.Type == typeof(float))
                {
                    float tmp;
                    isTypeConvertible = float.TryParse(valueAsString, out tmp);
                    if (isTypeConvertible)
                    {
                        value = tmp;
                    }
                }
                else if (desc.Type == typeof(double))
                {
                    double tmp;
                    isTypeConvertible = double.TryParse(valueAsString, out tmp);
                    if (isTypeConvertible)
                    {
                        value = tmp;
                    }
                }
                else
                {
                    isTypeConvertible = CanConvert(desc.Type, value);
                }
            }
            if (!isTypeConvertible && value is string)
            {
                if (desc is IListParamDesc)
                {
                    isTypeConvertible = true;
                }
                else if (desc.Type == typeof(Point3f))
                {
                    Point3f p;
                    isTypeConvertible = Point3f.TryParse((string)value, out p);
                    if (!isTypeConvertible)
                    {
                        log.DebugFormat("Cannot parse value {0} for parameter {1} of type {2}", value, desc.Name, desc.Type.Name);
                        return false;
                    }
                    value = p;
                }
                else
                {
                    try
                    {
                        object tmpVal = Convert.ChangeType(value, desc.Type, CultureInfo.InvariantCulture);
                        if (null == tmpVal)
                        {
                            throw new InvalidCastException("Cast failed and returned null.");
                        }
                        value = tmpVal;
                        isTypeConvertible = true;
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
            }
            if (!isTypeConvertible)
            {
                log.DebugFormat("{0}: Value has the wrong type ({1}) for parameter {2}.", this.Name, value.GetType().Name, desc.Name);
                return false;
            }

            IParamDescWithValidator descWithValidator = desc as IParamDescWithValidator;
            if (null != descWithValidator)
            {
                if (!descWithValidator.IsValid(value))
                {
                    //log.Debug("Generic test caught invalid parameter value.");
                    return false;
                }
            }

            return true;
        }

        // If this is an Auto* parameter without an own Descriptor (the usual case), try to get the underlying parameter's descriptor.
        private bool IsAutoParameter(string name)
        {
            ParamDesc baseDesc;
            return IsAutoParameter(name, out baseDesc);
        }
        // If this is an Auto* parameter without an own Descriptor (the usual case), try to get the underlying parameter's descriptor.
        private bool IsAutoParameter(string name, out ParamDesc baseDesc)
        {
            baseDesc = null;
            if (!ParamDesc.IsAutoParameterName(name))
            {
                return false;
            }
            baseDesc = GetParameter(ParamDesc.GetBaseParameterName(name));
            return baseDesc.SupportsAutoMode;
        }

        private void SetAutoParameter(string name, bool value)
        {
            if (!IsAutoParameter(name))
            {
                throw new InvalidOperationException(String.Format("{0}: {1} is not a valid Auto* parameter.", this.Name, name));
            }

            SetPropertyValue(name, value);
        }

        private object GetPropertyValue(string name)
        {
            PropertyInfo pi = this.GetType().GetProperty(name);
            if(pi != null)
            {
                object value = pi.GetValue(this, new object[] { });
                return value;
            }
            return null;
        }
        private void SetPropertyValue(string name, object value)
        {
            PropertyInfo pi = this.GetType().GetProperty(name);
            if(pi != null)
            {
                object myItem = Convert.ChangeType(value, pi.PropertyType, CultureInfo.InvariantCulture);
                pi.SetValue(this, myItem, new object[] { });
                log.InfoFormat("{0}: {1} <- {2}", this.Name, name, myItem);
            }
        }
        #endregion
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the unambiguous range for a single modulation frequency.
        /// Range = C / (2 * f_{mod})
        /// </summary>
        /// <param name="modulationFrequency"></param>
        /// <returns></returns>
        public static float GetUnambiguousRange(float modulationFrequency)
        {
            return SpeedOfLight / (2.0f * modulationFrequency);
        }

        /// <summary>Tests if a channel is supported by the current camera.</summary>
        /// <param name="channelName">Channel name.</param>
        public bool HasChannel(string channelName)
        {
            ChannelRegistry.ChannelDescriptor tmp = null;
            try
            {
                tmp = GetChannelDescriptor(channelName);
            }
            catch (ArgumentException)
            { /* empty */ }

            return (null != tmp);
        }

        /// <summary>
        /// Provides a unique identifier for a channel.
        /// </summary>
        /// <remarks>
        /// The channel identifier includes the camera vendor and model, its serial number, and the channel's name.
        /// It does not contain space characters.
        /// </remarks>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public string ChannelIdentifier(string channelName)
        {
            return ChannelIdentifier(SerialNumber, channelName);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Implementation of <see cref="SetParameter"/> without locks.
        /// </summary>
        /// <param name="name"><see cref="SetParameter"/></param>
        /// <param name="value"><see cref="SetParameter"/></param>
        protected virtual void SetParameterWithoutLock(string name, object value)
        {
            // Check if parameter exists and is writable
            ParamDesc desc = null;
            try
            {
                desc = GetParameter(name);
            }
            catch (ArgumentException)
            {
                if (ParamDesc.IsAutoParameterName(name) && value is bool)
                {
                    SetAutoParameter(name, (bool)value);
                    return;
                }
                throw;
            }
            if (null == desc)
            {
                string msg = String.Format("{0}: Parameter {1} does not exist.", this.Name, name);
                log.Debug(msg);
                throw new ParameterNotSupportedException(msg);
            }
            if (!desc.IsWritable)
            {
                string msg = String.Format("{0}: Parameter {1} is not writable.", this.Name, name);
                log.Debug(msg);
                throw new InvalidOperationException(msg);
            }

            // Check parameter value (e.g. type, range, list, etc.)
            if (!ValidateParameterValue(desc, ref value))
            {
                string valueAsString = TypeConversion.GetAsGoodString(value);
                string msg = String.Format("{0}: Value of {1} is invalid for parameter {2}.", this.Name, valueAsString, name);
                log.Debug(msg);
                throw new ArgumentException(msg);
            }

            if (desc is IListParamDesc && desc.Type.IsEnum)
            {
                // convert string to enum type
                try
                {
                    value = Enum.Parse(desc.Type, value.ToString());
                }
                catch (ArgumentException)
                {
                    // TODO: implement case-insenitive search, if desired
                    throw; // TODO: remove, when case-insensitive search is implemented.
                }
            }

            // All checks passed. Set the new value.
            try
            {
                SetPropertyValue(name, value);
            }
            catch (FormatException e)
            {
                string msg = String.Format("{0}: Value of {1} is invalid for parameter {2}.\n({3})", this.Name, value, name, e.Message);
                log.Debug(msg);
                throw new ArgumentException(msg);
            }
        }


        /// <summary>Get the <see cref="ChannelRegistry.ChannelDescriptor"/> for a channel name.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <exception cref="ArgumentException">[InDebug] If channel name is invalid for the camera.</exception>
        /// <remarks>In Release mode no exception is thrown, but null returned.</remarks>
        protected ChannelRegistry.ChannelDescriptor GetChannelDescriptor(string channelName)
        {
            foreach (var c in Channels)
            {
                if (c.Name == channelName)
                {
                    return c;
                }
            }

#if DEBUG
            ExceptionBuilder.Throw(typeof(ArgumentException), this, "error_invalidChannelName", channelName);
#endif
            return null;
        }

        /// <summary>
        /// Adds a channel to the list of <see cref="ActiveChannels"/> if it is not in it already.
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <returns>Whether the channel was actually added (true) or already on the list (false).</returns>
        protected bool AddToActiveChannels(string channelName)
        {
            if (IsChannelActive(channelName))
            {
                return false;
            }

            ActiveChannels.Add(GetChannelDescriptor(channelName));
            return true;
        }
        #endregion

        #region Abstract and Empty Virtual Methods
        /// <summary>
        /// Reset list of available channels (<see cref="Channels"/>) to union of all cameras supported by the implementing class.
        /// </summary>
        protected abstract void LoadAllAvailableChannels();
        /// <summary>
        /// Device-specific implementation of <see cref="Connect"/>.
        /// Connects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Connect"/> inside the camera lock.</remarks>
        protected abstract void ConnectImpl();
        /// <summary>
        /// Device-specific implementation of <see cref="Disconnect"/>.
        /// Disconnects the camera.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Disconnect"/> (usually inside the camera lock).</remarks>
        protected abstract void DisconnectImpl();
        /// <summary>
        /// Device-specific implementation of <see cref="Update"/>.
        /// Updates data buffers of all active channels with data of current frame.
        /// </summary>
        /// <remarks>This method is implicitely called by <see cref="Camera.Update"/> inside the camera lock.</remarks>
        protected abstract void UpdateImpl();
        /// <summary>
        /// Device-specific implementation of <see cref="ActivateChannel"/>.
        /// Activates a channel if is not yet active.
        /// </summary>
        /// <param name="channelName">Channel name.</param>
        /// <remarks>
        /// Depending on the underlying camera, the channel stream may be created / registered in this method.
        /// This method is implicitely called by <see cref="Camera.ActivateChannel"/> inside the camera lock.
        /// </remarks>
        protected virtual void ActivateChannelImpl(string channelName)
        {
            /*empty*/
        }
        /// <summary>
        /// Device-specific implementation of <see cref="DeactivateChannel"/>.
        /// Deactivates a channel, e.g. to save time in <see cref="Update"/>, if it is currently active.
        /// </summary>
        /// <param name="channelName">Channel name.</param>
        /// <remarks>
        /// Depending on the underlying camera, the channel stream may be destroyed / unregistered in this method.
        /// This method is implicitely called by <see cref="Camera.DeactivateChannel"/> inside the camera lock.
        /// </remarks>
        protected virtual void DeactivateChannelImpl(string channelName)
        {
            /*empty*/
        }

        /// <summary>Computes (image) data for a given channel.</summary>
        /// <param name="channelName">Channel name.</param>
        /// <returns>(Image) Data.</returns>
        /// <remarks>
        /// This method is implicitely called by <see cref="Camera.CalcChannel"/>, non-locked.
        /// If your implementation needs locking, use <see cref="Camera.cameraLock"/>.
        /// </remarks>
        protected abstract CameraImage CalcChannelImpl(string channelName);
        #endregion

        #region Private Methods
        /// <summary>
        /// Finds the best matching Projective Transform for a channel of this camera.
        /// </summary>
        /// <param name="channelName">Name of the channel.</param>
        /// <param name="isDefault">Indicates if the found calibration is a generic (not camera-specific) one.</param>
        /// <returns></returns>
        /// <remarks>Each camera may have several calibration files.
        /// Search order is:
        /// 1) Serial + "_" + ChannelName
        /// 1a) in local path
        /// 1b) in CalibrationPath set in registry
        /// 1c) in embedded resource
        /// 2) "default" + "_" + ChannelName
        /// 3) Serial only
        /// 4) "default" only
        /// 2-4a-c) accordingly.
        /// </remarks>
        private IProjectiveTransformation LoadPT(string channelName, out bool isDefault)
        {
            log.DebugFormat("LoadPT({0})", channelName);

            IProjectiveTransformation pt = null;
            string serial = SerialNumber;
            string suffix = channelName;
            isDefault = false;

            for (int j = 0; j <= 1; j++)// search for channel name or empty string as suffix.
            {
                serial = SerialNumber;
                isDefault = false;
                for (int i = 0; i <= 1; i++)// search for serial number or 'default'.
                {
                    string filename = ChannelIdentifier(serial, suffix) + ".pt";
                    log.DebugFormat("Looking for PT with filename {0}", filename);
                    log.Debug(" ... in current path");
                    pt = LoadPTFromCurrentPath(filename);
                    if (pt != null)
                    {
                        log.InfoFormat("Found PT file {0} in current path", filename);
                        break;
                    }
                    log.Debug("  ... in path from registry");
                    pt = LoadPTFromRegistry(filename);
                    if (pt != null)
                    {
                        log.InfoFormat("Found PT file {0} in path from registry", filename);
                        break;
                    }
                    log.Debug("  ... in embedded resources");
                    pt = LoadPTFromEmbeddedResource(filename);
                    if (pt != null)
                    {
                        log.InfoFormat("Found PT file {0} in embedded resource", filename);
                        break;
                    }

                    serial = "default";
                    isDefault = true;
                }

                if (pt != null)
                {
                    break;
                }

                //TODO: Does this make sense? 
                suffix = "";
            }

            return pt;
        }

        private string ChannelIdentifier(string _serialNumber, string channelName)
        {
            string channelIdentifier = Name + "_" + _serialNumber + (!string.IsNullOrWhiteSpace(channelName) ? "_" + channelName : "");
            return channelIdentifier.Replace(' ', '_');
        }

        private static RigidBodyTransformation LoadRBT(string rbtName)
        {
            RigidBodyTransformation rbt = null;

            string filename = rbtName + ".rbt";
            rbt = LoadRBTFromCurrentPath(filename);
            if (null != rbt)
            {
                return rbt;
            }
            rbt = LoadRBTFromRegistry(filename);
            if (null != rbt)
            {
                return rbt;
            }
            rbt = LoadRBTFromEmbeddedResource(filename);
            if (null != rbt)
            {
                return rbt;
            }

            return null;
        }
        private RigidBodyTransformation LoadRBT(string channelFromName, string channelToName)
        {
            return LoadRBT(GetRBTName(channelFromName, channelToName));
        }
        private string GetRBTName(string channelFromName, string channelToName)
        {
            return GetRBTName(this, channelFromName, this, channelToName);
        }
        private static string GetRBTName(Camera camFrom, string channelFromName, Camera camTo, string channelToName)
        {
            return camFrom.ChannelIdentifier(channelFromName)
                + "_"
                + camTo.ChannelIdentifier(channelToName);
        }

        private static IProjectiveTransformation LoadPTFromCurrentPath(string filename)
        {
            return LoadPTFromFilesystem(".", filename);
        }
        private static IProjectiveTransformation LoadPTFromRegistry(string filename)
        {
            return LoadPTFromFilesystem(GetCalibrationPathFromRegistry(), filename);
        }
        private static IProjectiveTransformation LoadPTFromFilesystem(string folder, string filename)
        {
            if (null == folder || "" == folder || null == filename || "" == filename)
            {
                return null;
            }
            return LoadPTFromFilesystem(folder + Path.DirectorySeparatorChar + filename);
        }
        private static IProjectiveTransformation LoadPTFromFilesystem(string filename)
        {
            if (null == filename)
            {
                return null;
            }

            log.InfoFormat("Trying to load PT from {0}", filename);

            if (!File.Exists(filename))
            {
                return null;
            }

            try
            {
                return new ProjectiveTransformationZhang(filename);
            }
            catch (Exception)
            {
                return null;
            }
        }
        private static IProjectiveTransformation LoadPTFromEmbeddedResource(string filename)
        {
            Assembly entryAssembly = GetManagedEntryAssembly();

            using (Stream stream = entryAssembly.GetManifestResourceStream(filename))
            {
                if (stream == null)
                {
                    return null;
                }

                using (BinaryReader br = new BinaryReader(stream))
                {
                    return (IProjectiveTransformation)ProjectiveTransformationZhang.ReadFromMetriStream(br);
                }
            }
        }

        private static RigidBodyTransformation LoadRBTFromCurrentPath(string filename)
        {
            return LoadRBTFromFilesystem(".", filename);
        }
        private static RigidBodyTransformation LoadRBTFromRegistry(string filename)
        {
            return LoadRBTFromFilesystem(GetCalibrationPathFromRegistry(), filename);
        }
        private static RigidBodyTransformation LoadRBTFromFilesystem(string folder, string filename)
        {
            if (null == folder || "" == folder || null == filename || "" == filename)
            {
                return null;
            }
            return LoadRBTFromFilesystem(folder + Path.DirectorySeparatorChar + filename);
        }
        private static RigidBodyTransformation LoadRBTFromFilesystem(string filename)
        {
            if (null == filename)
            {
                return null;
            }

            log.InfoFormat("Trying to load RBT from {0}", filename);

            if (!File.Exists(filename))
            {
                return null;
            }

            try
            {
                using (BinaryReader br = new BinaryReader(File.OpenRead(filename)))
                {
                    return RigidBodyTransformation.ReadFromMetriStream(br);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        private static RigidBodyTransformation LoadRBTFromEmbeddedResource(string filename)
        {
            Assembly entryAssembly = GetManagedEntryAssembly();

            using (Stream stream = entryAssembly.GetManifestResourceStream(filename))
            {
                if (stream == null)
                {
                    return null;
                }

                using (BinaryReader br = new BinaryReader(stream))
                {
                    return RigidBodyTransformation.ReadFromMetriStream(br);
                }
            }
        }

        private static String GetCalibrationPathFromRegistry()
        {
            log.Debug("GetCalibrationPathFromRegistry");

#if NETSTANDARD2_0
            return null;
#else
            if (null != calibrationPathRegistry)
            {
                log.DebugFormat("  using cached value: '{0}'", calibrationPathRegistry);
                return calibrationPathRegistry;
            }

            calibrationPathRegistry = "";

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
            {
                //throw new Exception("Registry key not found! Please reinstall.");
            }

            if (null == rk)
            {
                log.DebugFormat(@"Registry key HKLM\{0}\ not found", string.Join(@"\", subkeys));
                return null;
            }

            try
            {
                calibrationPathRegistry = (String)rk.GetValue("CalibrationPath");
            }
            catch
            {
                //throw new Exception("Registry key not found! Please reinstall.");
            }

            if (!Directory.Exists(calibrationPathRegistry))
            {
                //throw new Exception("Either Registry key not found, or path set in registry is not valid! Please reinstall.");
                log.DebugFormat(@"Directory '{0}' not found", calibrationPathRegistry);
                calibrationPathRegistry = "";
            }

            log.DebugFormat(@"Calibration path = '{0}'", calibrationPathRegistry);

            return calibrationPathRegistry;
#endif
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

            // if there is a _native_ entry point
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

        private static Attribute GetAttribute(Attribute[] attributes, Type AttributeType)
        {
            foreach (Attribute a in attributes)
            {
                if (a.GetType() == AttributeType)
                {
                    return a;
                }
            }

            return null;
        }

        private static ConnectionStates IsReadable(AccessStateAttribute access)
        {
            return access != null ? access.ReadableWhen : Enums.ConnectionStates.None;
        }

        private static ConnectionStates IsWritable(AccessStateAttribute access)
        {
            return access != null ? access.ReadableWhen : Enums.ConnectionStates.None;
        }

        private static string GetUnit(UnitAttribute unit)
        {
            return unit != null ? unit.Unit : "";
        }
        #endregion
    }
}
