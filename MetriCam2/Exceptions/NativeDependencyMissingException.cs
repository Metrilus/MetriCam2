// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;

namespace MetriCam2.Exceptions
{
    /// <summary>
    /// Thrown if a MetriCam2 DLL fails to load because of missing native depencencies.
    /// </summary>
    public class NativeDependencyMissingException : MetriCam2Exception
    {
        /// <summary>
        /// Names of missing native dependencies.
        /// </summary>
        public string[] MissingNativeDependencies { get; private set; }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Standard exception message.</param>
        /// <param name="missingNativeDependencies">Names of missing native dependencies.</param>
        public NativeDependencyMissingException(string message, string[] missingNativeDependencies)
            : base(message)
        {
            this.MissingNativeDependencies = missingNativeDependencies;
        }
    }
}
