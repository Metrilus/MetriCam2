#include "stdafx.h"
#include "MvBlueSirius.h"
#include <memory.h>

using namespace System;
using namespace System::Drawing;
using namespace System::Drawing::Imaging;
using namespace MetriCam2;

namespace MetriCam2
{
	namespace Cameras
	{
		MvBlueSirius::MvBlueSirius()
			: Camera("mvBlueSirius"), _focalLength(0.0f), _updateLock(gcnew Object())
		{
			int major = 0;
			int minor = 0;
			int patch = 0;
			const char* versionString = MV6D_GetBuildVersion(&major, &minor, &patch);
			log->DebugFormat("mv6D - {0}.{1}.{2} - Build \"{3}\"", major, minor, patch, gcnew String(versionString));

			_currentMasterImage = nullptr;
			_currentSlaveImage = nullptr;
			_currentZImageMapped = nullptr;
			_currentZImage = nullptr;
			_currentDistanceImage = nullptr;
			_currentDistanceImageMapped = nullptr;
			_currentPointCloud = nullptr;
			_currentPointCloudMapped = nullptr;

			_master = gcnew ImageData;
			_slave = gcnew ImageData;
			_color = gcnew ImageData;
			_depthMapped = gcnew ImageData;
			_depthRaw = gcnew ImageData;
		}

		MvBlueSirius::~MvBlueSirius()
		{
			Disconnect(false);
		}

		void MvBlueSirius::LoadAllAvailableChannels()
		{
			log->EnterMethod();

			ChannelRegistry^ cr = ChannelRegistry::Instance;
			Channels->Clear();
			Channels->Add(cr->RegisterChannel(ChannelNames::Color));
			Channels->Add(cr->RegisterChannel(ChannelNames::Distance));
			Channels->Add(cr->RegisterChannel(ChannelNames::ZImage));
			Channels->Add(cr->RegisterChannel(ChannelNames::Point3DImage));
			Channels->Add(cr->RegisterCustomChannel(ChannelNames::Left, FloatImage::typeid));
			Channels->Add(cr->RegisterCustomChannel(ChannelNames::Right, FloatImage::typeid));
			Channels->Add(cr->RegisterCustomChannel((String^)CustomChannelNames::ZMapped, FloatImage::typeid));
			Channels->Add(cr->RegisterCustomChannel((String^)CustomChannelNames::DistanceMapped, FloatImage::typeid));
			Channels->Add(cr->RegisterCustomChannel((String^)CustomChannelNames::PointCloudMapped, Point3fImage::typeid));
		}

