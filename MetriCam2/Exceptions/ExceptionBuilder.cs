// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2.Exceptions;
using Metrilus.Logging;
using System;
using System.Reflection;
using System.Resources;

namespace MetriCam2
{
    public class ExceptionBuilder
    {
        #region Private Fields
        /// <summary>
        /// The logger.
        /// </summary>
        private static MetriLog log = new MetriLog("MetriCam2");
        #endregion

        #region Private Methods
        private static Exception Build(Type exType, string msg)
        {
            log.ErrorFormat("{0}: {1}", exType.Name, msg);

            // Try ctor with message
            ConstructorInfo ci = exType.GetConstructor(new Type[] { typeof(string) });
            if (null != ci)
            {
                return (Exception)ci.Invoke(new object[] { msg });
            }

            // Fall-back
            return new Exception(exType + ": " + msg);
        }
        #endregion

        #region Public Methods
        public static MetriCam2Exception Build(Type exType, string cameraName, string message, Exception inner)
        {
            string msg = cameraName + ": " + message;
            ConstructorInfo ci = exType.GetConstructor(new Type[] { typeof(string), typeof(Exception) });
            log.ErrorFormat("{0}: {1}", exType.Name, msg);
            log.ErrorFormat("    inner exception {0}: {1}", inner.GetType().Name, inner.Message);
            return (MetriCam2Exception)ci.Invoke(new object[] { msg, inner });
        }

        /// <summary>
        /// This is a special version of the Build methods to allow customer-specific exception IDs.
        /// </summary>
        /// <param name="exceptionType"></param>
        /// <param name="cam"></param>
        /// <param name="exceptionID"></param>
        /// <param name="additionalInformation"></param>
        /// <returns></returns>
        public static MetriCam2Exception BuildFromID(Type exceptionType, Camera cam, int exceptionID, string additionalInformation = null)
        {
            ResourceManager rm = new ResourceManager("MetriCam2.Cameras.Properties.Resources", cam.GetType().Assembly);
            string resourceID = "_" + exceptionID.ToString("000");
            string fullExceptionID = cam.GetType().Name + resourceID;
            string message = null;
            try
            {
                message = rm.GetString(resourceID);
            }
            catch
            { /* ignore */ }
            string fullMessage = fullExceptionID
                + (string.IsNullOrWhiteSpace(message)
                    ? ""
                    : ": " + message)
                + (string.IsNullOrWhiteSpace(additionalInformation)
                    ? ""
                    : " [" + additionalInformation + "]");

            Exception ex = Build(exceptionType, fullMessage);
            if (exceptionType.IsSubclassOf(typeof(MetriCam2Exception)))
            {
                return (MetriCam2Exception)ex;
            }
            else
            {
                return new MetriCam2Exception(fullMessage, ex);
            }
        }

        public static Exception Build(Type exType, string cameraName, string messageCode)
        {
            string localizedErrorMessage = Localization.GetString(messageCode);

            if (localizedErrorMessage == null)
            {
                localizedErrorMessage = messageCode;
            }
            string msg = cameraName + ": " + localizedErrorMessage;

            return Build(exType, msg);
        }

        /// <summary>
        /// Throw methods are deprecated. Use <code>throw Build(...)</code> instead.
        /// </summary>
        /// <param name="exType"></param>
        /// <param name="cameraName"></param>
        /// <param name="messageCode"></param>
        /// <param name="messageExtraInfo"></param>
        public static Exception Build(Type exType, string cameraName, string messageCode, string messageExtraInfo)
        {
            string localizedErrorMessage = Localization.GetString(messageCode);

            if (localizedErrorMessage == null)
            {
                return Build(exType, cameraName, "Unknown error occurred.");
            }

            if (localizedErrorMessage.Contains("{0}"))
            {
                return Build(exType, cameraName + ": " + string.Format(localizedErrorMessage, messageExtraInfo));
            }
            else
            {
                return Build(exType, cameraName + ": " + localizedErrorMessage + "\n" + messageExtraInfo);
            }
        }
        #endregion
    }
}
