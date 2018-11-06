// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

#pragma once
#include <msclr/marshal.h>
#include <OpenNI.h>
#include "cmd.h"

//Adpated from SimpleViewer of experimental interface
#define IR_Exposure_MAX 4096
#define IR_Exposure_MIN 0
#define IR_Exposure_SCALE 256
#define IR_Gain_MIN 8
#define IR_Gain_MAX 96

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

		public ref class AstraOpenNI : Camera, IDisposable
		{
		public:
			AstraOpenNI();
			~AstraOpenNI();
			!AstraOpenNI();

			property int ProductID;
			property int VendorID;

			property bool EmitterEnabled
			{
				bool get(void)
				{
					// Reading the emitter status via the "cmd" class does not yet work. Check in future version of experimental SDK.
					return _emitterEnabled;
				}
				void set(bool value)
				{
					_emitterEnabled = value;
					SetEmitterStatus(_emitterEnabled);
					log->DebugFormat("Emitter state set to: {0}", _emitterEnabled.ToString());
				}
			}

			property bool IRFlooderEnabled
			{
				bool get(void)
				{
					// Reading the IrFlood status via the "cmd" class does not yet work. Check in future version of experimental SDK.
					return _irFlooderEnabled;
				}
				void set(bool value)
				{
					_irFlooderEnabled = value;
					SetIRFlooderStatus(_irFlooderEnabled);
					log->DebugFormat("IR flooder state set to: {0}", _irFlooderEnabled.ToString());
				}
			}

			property int IRGain
			{
				int get(void)
				{
					return _irGain;
				}
				void set(int value)
				{
					if (value != _irGain)
					{
						_irGain = value;
						SetIRGain(_irGain);
					}
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

#if !NETSTANDARD2_0
			property System::Drawing::Icon^ CameraIcon
			{
				System::Drawing::Icon^ get() override
				{
					System::Reflection::Assembly^ assembly = System::Reflection::Assembly::GetExecutingAssembly();
					System::IO::Stream^ iconStream = assembly->GetManifestResourceStream("OrbbecIcon.ico");
					return gcnew System::Drawing::Icon(iconStream);
				}
			}
#endif

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
			property ParamDesc<bool>^ EmitterEnabledDesc
			{
				inline ParamDesc<bool> ^get()
				{
					ParamDesc<bool> ^res = gcnew ParamDesc<bool>();
					res->Unit = "";
					res->Description = "Emitter is enabled";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			property ParamDesc<bool>^ IRFlooderEnabledDesc
			{
				inline ParamDesc<bool> ^get()
				{
					ParamDesc<bool> ^res = gcnew ParamDesc<bool>();
					res->Unit = "";
					res->Description = "IR flooder is enabled";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			// Disabled while the IRExposure getter is not implemented
			//property ParamDesc<unsigned int>^ IRExposureDesc
			//{
			//	inline ParamDesc<unsigned int> ^get()
			//	{
			//		ParamDesc<unsigned int> ^res = gcnew ParamDesc<unsigned int>();
			//		res->Unit = "";
			//		res->Description = "IR exposure";
			//		res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
			//		res->WritableWhen = ParamDesc::ConnectionStates::Connected;
			//		return res;
			//	}
			//}

			property ParamDesc<int>^ IRGainDesc
			{
				inline ParamDesc<int> ^get()
				{
					ParamDesc<int> ^res = ParamDesc::BuildRangeParamDesc(IR_Gain_MIN, IR_Gain_MAX);
					res->Unit = "";
					res->Description = "IR gain";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			FloatCameraImage^ CalcZImage();
			ColorCameraImage^ CalcColor();
			Point3fCameraImage^ CalcPoint3fImage();
			FloatCameraImage^ CalcIRImage();

			static bool OpenNIInit();
			static bool OpenNIShutdown();
			static void LogOpenNIError(String^ status);
			static int _openNIInitCounter = 0;

			bool _isDisposed = false;
			int _irGain = 0;

			void InitDepthStream();
			void InitIRStream();
			void InitColorStream();

			String^ GetIRFlooderStatus();
			void SetIRFlooderStatus(bool on);

			String^ GetEmitterStatus();
			void SetEmitterStatus(bool on);

			void SetIRGain(int value);
			unsigned short GetIRGain();

			void SetIRExposure(unsigned int value);
			unsigned int GetIRExposure();

			bool _emitterEnabled;
			bool _irFlooderEnabled;
			OrbbecNativeCameraData* _pCamData;

			msclr::interop::marshal_context marshalContext;
		};
	}
}