		void MvBlueSirius::ConnectImpl()
		{
			log->EnterMethod();

			MV6D_Handle camHandle;
			MV6D_ResultCode result = MV6D_Create(&camHandle, MV6D_ANY_GPU);
			CheckResult(result, ConnectionFailedException::typeid, 1);
			_h6D = camHandle;

			// Update list of supported cameras
			int deviceCount = 0;
			result = MV6D_DeviceListUpdate(_h6D, &deviceCount);
			CheckResult(result, ConnectionFailedException::typeid, 2);
			char tmpSerial[128] = { 0 };

			// find a perception camera that's not in use
			for (int index = 0; index < deviceCount; ++index)
			{
				int inUse = 1;
				result = MV6D_DeviceListGetSerial(_h6D, tmpSerial, sizeof(tmpSerial), &inUse, index);
				CheckResult(result, ConnectionFailedException::typeid, 3);
				
				if (!inUse && !IsNullOrWhiteSpace(tmpSerial))
				{
					char* CSerial = GCStrToCStr(this->SerialNumber);
					if (IsNullOrWhiteSpace(CSerial) || CSerial == tmpSerial)
					{
						this->SerialNumber = gcnew String(tmpSerial);
						if (!IsNullOrWhiteSpace(CSerial))
						{
							delete CSerial;
						}
						break;
					}
				}
			}

			CheckSerial(tmpSerial);

			// open the device
			result = MV6D_DeviceOpen(_h6D, tmpSerial);
			CheckResult(result, ConnectionFailedException::typeid, 5);

			// configure the stereo algorithm
			int filterSet = fsAllround;
			result = MV6D_SetDepthPreset(_h6D, daFilterSet, filterSet);
			CheckResult(result, ConnectionFailedException::typeid, 6);

			int minimumDistance = minDist800mm;
			result = MV6D_SetDepthPreset(_h6D, daMinimumDistance, minimumDistance);
			CheckResult(result, ConnectionFailedException::typeid, 7);

			// get framerate property
			MV6D_Property framerateProperty;
			result = MV6D_PropertyGet(_h6D, MV6D_PROPERTY_FRAMERATE, &framerateProperty);
			CheckResult(result, ConnectionFailedException::typeid, 8);

			// set framerate
			double framerate = 25.0;
			result = MV6D_PropertyWrite(_h6D, framerateProperty, &framerate, sizeof(framerate));
			CheckResult(result, ConnectionFailedException::typeid, 9);

			result = MV6D_SetDepthPreset(_h6D, daStereoAlgorithm, MV6D_Stereo_Algorithm::stereoAlgoRSGM);
			CheckResult(result, ConnectionFailedException::typeid, 10);

			// start capturing from the device
			result = MV6D_DeviceStart(_h6D);
			CheckResult(result, ConnectionFailedException::typeid, 11);

			// Check calibration

			// request buffer pointers
			MV6D_RequestBuffer* currentRequestBuffer = nullptr;
			MV6D_RequestBuffer* lastRequestBuffer = nullptr;

			AutoExposure = true;

			ActivateChannel(ChannelNames::Color);
			ActivateChannel(ChannelNames::Distance);
			ActivateChannel(ChannelNames::ZImage);
			ActivateChannel(ChannelNames::Point3DImage);
			ActivateChannel((String^)CustomChannelNames::ZMapped);
			ActivateChannel((String^)CustomChannelNames::DistanceMapped);
			ActivateChannel((String^)CustomChannelNames::PointCloudMapped);
			ActivateChannel(ChannelNames::Left);
			ActivateChannel(ChannelNames::Right);
			SelectChannel(ChannelNames::Color);
		}

		void MvBlueSirius::DisconnectImpl()
		{
			log->EnterMethod();

			MV6D_ResultCode result = MV6D_DeviceClose(_h6D);
			CheckResult(result, InvalidOperationException::typeid, 12);
			result = MV6D_Close(_h6D);
			CheckResult(result, InvalidOperationException::typeid, 13);

			System::Threading::Monitor::Enter(_updateLock);
			_master->FreeData();
			_slave->FreeData();
			_color->FreeData();
			_depthMapped->FreeData();
			_depthRaw->FreeData();
			System::Threading::Monitor::Exit(_updateLock);
		}

		void MvBlueSirius::UpdateImpl()
		{
			// request buffer pointer
			MV6D_RequestBuffer* requestBuffer = nullptr;

			// wait up to two seconds
			int timeout = 20000;

			// dropped frames since last call
			int dropped = 0;

			// request a new buffer object
			if (MV6D_DeviceResultWaitFor(_h6D, &requestBuffer, &dropped, timeout) != rcOk)
			{
				throw gcnew MetriCam2::Exceptions::ImageAcquisitionFailedException("No image data received in time");
			}

			System::Threading::Monitor::Enter(_updateLock);

			// get color image
			if (requestBuffer->colorMapped.pData)
			{
				_color->Width = requestBuffer->colorMapped.iWidth;
				_color->Height = requestBuffer->colorMapped.iHeight;
				_color->CopyColorData(requestBuffer->colorMapped);
			}

			// get master raw image
			if (requestBuffer->rawMaster.pData)
			{
				_currentMasterImage = nullptr;
				_master->Width = requestBuffer->rawMaster.iWidth;
				_master->Height = requestBuffer->rawMaster.iHeight;
				_master->CopyGrayData(requestBuffer->rawMaster);
			}

			// get slave raw image
			if (requestBuffer->rawSlave1.pData)
			{
				_currentSlaveImage = nullptr;
				_slave->Width = requestBuffer->rawSlave1.iWidth;
				_slave->Height = requestBuffer->rawSlave1.iHeight;
				_slave->CopyGrayData(requestBuffer->rawSlave1);
			}

			// get depth mapped image
			if (requestBuffer->depthMapped.pData)
			{
				_currentZImageMapped = nullptr;
				_depthMapped->Width = requestBuffer->depthMapped.iWidth;
				_depthMapped->Height = requestBuffer->depthMapped.iHeight;
				_depthMapped->CopyDepthData(requestBuffer->depthMapped);
			}

			// get depth raw image
			if (requestBuffer->depthRaw.pData)
			{
				_currentDistanceImage = nullptr;
				_currentDistanceImageMapped = nullptr;
				_currentPointCloud = nullptr;
				_currentPointCloudMapped = nullptr;
				_currentZImage = nullptr;

				_depthRaw->Width = requestBuffer->depthRaw.iWidth;
				_depthRaw->Height= requestBuffer->depthRaw.iHeight;
				_depthRaw->CopyDepthData(requestBuffer->depthRaw);
			}

			_focalLength = (float)requestBuffer->focalLength;

			System::Threading::Monitor::Exit(_updateLock);

			// unlock request buffer
			MV6D_ResultCode result = MV6D_UnlockRequest(_h6D, requestBuffer);
			CheckResult(result, InvalidOperationException::typeid, 14);
		}

