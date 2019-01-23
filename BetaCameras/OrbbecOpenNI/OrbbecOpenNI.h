// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

#pragma once
// When USE_I2C_GAIN is set, then the old, I2C code is used to get/set the IrGain.
// Otherwise, the new Orbbec OpenNI extension is used (which seems still buggy).
#define USE_I2C_GAIN 1

#include <msclr/marshal.h>
#include <PS1080.h>
#include <OpenNI.h>

#if USE_I2C_GAIN
#include <iostream>
#include <vector>
#endif

//Adpated from SimpleViewer of experimental interface
const int IR_Exposure_MAX = 1 << 14;
const int IR_Exposure_MIN = 0;
const int IR_Gain_MIN = 8;
const int IR_Gain_MAX = 63;

using namespace System;
using namespace System::ComponentModel;
using namespace System::Threading;
using namespace System::Runtime::CompilerServices;
using namespace System::Runtime::InteropServices;
using namespace System::Drawing;
using namespace System::Collections::Generic;
using namespace Metrilus::Util;
using namespace Metrilus::Logging;

#if USE_I2C_GAIN
bool atoi2(const char* str, int* pOut);
unsigned short read_i2c(openni::Device& device, std::vector<std::string>& Command, XnControlProcessingData& I2C);
bool write_i2c(openni::Device& device, std::vector<std::string>& Command, XnControlProcessingData& I2C);
template<typename ... Args>
std::string string_format(const std::string& format, Args ... args);
#endif

namespace MetriCam2 
{
	namespace Cameras 
	{
		struct OrbbecNativeCameraData
		{
			openni::Device device;

			openni::VideoStream depth;
			int depthWidth;
			int depthHeight;

			openni::VideoStream ir;
			int irWidth;
			int irHeight;

			openni::VideoStream color;
			int colorWidth;
			int colorHeight;
		};

