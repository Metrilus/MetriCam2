#pragma once
#include <msclr/marshal.h>
#include "mv6D.h"

using namespace MetriCam2;
using namespace MetriCam2::Exceptions;
using namespace Metrilus::Util;
using namespace System;
using namespace System::Runtime::InteropServices;

namespace MetriCam2
{
	namespace Cameras
	{
		public ref class MvBlueSirius : Camera
		{
		public:
			/// <summary>
			/// Defines the custom channel names for easier handling.
			/// </summary>
			/// <remarks>Similar to MetriCam2.ChannelNames for standard channel names.</remarks>
			ref class CustomChannelNames
			{
				public:
					// Depth buffer.
					// Pixel mapped (color, flow, depth).
					static const String^ DepthMapped = "DepthMapped";

					// Raw depth buffer.
					// Not pixel mapped.
					// The raw depth buffer is not mapped with the color nor
					// the flow buffer. Therefore it holds more depth information
					// as the mapped depth buffer.
					static const String^ DepthRaw = "DepthRaw";

					// Distance image computed from DepthMapped data
					static const String^ DistanceMapped = "DistanceMapped";

					// Point cloud computed from DepthMapped data
					static const String^ PointCloudMapped = "PointCloudMapped";
			};

		private:
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

				void CopyColorData(MV6D_ColorBuffer& colorBuffer)
				{
					unsigned int numElements = this->Width * this->Height;
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

			private:
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
			};

			MV6D_Handle _h6D;
			System::Object^ _updateLock;
			float _focalLength;

			ImageData^ _master;
			ImageData^ _slave;
			ImageData^ _color;
			ImageData^ _depthMapped;
			ImageData^ _depthRaw;

			FloatCameraImage^ _currentMasterImage; // caches computed FloatCameraImage
			FloatCameraImage^ _currentSlaveImage; // caches computed FloatCameraImage
			ColorCameraImage^ _currentColorImage; // caches computed bitmap
			FloatCameraImage^ _currentDepthMappedImage; // caches computed FloatCameraImage
			FloatCameraImage^ _currentDepthRawImage; // caches computed FloatCameraImage
			FloatCameraImage^ _currentDistanceImage; // caches computed FloatCameraImage
			FloatCameraImage^ _currentDistanceImageMapped; // caches computed FloatCameraImage
			Point3fCameraImage^ _currentPointCloud; // caches computed Point3fCameraImage
			Point3fCameraImage^ _currentPointCloudMapped; // caches computed Point3fCameraImage

			property ParamDesc<bool>^ AutoExposureDesc
			{
				inline ParamDesc<bool> ^get()
				{
					ParamDesc<bool> ^res = gcnew ParamDesc<bool>();
					res->Unit = "";
					res->Description = "Auto Exposure enabled";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			property RangeParamDesc<float>^ ExposureDesc
			{
				inline RangeParamDesc<float> ^get()
				{
					MV6D_Property exposureProperty;
					MV6D_ResultCode	result = MV6D_PropertyGet(_h6D, MV6D_PROPERTY_CAMERA_CONTROL_EXPOSURE, &exposureProperty);
					CheckResult(result, InvalidOperationException::typeid, 1007);

					int hasMinimum = 0;
					float minimum = 0;
					int minimumSize = 0;
					result = MV6D_PropertyGetMinimum(_h6D, exposureProperty, &hasMinimum, &minimum, &minimumSize);
					CheckResult(result, InvalidOperationException::typeid, 1008);

					int hasMaximum = 0;
					float maximum = 0;
					int maximumSize = 0;
					result = MV6D_PropertyGetMaximum(_h6D, exposureProperty, &hasMaximum, &maximum, &maximumSize);
					CheckResult(result, InvalidOperationException::typeid, 1009);

					if (hasMinimum == 0 || hasMaximum == 0)
					{
						throw gcnew Exception("Property Exposure does not have maximum or minimum");
					}

					RangeParamDesc<float> ^res = gcnew RangeParamDesc<float>(minimum, maximum);
					res->Unit = "";
					res->Description = "Exposure time in [?]";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			property RangeParamDesc<float>^ GainDesc
			{
				inline RangeParamDesc<float> ^get()
				{
					MV6D_Property gainProperty;
					MV6D_ResultCode	result = MV6D_PropertyGet(_h6D, MV6D_PROPERTY_CAMERA_CONTROL_ANALOG_GAIN, &gainProperty);
					CheckResult(result, InvalidOperationException::typeid, 1014);

					int hasMinimum = 0;
					float minimum = 0;
					int minimumSize = 0;
					result = MV6D_PropertyGetMinimum(_h6D, gainProperty, &hasMinimum, &minimum, &minimumSize);
					CheckResult(result, InvalidOperationException::typeid, 1015);

					int hasMaximum = 0;
					float maximum = 0;
					int maximumSize = 0;
					result = MV6D_PropertyGetMaximum(_h6D, gainProperty, &hasMaximum, &maximum, &maximumSize);
					CheckResult(result, InvalidOperationException::typeid, 1016);

					if (hasMinimum == 0 || hasMaximum == 0)
					{
						throw gcnew Exception("Property Gain does not have maximum or minimum");
					}

					RangeParamDesc<float> ^res = gcnew RangeParamDesc<float>(minimum, maximum);
					res->Unit = "";
					res->Description = "Gain";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			property RangeParamDesc<float>^ GainColorDesc
			{
				inline RangeParamDesc<float> ^get()
				{
					MV6D_Property gainColorProperty;
					MV6D_ResultCode	result = MV6D_PropertyGet(_h6D, MV6D_PROPERTY_CAMERA_CONTROL_ANALOG_GAIN_COLOR, &gainColorProperty);
					CheckResult(result, InvalidOperationException::typeid, 1020);

					int hasMinimum = 0;
					float minimum = 0;
					int minimumSize = 0;
					result = MV6D_PropertyGetMinimum(_h6D, gainColorProperty, &hasMinimum, &minimum, &minimumSize);
					CheckResult(result, InvalidOperationException::typeid, 1021);

					int hasMaximum = 0;
					float maximum = 0;
					int maximumSize = 0;
					result = MV6D_PropertyGetMaximum(_h6D, gainColorProperty, &hasMaximum, &maximum, &maximumSize);
					CheckResult(result, InvalidOperationException::typeid, 1022);

					if (hasMinimum == 0 || hasMaximum == 0)
					{
						throw gcnew Exception("Property GainColor does not have maximum or minimum");
					}

					RangeParamDesc<float> ^res = gcnew RangeParamDesc<float>(minimum, maximum);
					res->Unit = "";
					res->Description = "Gain";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected;
					return res;
				}
			}

			property ParamDesc<float>^ FocalLengthDesc
			{
				inline ParamDesc<float> ^get()
				{
					ParamDesc<float> ^res = gcnew ParamDesc<float>();
					res->Unit = "";
					res->Description = "Focal Length";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
					return res;
				}
			}

		public:
			MvBlueSirius();
			~MvBlueSirius();

			static Point3fCameraImage^ DepthImageToPointCloud(FloatCameraImage^ depthImage, float focalLength);
			virtual IProjectiveTransformation^ GetIntrinsics(String^ channelName) override;

			property System::String^ Vendor
			{
				System::String^ get() override
				{
					return "Matrix Vision";
				}
			}

			property System::String^ Model
			{
				System::String^ get() override
				{
					return "mvBlueSirius";
				}
			}

#if !NETSTANDARD2_0
			property System::Drawing::Icon^ CameraIcon
			{
				System::Drawing::Icon^ get() override
				{
					System::Reflection::Assembly^ assembly = System::Reflection::Assembly::GetExecutingAssembly();
					System::IO::Stream^ iconStream = assembly->GetManifestResourceStream("MatrixVisionIcon.ico");
					return gcnew System::Drawing::Icon(iconStream);
				}
			}
#endif

		////////////////////////////////////////
		//// CAMERA PARAMETERS
		////////////////////////////////////////

		property bool AutoExposure
		{
			void set(bool value)
			{
				MV6D_Property myAutoProperty;
				int regValue = value ? 1 : 0;
				MV6D_ResultCode	result = MV6D_PropertyGet(_h6D, MV6D_PROPERTY_CAMERA_CONTROL_AUTO, &myAutoProperty);
				CheckResult(result, InvalidOperationException::typeid, 1001);
				result = MV6D_PropertyWrite(_h6D, myAutoProperty, &regValue, sizeof(regValue));
				CheckResult(result, InvalidOperationException::typeid, 1002);
			}
		}
		
		//! Gets/Sets the exposure time in [?].
		property float Exposure
		{
			float get(void)
			{
				double exposure;
				int size = sizeof(exposure);

				MV6D_Property exposureProperty;
				MV6D_ResultCode	result = MV6D_PropertyGet(_h6D, MV6D_PROPERTY_CAMERA_CONTROL_EXPOSURE, &exposureProperty);
				CheckResult(result, InvalidOperationException::typeid, 1003);

				result = MV6D_PropertyRead(_h6D, exposureProperty, &exposure, &size);
				CheckResult(result, InvalidOperationException::typeid, 1004);
				return (float)exposure;
			}
			void set(float value)
			{
				double exposure = value;
				int size = sizeof(exposure);

				MV6D_Property myExposureProperty;
				MV6D_ResultCode	result = MV6D_PropertyGet(_h6D, MV6D_PROPERTY_CAMERA_CONTROL_EXPOSURE, &myExposureProperty);
				CheckResult(result, InvalidOperationException::typeid, 1005);
				result = MV6D_PropertyWrite(_h6D, myExposureProperty, &exposure, size);
				CheckResult(result, InvalidOperationException::typeid, 1006);
			}
		}
		
		//! Gets/Sets the gain.
		property float Gain
		{
			float get(void)
			{
				double gain;
				int size = sizeof(gain);

				MV6D_Property gainProperty;
				MV6D_ResultCode	result = MV6D_PropertyGet(_h6D, MV6D_PROPERTY_CAMERA_CONTROL_ANALOG_GAIN, &gainProperty);
				CheckResult(result, InvalidOperationException::typeid, 1010);

				result = MV6D_PropertyRead(_h6D, gainProperty, &gain, &size);
				CheckResult(result, InvalidOperationException::typeid, 1011);
				return (float)gain;
			}
			void set(float value)
			{
				double gain = value;
				int size = sizeof(gain);

				MV6D_Property myGainProperty;
				MV6D_ResultCode	result = MV6D_PropertyGet(_h6D, MV6D_PROPERTY_CAMERA_CONTROL_ANALOG_GAIN, &myGainProperty);
				CheckResult(result, InvalidOperationException::typeid, 1012);
				result = MV6D_PropertyWrite(_h6D, myGainProperty, &gain, size);
				CheckResult(result, InvalidOperationException::typeid, 1013);
			}
		}
		
		property float GainColor
		{
			float get(void)
			{
				double gain;
				int size = sizeof(gain);

				MV6D_Property gainProperty;
				MV6D_ResultCode	result = MV6D_PropertyGet(_h6D, MV6D_PROPERTY_CAMERA_CONTROL_ANALOG_GAIN_COLOR, &gainProperty);
				CheckResult(result, InvalidOperationException::typeid, 1017);

				result = MV6D_PropertyRead(_h6D, gainProperty, &gain, &size);
				CheckResult(result, InvalidOperationException::typeid, 1018);
				return (float)gain;
			}
			void set(float value)
			{
				double gain = value;
				int size = sizeof(gain);

				MV6D_Property myGainProperty;
				MV6D_ResultCode	result = MV6D_PropertyGet(_h6D, MV6D_PROPERTY_CAMERA_CONTROL_ANALOG_GAIN_COLOR, &myGainProperty);
				CheckResult(result, InvalidOperationException::typeid, 1018);
				result = MV6D_PropertyWrite(_h6D, myGainProperty, &gain, size);
				CheckResult(result, InvalidOperationException::typeid, 1019);
			}
		}

		property float FocalLength
		{
			inline float get()
			{
				return _focalLength;
			}
		}

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
			inline bool IsNullOrWhiteSpace(char* str)
			{
				if (nullptr == str || 0 == strcmp("", str))
				{
					return true;
				}

				return false;
			}
			inline char* GCStrToCStr(System::String^ str)
			{
				IntPtr ptrToNativeString = Marshal::StringToHGlobalAnsi(str);
				return static_cast<char*>(ptrToNativeString.ToPointer());
			}
			inline void CheckSerial(char* serial)
			{
				if (IsNullOrWhiteSpace(serial))
				{
					throw ExceptionBuilder::BuildFromID(ConnectionFailedException::typeid, this, 4, "No available mv6D camera found");
				}
			}

		};

	}
}