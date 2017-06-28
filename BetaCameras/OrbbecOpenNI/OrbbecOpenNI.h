// OrbbecOpenNI.h

#include <msclr/marshal.h>
#include <atomic>
#include <OpenNI.h>
#pragma once

using namespace System;
using namespace Metrilus::Util;
using namespace Metrilus::Logging;

namespace MetriCam2 {
	namespace Cameras {

		struct OrbbecNativeCameraData
		{
			openni::Device* device;
			openni::VideoStream* depth;
			int depthWidth;
			int depthHeight;
			openni::VideoStream* ir;

			int irWidth;
			int irHeight;
		};

		public ref class AstraOpenNI : Camera
		{
		public:
			AstraOpenNI();
			~AstraOpenNI();

			property bool EmitterEnabled {
				bool get(void) {
					return emitterEnabled;
				}
				void set(bool value) {
					emitterEnabled = value;
				}
			}

			property ParamDesc<bool>^ EmitterEnabledDesc
			{
				inline ParamDesc<bool> ^get()
				{
					ParamDesc<bool> ^res = gcnew ParamDesc<bool>();
					res->Unit = "";
					res->Description = "Emitter is enabled";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
					return res;
				}
			}

			static array<String^, 1>^ GetSerialNumbersOfAttachedCameras();

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


		private:
			FloatCameraImage^ CalcZImage();
			ColorCameraImage^ CalcColor();
			Point3fCameraImage^ CalcPoint3fImage();
			FloatCameraImage^ CalcIRImage();

			static bool OpenNIInit();
			static bool OpenNIShutdown();
			static void LogOpenNIError(String^ status);
			static int openNIInitCounter = 0;

			bool emitterEnabled = true;
			OrbbecNativeCameraData* camData;
			static MetriLog^ log = gcnew MetriLog();

			// for converting managed strings to const char*
			msclr::interop::marshal_context oMarshalContext;
		};
	}
}