		CameraImage^ MvBlueSirius::CalcChannelImpl(String^ channelName)
		{
			if (ChannelNames::Color == channelName)
			{
				return CalcColorImage(_color);
			}
			if (ChannelNames::Left == channelName)
			{
				if (nullptr == _currentMasterImage)
				{
					_currentMasterImage = CalcGreyImage(_master);
				}
				return gcnew FloatImage(_currentMasterImage);
			}
			if (ChannelNames::Right == channelName)
			{
				if (nullptr == _currentSlaveImage)
				{
					_currentSlaveImage = CalcGreyImage(_slave);
				}
				return gcnew FloatImage(_currentSlaveImage);
			}
			if (CustomChannelNames::ZMapped == channelName)
			{
				if (nullptr == _currentZImageMapped)
				{
					_currentZImageMapped = CalcDepthImage(_depthMapped);
				}
				return gcnew FloatImage(_currentZImageMapped);
			}
			if (ChannelNames::ZImage == channelName)
			{
				if (nullptr == _currentZImage)
				{
					_currentZImage = CalcDepthImage(_depthRaw);
				}
				return gcnew FloatImage(_currentZImage);
			}
			if (CustomChannelNames::PointCloudMapped == channelName)
			{
				if (nullptr == _currentPointCloudMapped)
				{
					FloatImage^ zImageMapped = (FloatImage^)CalcChannelImpl((System::String^)CustomChannelNames::ZMapped);
					_currentPointCloudMapped = CalcPointCloud(zImageMapped);
				}
				return gcnew Point3fImage(_currentPointCloudMapped);
			}
			if (CustomChannelNames::DistanceMapped == channelName)
			{
				if (nullptr == _currentDistanceImageMapped)
				{
					Point3fImage^ pts3DMapped = (Point3fImage^)CalcChannelImpl((System::String^)CustomChannelNames::PointCloudMapped);
					_currentDistanceImageMapped = CalcDistances(pts3DMapped);
				}
				return gcnew FloatImage(_currentDistanceImageMapped);
			}
			if (ChannelNames::Distance == channelName)
			{
				if (nullptr == _currentDistanceImage)
				{
					Point3fImage^ pts3D = (Point3fImage^)CalcChannelImpl(ChannelNames::Point3DImage);
					_currentDistanceImage = CalcDistances(pts3D);
				}
				return gcnew FloatImage(_currentDistanceImage);
			}
			if (ChannelNames::Point3DImage == channelName)
			{
				if (nullptr == _currentPointCloud)
				{
					FloatImage^ zImage = (FloatImage^)CalcChannelImpl(ChannelNames::ZImage);
					_currentPointCloud = CalcPointCloud(zImage);
				}
				return gcnew Point3fImage(_currentPointCloud);
			}

			// this should not happen, because Camera checks if the channel is active.
			return nullptr;
		}

