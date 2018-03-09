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

			ref struct ImageData {
				unsigned int Width;
				unsigned int Height;
				unsigned int BufferSize;
				unsigned char* Data;

				void FreeData()
				{
					if (nullptr != this->Data)
					{
						delete[] this->Data;
					}
					this->BufferSize = 0;
					this->Data = nullptr;
				}

				void ResizeBuffer(int sizeInBytes)
				{
					if (nullptr != this->Data)
					{
						// Buffer exists
						if (this->BufferSize == sizeInBytes)
						{
							// Buffer has correct size
							return;
						}

						this->FreeData();
					}

					this->Data = new unsigned char[sizeInBytes];
					this->BufferSize = sizeInBytes;
				}

				void CopyColorData(MV6D_ColorBuffer& colorBuffer)
				{
					int numElements = this->Width * this->Height;
					int sizeInBytes = numElements * sizeof(char) * 3;
					ResizeBuffer(sizeInBytes);

					for (unsigned int i = 0; i < numElements; i++)
					{
						this->Data[(3 * i) + 0] = colorBuffer.pData[i].b;
						this->Data[(3 * i) + 1] = colorBuffer.pData[i].g;
						this->Data[(3 * i) + 2] = colorBuffer.pData[i].r;
					}
				}

				void CopyDepthData(MV6D_DepthBuffer& depthBuffer)
				{
					int numElements = this->Width * this->Height;
					int sizeInBytes = numElements * sizeof(float);
					ResizeBuffer(sizeInBytes);
					memcpy_s((void*)this->Data, sizeInBytes, (void*)depthBuffer.pData, sizeInBytes);
				}

				void CopyGrayData(MV6D_GrayBuffer& buffer)
				{
					int numElements = this->Width * this->Height;
					int sizeInBytes = numElements * sizeof(char);
					ResizeBuffer(sizeInBytes);
					memcpy_s((void*)this->Data, sizeInBytes, (void*)buffer.pData, sizeInBytes);
				}
			};

			float _focalLength;

			ImageData^ _Master;
			ImageData^ _Slave;
			ImageData^ _Color;
			ImageData^ _DepthMapped;
			ImageData^ _DepthRaw;

			FloatCameraImage^ _currentMasterImage; // caches computed FloatCameraImage
			FloatCameraImage^ _currentSlaveImage; // caches computed FloatCameraImage
			ColorCameraImage^ _currentColorImage; // caches computed bitmap
			FloatCameraImage^ _currentDepthMappedImage; // caches computed FloatCameraImage
			FloatCameraImage^ _currentDepthRawImage; // caches computed FloatCameraImage
			FloatCameraImage^ _currentDistanceImage; // caches computed FloatCameraImage
			FloatCameraImage^ _currentDistanceImageMapped; // caches computed FloatCameraImage
			Point3fCameraImage^ _currentPointCloud; // caches computed Point3fCameraImage
			Point3fCameraImage^ _currentPointCloudMapped; // caches computed Point3fCameraImage

			msclr::interop::marshal_context _oMarshalContext;

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
				return _focalLength;
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
			ColorCameraImage^ CalcColorImage(ImageData^ image);
			FloatCameraImage^ CalcFloatImage(ImageData^ image);
			FloatCameraImage^ CalcDistances(Point3fCameraImage^ image);
			Point3fCameraImage^ CalcPointCloud(FloatCameraImage^ depthImage);
			inline bool CheckResult(MV6D_ResultCode r, Type^ exceptionType, int exceptionID)
			{
				if (r != rcOk)
				{
					if (r == rcLaserCritical)
					{
						log->Warn("Laser status critical.");
						return true;
					}

					String^ msg = String::Format("Failed with message = '{0}'.", gcnew String(MV6D_ResultCodeToString(r)));
					throw ExceptionBuilder::BuildFromID(exceptionType, this, exceptionID, msg);
				}
				return true;
			}
		};

	}
}