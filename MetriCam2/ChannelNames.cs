using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetriCam2
{
    /// <summary>
    /// Provides names for the default channels.
    /// </summary>
    /// <remarks>Used for consistency and code-completion.</remarks>
    public abstract class ChannelNames
    {
        /// <summary>
        /// Amplitude channel.
        /// </summary>
        public const string Amplitude = "Amplitude";
        /// <summary>
        /// Color channel.
        /// </summary>
        public const string Color = "Color";
        /// <summary>
        /// Red color channel.
        /// </summary>
        public const string Red = "Red";
        /// <summary>
        /// Green color channel.
        /// </summary>
        public const string Green = "Green";
        /// <summary>
        /// Blue color channel.
        /// </summary>
        public const string Blue = "Blue";
        /// <summary>
        /// Disparities channel.
        /// </summary>
        public const string Disparities = "Disparities";
        /// <summary>
        /// Distance channel (radial distance from camera).
        /// </summary>
        public const string Distance = "Distance";
        /// <summary>
        /// Intensity channel.
        /// </summary>
        /// <remarks>An intensity channel (e.g. black/white 2D camera, IR camera, etc.).</remarks>
        public const string Intensity = "Intensity";
        /// <summary>
        /// In a camera with two sensors this channel represents the left sensor.
        /// </summary>
        public const string Left = "Left";// is not registered as default channel, because the type is unknown
        /// <summary>
        /// In a camera with two sensors this channel represents the right sensor.
        /// </summary>
        public const string Right = "Right";// is not registered as default channel, because the type is unknown
        /// <summary>
        /// <see cref="Point3DImage"/>
        /// </summary>
        [Obsolete("Use Point3DImage instead.")]
        public const string PointCloud = "PointCloud";
        /// <summary>
        /// Unfiltered 3D points, with an XY-relationship.
        /// </summary>
        public const string Point3DImage = "Point3DImage";
        /// <summary>
        /// Depth channel (z-distance from camera).
        /// </summary>
        public const string ZImage = "ZImage";
        /// <summary>
        /// Confidence map as float image with pixel values between 0 and 1
        /// </summary>
        public const string ConfidenceMap = "ConfidenceMap";
        /// <summary>
        /// Raw confidence map as provided by camera
        /// Will not be registered as default channel as type is device-specific
        /// </summary>
        public const string RawConfidenceMap = "RawConfidenceMap";
    }
}
