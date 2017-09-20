﻿// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Logging;
using Metrilus.Util;
using System;
using System.Collections.Generic;

namespace MetriCam2
{
    public class ChannelRegistry
    {
        #region Types
        public class ChannelDescriptor
        {
            internal ChannelDescriptor(string name, Type imageType)
            {
                if (!typeof(CameraImage).IsAssignableFrom(imageType))
                {
                    throw new ArgumentException(this.GetType().Name + ": imageType has to be of CameraImage.", "imageType");
                }
                Name = name;
                ImageType = imageType;
            }

            public string Name { get; private set; }
            public Type ImageType { get; private set; }
        }
        #endregion

        #region Public Properties
        /// <summary>Singleton instance.</summary>
        public static ChannelRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ChannelRegistry();
                }
                return instance;
            }
        }
        #endregion

        #region Private Fields
        private static MetriLog log = new MetriLog();
        private static ChannelRegistry instance = null; // Singleton holder.
        private Dictionary<string, Type> registeredChannels;
        #endregion

        #region Constructor
        private ChannelRegistry()
        {
            registeredChannels = new Dictionary<string, Type>();
            registeredChannels.Add(ChannelNames.Disparities, typeof(FloatCameraImage));
            registeredChannels.Add(ChannelNames.Distance, typeof(FloatCameraImage));
            registeredChannels.Add(ChannelNames.Color, typeof(ColorCameraImage));
            registeredChannels.Add(ChannelNames.Red, typeof(FloatCameraImage));
            registeredChannels.Add(ChannelNames.Green, typeof(FloatCameraImage));
            registeredChannels.Add(ChannelNames.Blue, typeof(FloatCameraImage));
            registeredChannels.Add(ChannelNames.Amplitude, typeof(FloatCameraImage));
            registeredChannels.Add(ChannelNames.Intensity, typeof(FloatCameraImage));
            registeredChannels.Add(ChannelNames.ZImage, typeof(FloatCameraImage));
            registeredChannels.Add(ChannelNames.Point3DImage, typeof(Point3fCameraImage));
            registeredChannels.Add(ChannelNames.ConfidenceMap, typeof(FloatCameraImage));
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// A camera can register channels of a default name and type (e.g. <see cref="ChannelNames.Distance"/>).
        /// </summary>
        /// <param name="name">Symbolic name of the channel.</param>
        /// <returns>ChannelName object containing information about the channel.</returns>
        /// <remarks>The type is derived automatically from the channel name.</remarks>
        public ChannelDescriptor RegisterChannel(string name)
        {
            Type imageType;
            bool isKnownChannel = registeredChannels.TryGetValue(name, out imageType);
            if (!isKnownChannel)
            {
                string msg = string.Format("{0}: Could not register channel '{1}'. Channels of that name are unknown.", this.GetType().Name, name);
                log.Warn(msg);
                throw new ArgumentException(msg);
            }

            return new ChannelDescriptor(name, imageType);
        }
        /// <summary>
        /// A camera can register custom channels which are not of a default name and type.
        /// </summary>
        /// <param name="name">Symbolic name of the channel.</param>
        /// <param name="imageType">Data type of the channel.</param>
        /// <returns>ChannelName object containing information about the channel.</returns>
        /// <remarks>If a custom channel has been registered before, the new one will be ignored (assuming it has the same type).</remarks>
        public ChannelDescriptor RegisterCustomChannel(string name, Type imageType)
        {
            AddChannel(name, imageType);
            return new ChannelDescriptor(name, imageType);
        }
        #endregion

        #region Private Methods
        private void AddChannel(string channelName, Type imageType)
        {
            try
            {
                registeredChannels.Add(channelName, imageType);
            }
            catch (ArgumentException)
            {
                // TODO: Overthink channel registration best practices. This warning is always issued when a new object of a previously instanciated camera type is created. Possible solution: Do channel registration in Camera base-class and provide a virtual property which is overriden to supply all possible camera channels.
                // if a channel of that name has been added before, ignore this one.
                // log.WarnFormat("A channel with the name '{0}' already existed (maybe you used a default channel name of MetriCam 2). Ignoring the new channel.", channelName);
            }
        }
        #endregion
    }
}
