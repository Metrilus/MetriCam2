// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

#pragma once
#include <msclr/marshal.h>
#include <OpenNI.h>
#include "cmd.h"

using namespace System;
using namespace System::ComponentModel;
using namespace System::Threading;
using namespace System::Runtime::InteropServices;
using namespace System::Drawing;
using namespace Metrilus::Util;
using namespace Metrilus::Logging;

namespace MetriCam2 
{
	namespace Cameras 
	{

		struct OrbbecNativeCameraData
		{
			cmd* openNICam;

			openni::VideoStream* depth;
			int depthWidth;
			int depthHeight;

			openni::VideoStream* ir;
			int irWidth;
			int irHeight;

			openni::VideoStream* color;
			int colorWidth;
			int colorHeight;
		};

		public ref class AstraOpenNI : Camera
		{
		public:
			AstraOpenNI();
			~AstraOpenNI();

			property ParamDesc<bool>^ EmitterEnabledDesc
			{
				inline ParamDesc<bool> ^get()
				{
					ParamDesc<bool> ^res = gcnew ParamDesc<bool>();
					res->Unit = "";
					res->Description = "Emitter is enabled";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			property bool EmitterEnabled
			{
				bool get(void)
				{
					//Reader the emitter status via the "cmd" class does not yet work. Check in future version of experimental SDK.
					return _emitterEnabled;
				}
				void set(bool value)
				{
					_emitterEnabled = value;
					SetEmitterStatus(_emitterEnabled);
					log->DebugFormat("Emitter state set to: {0}", _emitterEnabled.ToString());
				}
			}

			property ParamDesc<bool>^ IRFlooderEnabledDesc
			{
				inline ParamDesc<bool> ^get()
				{
					ParamDesc<bool> ^res = gcnew ParamDesc<bool>();
					res->Unit = "";
					res->Description = "IR flooder is enabled";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			property bool IRFlooderEnabled
			{
				bool get(void)
				{
					//Reader the IrFlood status via the "cmd" class does not yet work. Check in future version of experimental SDK.
					return _irFlooderEnabled;
				}
				void set(bool value)
				{
					_irFlooderEnabled = value;
					SetIRFlooderStatus(_irFlooderEnabled);
					log->DebugFormat("IR flooder state set to: {0}", _irFlooderEnabled.ToString());
				}
			}

			property unsigned char IRGain
			{
				unsigned char get(void)
				{
					return (unsigned char)GetIRGain();
				}
				void set(unsigned char value)
				{
					SetIRGain(value);
					_irGain = value;
				}
			}

			// Implementation in experimental interface seems to be buggy, changing the value destroys the distance image
			property unsigned int IRExposure
			{
				unsigned int get(void)
				{
					//ir_exposure_get in cmd class not yet functional and can destroy the current state of the camera
					throw gcnew NotImplementedException();
					//return GetIRExposure();
				}
				void set(unsigned int value)
				{
					SetIRExposure(value);
					// Set IRExposure resets the gain to its default value (96 for Astra and 8 for AstraS). We have to set the gain to the memorized value (member irGain).
					SetIRGain(_irGain);
				}
			}

			static System::Collections::Generic::Dictionary<String^, String^>^ GetSerialToUriMappingOfAttachedCameras();

			virtual Metrilus::Util::IProjectiveTransformation^ GetIntrinsics(String^ channelName) override;
			virtual Metrilus::Util::RigidBodyTransformation^ GetExtrinsics(String^ channelFromName, String^ channelToName) override;

		protected:
			/// <summary>
			/// Resets list of available channels (<see cref="Channels"/>) to union of all cameras supported by the implementing class.
			/// </summary>
			virtual void LoadAllAvailableChannels() override;

			/// <summary>
			/// Connects the camera.
			/// </summary>
			/// <remarks>This method is implicitely called by <see cref="Camera.Connect"/> inside a camera lock.</remarks>
			/// <seealso cref="Camera.Connect"/>
			virtual void ConnectImpl() override;

			/// <summary>
			/// Disconnects the camera.
			/// </summary>
			/// <remarks>This method is implicitely called by <see cref="Camera.Disconnect"/> inside a camera lock.</remarks>
			/// <seealso cref="Camera.Disconnect"/>
			virtual void DisconnectImpl() override;

			/// <summary>
			/// Updates data buffers of all active channels with data of current frame.
			/// </summary>
			/// <remarks>This method is implicitely called by <see cref="Camera.Update"/> inside a camera lock.</remarks>
			/// <seealso cref="Camera.Update"/>
			virtual void UpdateImpl() override;

			/// <summary>Computes (image) data for a given channel.</summary>
			/// <param name="channelName">Channel name.</param>
			/// <returns>(Image) Data.</returns>
			/// <seealso cref="Camera.CalcChannel"/>
			virtual Metrilus::Util::CameraImage^ CalcChannelImpl(String^ channelName) override;

			/// <summary>
			/// Activate a channel.
			/// </summary>
			/// <param name="channelName">Channel name.</param>
			virtual void ActivateChannelImpl(String^ channelName) override;

			/// <summary>
			/// Deactivate a channel.
			/// </summary>
			/// <param name="channelName">Channel name.</param>
			virtual void DeactivateChannelImpl(String^ channelName) override;
			
		private:
			FloatCameraImage^ CalcZImage();
			ColorCameraImage^ CalcColor();
			Point3fCameraImage^ CalcPoint3fImage();
			FloatCameraImage^ CalcIRImage();

			static bool OpenNIInit();
			static bool OpenNIShutdown();
			static void LogOpenNIError(String^ status);
			static int _openNIInitCounter = 0;

			unsigned short _irGain = 0;

			void InitDepthStream();
			void InitIRStream();
			void InitColorStream();

			String^ GetIRFlooderStatus();
			void SetIRFlooderStatus(bool on);

			String^ GetEmitterStatus();
			void SetEmitterStatus(bool on);

			void SetIRGain(char value);
			unsigned short GetIRGain();

			void SetIRExposure(unsigned int value);
			unsigned int GetIRExposure();

			bool _emitterEnabled;
			bool _irFlooderEnabled;
			OrbbecNativeCameraData* _pCamData;
		};
	}
}