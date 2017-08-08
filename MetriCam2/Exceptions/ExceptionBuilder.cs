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
            ConstructorInfo ci = exType.GetConstructor(new Type[] { typeof(string) });
            log.ErrorFormat("{0}: {1}", exType.Name, msg);
            return (Exception)ci.Invoke(new object[] { msg });
        }
        private static void Throw(Type exType, string msg)
        {
            throw Build(exType, msg);
        }

        private static MetriCam2Exception Build(Type exType, string msg, Exception inner)
        {
            ConstructorInfo ci = exType.GetConstructor(new Type[] { typeof(string), typeof(Exception) });
            log.ErrorFormat("{0}: {1}", exType.Name, msg);
            log.ErrorFormat("    inner exception {0}: {1}", inner.GetType().Name, inner.Message);
            return (MetriCam2Exception)ci.Invoke(new object[] { msg, inner });
        }
        private static void Throw(Type exType, string msg, Exception inner)
        {
            throw Build(exType, msg, inner);
        }

        private static void Throw(Type exType, string name, string message, Exception inner)
        {
            throw Build(exType, name + ": " + message, inner);
        }
        #endregion

        #region Public Methods
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
        public static Exception Build(Type exType, Camera cam, string messageCode)
        {
            return Build(exType, cam.Name, messageCode);
        }
        public static Exception Build(Type exType, string name, string messageCode)
        {
            string localizedErrorMessage = Localization.GetString(messageCode);

            if (localizedErrorMessage == null)
            {
                localizedErrorMessage = messageCode;
            }
            string msg = name + ": " + localizedErrorMessage;

            return Build(exType, msg);
        }

        /// <summary>
        /// Throw methods are deprecated. Use <code>throw Build(...)</code> instead.
        /// </summary>
        /// <param name="exType"></param>
        /// <param name="cam"></param>
        /// <param name="messageCode"></param>
        public static void Throw(Type exType, Camera cam, string messageCode)
        {
            throw Build(exType, cam.Name, messageCode);
        }

        /// <summary>
        /// Throw methods are deprecated. Use <code>throw Build(...)</code> instead.
        /// </summary>
        /// <param name="exType"></param>
        /// <param name="name"></param>
        /// <param name="messageCode"></param>
        public static void Throw(Type exType, string name, string messageCode)
        {
            throw Build(exType, name, messageCode);
        }

        /// <summary>
        /// Throw methods are deprecated. Use <code>throw Build(...)</code> instead.
        /// </summary>
        /// <param name="exType"></param>
        /// <param name="cam"></param>
        /// <param name="messageCode"></param>
        /// <param name="messageExtraInfo"></param>
        public static void Throw(Type exType, Camera cam, string messageCode, string messageExtraInfo)
        {
            Throw(exType, cam.Name, messageCode, messageExtraInfo);
        }

        /// <summary>
        /// Throw methods are deprecated. Use <code>throw Build(...)</code> instead.
        /// </summary>
        /// <param name="exType"></param>
        /// <param name="name"></param>
        /// <param name="messageCode"></param>
        /// <param name="messageExtraInfo"></param>
        public static void Throw(Type exType, string name, string messageCode, string messageExtraInfo)
        {
            string localizedErrorMessage = Localization.GetString(messageCode);

            if (localizedErrorMessage == null)
            {
                Throw(exType, name, "Unknown error occurred.");
            }

            if (localizedErrorMessage.Contains("{0}"))
            {
                throw Build(exType, name + ": " + string.Format(localizedErrorMessage, messageExtraInfo));
            }
            else
            {
                throw Build(exType, name + ": " + localizedErrorMessage + "\n" + messageExtraInfo);
            }
        }

        /// <summary>
        /// Throw methods are deprecated. Use <code>throw Build(...)</code> instead.
        /// </summary>
        /// <param name="exType"></param>
        /// <param name="cam"></param>
        /// <param name="messageCode"></param>
        /// <param name="messageExtraInfo"></param>
        /// <param name="oniIError"></param>
        public static void Throw(Type exType, Camera cam, string messageCode, string messageExtraInfo, string oniIError)
        {
            Throw(exType, cam.Name, messageCode, messageExtraInfo, oniIError);
        }

        /// <summary>
        /// Throw methods are deprecated. Use <code>throw Build(...)</code> instead.
        /// </summary>
        /// <param name="exType"></param>
        /// <param name="name"></param>
        /// <param name="messageCode"></param>
        /// <param name="messageExtraInfo"></param>
        /// <param name="oniIError"></param>
        public static void Throw(Type exType, string name, string messageCode, string messageExtraInfo, string oniIError)
        {
            string localizedErrorMessage = Localization.GetString(messageCode);

            if (localizedErrorMessage == null)
            {
                Throw(exType, name, "Unknown error occurred.");
            }

            if (localizedErrorMessage.Contains("{0}"))
            {
                Throw(exType, name + ": " + String.Format(localizedErrorMessage, messageExtraInfo) + "\nOpenNI2 error message: " + oniIError);
            }
            else
            {
                Throw(exType, name + ": " + localizedErrorMessage + "\n" + messageExtraInfo + "\nOpenNI2 error message: " + oniIError);
            }
        }

        /// <summary>
        /// Throw methods are deprecated. Use <code>throw Build(...)</code> instead.
        /// </summary>
        /// <param name="exType"></param>
        /// <param name="cam"></param>
        /// <param name="inner"></param>
        public static void Throw(Type exType, Camera cam, Exception inner)
        {
            Throw(exType, cam.Name, inner.Message, inner);
        }
        #endregion
    }
}
