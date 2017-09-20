#pragma once
#include <msclr/marshal.h>
#include "mv6D.h"

using namespace MetriCam2;
using namespace MetriCam2::Exceptions;
using namespace Metrilus::Util;
using namespace System;

namespace MetriCam2
{
	namespace Cameras
	{
		public ref class MvBlueSirius : Camera
		{
		public:
		public:
			/// <summary>
			/// Defines the custom channel names for easier handling.
			/// </summary>
			/// <remarks>Similar to MetriCam2.ChannelNames for standard channel names.</remarks>
			static ref class CustomChannelNames
			{
			public:
				static const String^ DepthMapped = "DepthMapped";
				static const String^ DepthRaw = "DepthRaw";
				static const String^ DistanceMapped = "DistanceMapped";
				static const String^ PointCloudMapped = "PointCloudMapped";
			};

		private:
			MV6D_Handle h6D;
			System::Object^ updateLock;
			String^ ipAddress;

			float focalLength;

			unsigned char* pRawColorData;
			unsigned int colorBufferSizeInBytes;
			unsigned int colorWidth;
			unsigned int colorHeight;
			ColorCameraImage^ currentColorImage; // caches computed bitmap

			float* pRawDepthMappedData;
			unsigned int depthMappedBufferNumElements;
			unsigned int depthMappedWidth;
			unsigned int depthMappedHeight;
			FloatCameraImage^ currentDepthMappedImage; // caches computed FloatCameraImage

			float* pRawDepthRawData;
			unsigned int depthRawBufferNumElements;
			unsigned int depthRawWidth;
			unsigned int depthRawHeight;
			FloatCameraImage^ currentDepthRawImage; // caches computed FloatCameraImage

			FloatCameraImage^ currentDistanceImage; // caches computed FloatCameraImage
			FloatCameraImage^ currentDistanceImageMapped; // caches computed FloatCameraImage
			Point3fCameraImage^ currentPointCloud; // caches computed Point3fCameraImage
			Point3fCameraImage^ currentPointCloudMapped; // caches computed Point3fCameraImage

			msclr::interop::marshal_context oMarshalContext;

		public:
			MvBlueSirius();
			~MvBlueSirius();

			static Point3fCameraImage^ DepthImageToPointCloud(FloatCameraImage^ depthImage, float focalLength);
			virtual IProjectiveTransformation^ GetIntrinsics(String^ channelName) override;

			////////////////////////////////////////
			//// CAMERA PARAMETERS

		public: property bool AutoExposure
		{
			void set(bool value)
			{
				MV6D_Property myAutoProperty;
				int regValue = value ? 1 : 0;
				MV6D_ResultCode	result = MV6D_PropertyGet(h6D, MV6D_PROPERTY_CAMERA_CONTROL_AUTO, &myAutoProperty);
				result = MV6D_PropertyWrite(h6D, myAutoProperty, &regValue, sizeof(regValue));
			}
		}
				//! Gets/Sets the exposure time in [?].
		public: property float Exposure
		{
			float get(void)
			{
				double exposure;
				int size = sizeof(exposure);

				MV6D_Property exposureProperty;
				MV6D_ResultCode	result = MV6D_PropertyGet(h6D, MV6D_PROPERTY_CAMERA_CONTROL_EXPOSURE, &exposureProperty);

				result = MV6D_PropertyRead(h6D, exposureProperty, &exposure, &size);
				return (float)exposure;
			}
			void set(float value)
			{
				double exposure = value;
				int size = sizeof(exposure);

				MV6D_Property myExposureProperty;
				MV6D_ResultCode	result = MV6D_PropertyGet(h6D, MV6D_PROPERTY_CAMERA_CONTROL_EXPOSURE, &myExposureProperty);
				result = MV6D_PropertyWrite(h6D, myExposureProperty, &exposure, size);
			}
		}
				//! Gets/Sets the gain.
		public: property float Gain
		{
			float get(void)
			{
				double gain;
				int size = sizeof(gain);

				MV6D_Property gainProperty;
				MV6D_ResultCode	result = MV6D_PropertyGet(h6D, MV6D_PROPERTY_CAMERA_CONTROL_ANALOG_GAIN, &gainProperty);

				result = MV6D_PropertyRead(h6D, gainProperty, &gain, &size);
				return (float)gain;
			}
			void set(float value)
			{
				double gain = value;
				int size = sizeof(gain);

				MV6D_Property myGainProperty;
				MV6D_ResultCode	result = MV6D_PropertyGet(h6D, MV6D_PROPERTY_CAMERA_CONTROL_ANALOG_GAIN, &myGainProperty);
				result = MV6D_PropertyWrite(h6D, myGainProperty, &gain, size);
			}
		}
				// TODO: Add GainDesc
		public: property float GainColor
		{
			float get(void)
			{
				double gain;
				int size = sizeof(gain);

				MV6D_Property gainProperty;
				MV6D_ResultCode	result = MV6D_PropertyGet(h6D, MV6D_PROPERTY_CAMERA_CONTROL_ANALOG_GAIN_COLOR, &gainProperty);

				result = MV6D_PropertyRead(h6D, gainProperty, &gain, &size);
				return (float)gain;
			}
			void set(float value)
			{
				double gain = value;
				int size = sizeof(gain);

				MV6D_Property myGainProperty;
				MV6D_ResultCode	result = MV6D_PropertyGet(h6D, MV6D_PROPERTY_CAMERA_CONTROL_ANALOG_GAIN_COLOR, &myGainProperty);
				result = MV6D_PropertyWrite(h6D, myGainProperty, &gain, size);
			}
		}
				// TODO: Add GainColorDesc

		public: property float FocalLength
		{
			inline float get()
			{
				return focalLength;
			}
		}

				////////////////////////////////////////
				//// MetriCam2 Camera Interface

				////////////////////////////////////////
				//// MetriCam2 Camera Interface Methods

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
			virtual CameraImage^ CalcChannelImpl(String^ channelName) override;

		private:
			// Internal helper functions
			ColorCameraImage^ CalcColor();
			FloatCameraImage^ CalcDepthMapped();
			FloatCameraImage^ CalcDepthRaw();
			FloatCameraImage^ CalcDistances();
			FloatCameraImage^ CalcDistancesMapped();
			Point3fCameraImage^ CalcPointCloudFromDepthRaw();
			Point3fCameraImage^ CalcPointCloudFromDepthMapped();
			FloatCameraImage^ CalcDepthRawIR();
			FloatCameraImage^ CalcFlow();
			void CopyColorData(MV6D_ColorBuffer colorBuffer);
			void CopyDepthMappedData(MV6D_DepthBuffer depthBuffer);
			void CopyDepthRawData(MV6D_DepthBuffer depthBuffer);
			void ResizeColorBuffer(int sizeInBytes);
			void ResizeDepthMappedBuffer(int numElements);
			void ResizeDepthRawBuffer(int numElements);
			inline void FreeColorBuffer()
			{
				if (nullptr != pRawColorData)
				{
					delete[] pRawColorData;
				}
				colorBufferSizeInBytes = 0;
				pRawColorData = nullptr;
			};
			inline void FreeDepthMappedBuffer()
			{
				if (nullptr != pRawDepthMappedData)
				{
					delete[] pRawDepthMappedData;
				}
				depthMappedBufferNumElements = 0;
				pRawDepthMappedData = nullptr;
			};
			inline void FreeDepthRawBuffer()
			{
				if (nullptr != pRawDepthRawData)
				{
					delete[] pRawDepthRawData;
				}
				depthRawBufferNumElements = 0;
				pRawDepthRawData = nullptr;
			};
			inline bool CheckResult(MV6D_ResultCode r, Type^ exceptionType, int exceptionID)
			{
				if (r != rcOk)
				{
					String^ msg = String::Format("Failed with message = '{0}'.", gcnew String(MV6D_ResultCodeToString(r)));
					throw ExceptionBuilder::BuildFromID(exceptionType, this, exceptionID, msg);
				}
				return true;
			}
		};

	}
}