		public ref class AstraOpenNI : Camera, IDisposable
		{
		public:
			AstraOpenNI();
			~AstraOpenNI();
			!AstraOpenNI();

			virtual property String^ Vendor
			{
				String^ get() override
				{
					return "Orbbec";
				}
			}
			property int ProductID
			{
				int get() { return _pid; }
			private:
				void set(int value) { _pid = value; }
			}
			property int VendorID
			{
				int get() { return _vid; }
			private:
				void set(int value) { _vid = value; }
			}
			property String^ DeviceType
			{
				String^ get() { return _deviceType; }
			private:
				void set(String^ value) { _deviceType = value; }
			}

			property bool EmitterEnabled
			{
				bool get() { return GetEmitterStatus(); }
				void set(bool value) { SetEmitterStatus(value); }
			}

			property int IRExposure
			{
				int get() { return GetIRExposure(); }
				void set(int value)
				{
					auto irGainBefore = GetIRGain();
					SetIRExposure(value);
					// Set IRExposure resets the gain to its default value (96 for Astra and 8 for AstraS). We have to set the gain to the memorized value (member irGain).
					SetIRGain(irGainBefore);
				}
			}

			property bool IRFlooderEnabled
			{
				bool get() { return GetIRFlooderStatus(); }
				void set(bool value) { SetIRFlooderStatus(value); }
			}

			property int IRGain
			{
				int get() { return GetIRGain(); }
				void set(int value) { SetIRGain(value); }
			}

			//Is buggy in OpenNI version 2.3.1.48, depth channel (if started) will turn black if one of this methods is called.
			/*property bool ProximitySensorEnabled
			{
				bool get() { return GetProximitySensorStatus(); }
				void set(bool value) { SetProximitySensorStatus(value); }
			}*/

			static System::Collections::Generic::Dictionary<String^, String^>^ GetSerialToUriMappingOfAttachedCameras();

			virtual Metrilus::Util::IProjectiveTransformation^ GetIntrinsics(String^ channelName) override;
			virtual Metrilus::Util::RigidBodyTransformation^ GetExtrinsics(String^ channelFromName, String^ channelToName) override;

			/// <summary>
			/// Updates the emitter (laser) status and waits for the next valid or invalid frame.
			/// </summary>
			/// <remarks>
			/// Currently only implemented if the z-image channel is active.
			/// If it's not active the wait will be skipped.
			/// </remarks>
			void SetEmitterStatusAndWait(bool on);

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
			property openni::Device& Device
			{
				openni::Device& get()
				{
					return _pCamData->device;
				}
			}
			property openni::VideoStream& DepthStream
			{
				openni::VideoStream& get()
				{
					return _pCamData->depth;
				}
			}
			property openni::VideoStream& IrStream
			{
				openni::VideoStream& get()
				{
					return _pCamData->ir;
				}
			}
			property openni::VideoStream& ColorStream
			{
				openni::VideoStream& get()
				{
					return _pCamData->color;
				}
			}

			property ParamDesc<bool>^ EmitterEnabledDesc
			{
				inline ParamDesc<bool>^ get()
				{
					ParamDesc<bool>^ res = gcnew ParamDesc<bool>();
					res->Unit = "";
					res->Description = "Emitter is enabled";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			property ParamDesc<int>^ IRExposureDesc
			{
				inline ParamDesc<int>^ get()
				{
					ParamDesc<int>^ res = ParamDesc::BuildRangeParamDesc(IR_Exposure_MIN, IR_Exposure_MAX);
					res->Unit = "";
					res->Description = "IR exposure";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			property ParamDesc<bool>^ IRFlooderEnabledDesc
			{
				inline ParamDesc<bool>^ get()
				{
					ParamDesc<bool>^ res = gcnew ParamDesc<bool>();
					res->Unit = "";
					res->Description = "IR flooder is enabled";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			property ParamDesc<int>^ IRGainDesc
			{
				inline ParamDesc<int>^ get()
				{
					ParamDesc<int>^ res = ParamDesc::BuildRangeParamDesc(IR_Gain_MIN, IR_Gain_MAX);
					res->Unit = "";
					res->Description = "IR gain";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			property ParamDesc<bool>^ ProximitySensorEnabledDesc
			{
				inline ParamDesc<bool>^ get()
				{
					ParamDesc<bool>^ res = gcnew ParamDesc<bool>();
					res->Unit = "";
					res->Description = "Proximity sensor is enabled";
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

			void InitDepthStream();
			void InitIRStream();
			void InitColorStream();

			bool _irFlooderEnabled;
			bool GetIRFlooderStatus();
			void SetIRFlooderStatus(bool on);

			bool _emitterEnabled;
			bool GetEmitterStatus();
			void SetEmitterStatus(bool on);

			//Is buggy in OpenNI version 2.3.1.48, depth channel (if started) will turn black if one of this methods is called.
			/*bool GetProximitySensorStatus();
			void SetProximitySensorStatus(bool on);*/

			int GetIRGain();
			void SetIRGain(int value);

			int GetIRExposure();
			void SetIRExposure(int value);

			void WaitUntilNextValidFrame();
			void WaitUntilNextInvalidFrame();
			bool IsDepthFrameValid_MinimumMean(FloatCameraImage^ img);
			bool IsDepthFrameValid_NumberNonZeros(FloatCameraImage^ img);
			bool IsDepthFrameValid_MinimumMean(FloatCameraImage^ img, float threshold);
			bool IsDepthFrameValid_NumberNonZeros(FloatCameraImage^ img, int thresholdPercentage);

			OrbbecNativeCameraData* _pCamData;
			int _vid;
			int _pid;
			String^ _deviceType;
			Point2i _depthResolution;
			bool _hasColor;
			// Compensate for offset between IR and Distance images:
			// Translate infrared frame by a certain number of pixels in vertical direction to match infrared with depth image.
			int _intensityYTranslation;
			System::Collections::Generic::Dictionary<String^, RigidBodyTransformation^>^ _extrinsicsCache;
			System::Collections::Generic::Dictionary<String^, IProjectiveTransformation^>^ _intrinsicsCache;

			msclr::interop::marshal_context marshalContext;
		};
	}
}