		IProjectiveTransformation^ MvBlueSirius::GetIntrinsics(String^ channelName)
		{
			if (MetriCam2::ChannelNames::Distance == channelName
				|| MetriCam2::ChannelNames::ZImage == channelName)
			{
				return gcnew ProjectiveTransformationRational(_depthRaw->Width, _depthRaw->Height, FocalLength, FocalLength, _depthRaw->Width / 2.0f, _depthRaw->Height / 2.0f, 0, 0, 0, 0, 0, 0, 0, 0, float::NaN);
			}
			if (MetriCam2::ChannelNames::Color == channelName)
			{
				return gcnew ProjectiveTransformationRational(_color->Width, _color->Height, FocalLength, FocalLength, _color->Width / 2.0f, _color->Height / 2.0f, 0, 0, 0, 0, 0, 0, 0, 0, float::NaN);
			}
			return Camera::GetIntrinsics(channelName);
		}

		ColorImage ^ MvBlueSirius::CalcColorImage(ImageData^ image)
		{
			System::Threading::Monitor::Enter(_updateLock);

			ColorImage^ cImage = gcnew ColorImage(image->Width, image->Height);
			BitmapData^ bitmapData = cImage->Data->LockBits(System::Drawing::Rectangle(0, 0, image->Width, image->Height), ImageLockMode::WriteOnly, cImage->Data->PixelFormat);
			int colorIdx = 0;
			for (unsigned int y = 0; y < image->Height; y++)
			{
				unsigned char* linePtr = (unsigned char*)(bitmapData->Scan0.ToPointer()) + bitmapData->Stride * y;
				for (unsigned int x = 0; x < image->Width; x++)
				{
					*linePtr++ = image->Data[colorIdx++];
					*linePtr++ = image->Data[colorIdx++];
					*linePtr++ = image->Data[colorIdx++];
					*linePtr++ = 255;
				}
			}
			cImage->Data->UnlockBits(bitmapData);

			System::Threading::Monitor::Exit(_updateLock);

			return cImage;
		}

		FloatImage ^ MvBlueSirius::CalcGreyImage(ImageData^ image)
		{
			System::Threading::Monitor::Enter(_updateLock);

			FloatImage^ fImage = gcnew FloatImage(image->Width, image->Height);
			int i = 0;
			for (unsigned int y = 0; y < image->Height; y++)
			{
				for (unsigned int x = 0; x < image->Width; x++)
				{
					fImage[y, x] = image->Data[i++];
				}
			}

			System::Threading::Monitor::Exit(_updateLock);

			return fImage;
		}

		FloatImage ^ MvBlueSirius::CalcDepthImage(ImageData^ image)
		{
			System::Threading::Monitor::Enter(_updateLock);

			FloatImage^ fImage = gcnew FloatImage(image->Width, image->Height);
			float* imageData = (float*)image->Data;
			int i = 0;
			for (unsigned int y = 0; y < image->Height; y++)
			{
				for (unsigned int x = 0; x < image->Width; x++)
				{
					fImage[y, x] = imageData[i++];
				}
			}

			System::Threading::Monitor::Exit(_updateLock);

			return fImage;
		}

		FloatImage ^ MvBlueSirius::CalcDistances(Point3fImage^ image)
		{
			System::Threading::Monitor::Enter(_updateLock);

			FloatImage^ fImage = gcnew FloatImage(image->Width, image->Height);
			int i = 0;
			for (int y = 0; y < image->Height; y++)
			{
				for (int x = 0; x < image->Width; x++)
				{
					fImage[y, x] = image[y, x].GetLength();
				}
			}

			System::Threading::Monitor::Exit(_updateLock);

			return fImage;
		}

		Point3fImage ^ MvBlueSirius::CalcPointCloud(FloatImage^ depthImage)
		{
			System::Threading::Monitor::Enter(_updateLock);

			int depthWidth = depthImage->Width;
			int depthHeight = depthImage->Height;

			Point3fImage^ PointCloud = gcnew Point3fImage(depthWidth, depthHeight);

			int halfWidth = depthWidth / 2;
			int halfHeight = depthHeight / 2;

			for (int y = 0; y < depthHeight; y++)
			{
				for (int x = 0; x < depthWidth; x++)
				{
					float wz = depthImage[y, x];
					if (wz <= 0)
					{
						continue;
					}

					float wx = ((x - halfWidth) / FocalLength) * wz;
					float wy = ((y - halfHeight) / FocalLength) * wz;
					PointCloud[y, x] = Point3f(wx, wy, wz);
				}
			}

			System::Threading::Monitor::Exit(_updateLock);

			return PointCloud;
		}
	}
}