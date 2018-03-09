#include "stdafx.h"
#include "MvBlueSirius.h"
#include <memory.h>

using namespace System;
using namespace System::Drawing;
using namespace System::Drawing::Imaging;

namespace MetriCam2
{
	namespace Cameras
	{
		MvBlueSirius::MvBlueSirius()
			: _focalLength(0.0f), updateLock(gcnew Object())
		{
			modelName = "mvBlueSirius";

			int major = 0;
			int minor = 0;
			int patch = 0;
			const char* versionString = MV6D_GetBuildVersion(&major, &minor, &patch);
			log->InfoFormat("mv6D - {0}.{1}.{2} - Build \"{3}\"", major, minor, patch, gcnew String(versionString));

			_currentColorImage = nullptr;
			_currentDepthMappedImage = nullptr;
			_currentDepthRawImage = nullptr;

			_currentMasterImage = nullptr;
			_currentSlaveImage = nullptr;
			_currentColorImage = nullptr;
			_currentDepthMappedImage = nullptr;
			_currentDepthRawImage = nullptr;
			_currentDistanceImage = nullptr;
			_currentDistanceImageMapped = nullptr;
			_currentPointCloud = nullptr;
			_currentPointCloudMapped = nullptr;

			_Master = gcnew ImageData;
			_Slave = gcnew ImageData;
			_Color = gcnew ImageData;
			_DepthMapped = gcnew ImageData;
			_DepthRaw = gcnew ImageData;
		}

		MvBlueSirius::~MvBlueSirius()
		{
			/* empty */
		}

		void MvBlueSirius::LoadAllAvailableChannels()
		{
			log->EnterMethod();

			ChannelRegistry^ cr = ChannelRegistry::Instance;
			Channels->Clear();
			Channels->Add(cr->RegisterChannel(ChannelNames::Color));
			Channels->Add(cr->RegisterChannel(ChannelNames::Distance));
			Channels->Add(cr->RegisterCustomChannel(ChannelNames::Left, FloatCameraImage::typeid));
			Channels->Add(cr->RegisterCustomChannel(ChannelNames::Right, FloatCameraImage::typeid));
			Channels->Add(cr->RegisterCustomChannel((String^)CustomChannelNames::DepthMapped, FloatCameraImage::typeid));
			Channels->Add(cr->RegisterCustomChannel((String^)CustomChannelNames::DepthRaw, FloatCameraImage::typeid));
			Channels->Add(cr->RegisterCustomChannel((String^)CustomChannelNames::DistanceMapped, FloatCameraImage::typeid));
			Channels->Add(cr->RegisterCustomChannel((String^)CustomChannelNames::PointCloudMapped, Point3fCameraImage::typeid));
			// TODO: add other channels as well
		}

		void MvBlueSirius::ConnectImpl()
		{
			MV6D_Handle h6D_myass;
			MV6D_ResultCode result = MV6D_Create(&h6D_myass, MV6D_ANY_GPU);
			CheckResult(result, ConnectionFailedException::typeid, 1);
			h6D = h6D_myass;

			// Update list of supported cameras
			int deviceCount = 0;
			result = MV6D_DeviceListUpdate(h6D, &deviceCount);
			CheckResult(result, ConnectionFailedException::typeid, 2);
			char serial[128] = { 0 };

			// find a perception camera that's not in use
			for (int index = 0; index < deviceCount; ++index)
			{
				int inUse = 1;
				result = MV6D_DeviceListGetSerial(h6D, serial, sizeof(serial), &inUse, index);
				CheckResult(result, ConnectionFailedException::typeid, 3);

				if (!inUse)
				{
					break;
				}
			}

			if (nullptr != serial && 0 != strcmp("", serial))
			{
				SerialNumber = gcnew String(serial);
			}
			else
			{
				throw ExceptionBuilder::BuildFromID(ConnectionFailedException::typeid, this, 4, "No available mv6D camera found");
			}

			// open the device
			result = MV6D_DeviceOpen(h6D, serial);
			CheckResult(result, ConnectionFailedException::typeid, 5);

			// configure the stereo algorithm
			int filterSet = fsNone;
			result = MV6D_SetDepthPreset(h6D, daFilterSet, filterSet);
			CheckResult(result, ConnectionFailedException::typeid, 6);

			int minimumDistance = minDist800mm;
			result = MV6D_SetDepthPreset(h6D, daMinimumDistance, minimumDistance);
			CheckResult(result, ConnectionFailedException::typeid, 7);

			// get framerate property
			MV6D_Property framerateProperty;
			result = MV6D_PropertyGet(h6D, MV6D_PROPERTY_FRAMERATE, &framerateProperty);
			CheckResult(result, ConnectionFailedException::typeid, 8);

			// set framerate
			double framerate = 15.0;
			result = MV6D_PropertyWrite(h6D, framerateProperty, &framerate, sizeof(framerate));
			CheckResult(result, ConnectionFailedException::typeid, 9);

			result = MV6D_SetDepthPreset(h6D, daStereoAlgorithm, MV6D_Stereo_Algorithm::stereoAlgoRSGM);
			CheckResult(result, ConnectionFailedException::typeid, 10);

			// start capturing from the device
			result = MV6D_DeviceStart(h6D);
			CheckResult(result, ConnectionFailedException::typeid, 11);

			// Check calibration

			// request buffer pointers
			MV6D_RequestBuffer* currentRequestBuffer = nullptr;
			MV6D_RequestBuffer* lastRequestBuffer = nullptr;

			AutoExposure = true;

			ActivateChannel(ChannelNames::Color);
			ActivateChannel(ChannelNames::Distance);
			ActivateChannel((String^)CustomChannelNames::DepthMapped);
			ActivateChannel((String^)CustomChannelNames::DepthRaw);
			ActivateChannel((String^)CustomChannelNames::DistanceMapped);
			ActivateChannel((String^)CustomChannelNames::PointCloudMapped);
			ActivateChannel(ChannelNames::Left);
			ActivateChannel(ChannelNames::Right);
			SelectChannel(ChannelNames::Color);
		}

		void MvBlueSirius::DisconnectImpl()
		{
			MV6D_ResultCode result = MV6D_DeviceClose(h6D);
			CheckResult(result, InvalidOperationException::typeid, 12);
			result = MV6D_Close(h6D);
			CheckResult(result, InvalidOperationException::typeid, 13);

			System::Threading::Monitor::Enter(updateLock);
			_Master->FreeData();
			_Slave->FreeData();
			_Color->FreeData();
			_DepthMapped->FreeData();
			_DepthRaw->FreeData();
			System::Threading::Monitor::Exit(updateLock);
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
			if (MV6D_DeviceResultWaitFor(h6D, &requestBuffer, &dropped, timeout) == rcOk)
			{
				System::Threading::Monitor::Enter(updateLock);

				// get color image
				if (requestBuffer->colorMapped.pData)
				{
					_currentColorImage = nullptr;
					_Color->Width = requestBuffer->colorMapped.iWidth;
					_Color->Height = requestBuffer->colorMapped.iHeight;
					_Color->CopyColorData(requestBuffer->colorMapped);
				}

				// get master raw image
				if (requestBuffer->rawMaster.pData)
				{
					_currentMasterImage = nullptr;
					_Master->Width = requestBuffer->rawMaster.iWidth;
					_Master->Height = requestBuffer->rawMaster.iHeight;
					_Master->CopyGrayData(requestBuffer->rawMaster);
				}

				// get slave raw image
				if (requestBuffer->rawSlave1.pData)
				{
					_currentSlaveImage = nullptr;
					_Slave->Width = requestBuffer->rawSlave1.iWidth;
					_Slave->Height = requestBuffer->rawSlave1.iHeight;
					_Slave->CopyGrayData(requestBuffer->rawSlave1);
				}

				// get depth mapped image
				if (requestBuffer->depthMapped.pData)
				{
					_currentDepthMappedImage = nullptr;
					_DepthMapped->Width = requestBuffer->depthMapped.iWidth;
					_DepthMapped->Height = requestBuffer->depthMapped.iHeight;
					_DepthMapped->CopyDepthData(requestBuffer->depthMapped);
				}

				// get depth raw image
				if (requestBuffer->depthRaw.pData)
				{
					_currentDistanceImage = nullptr;
					_currentDistanceImageMapped = nullptr;
					_currentPointCloud = nullptr;
					_currentPointCloudMapped = nullptr;
					_currentDepthRawImage = nullptr;

					_DepthRaw->Width = requestBuffer->depthRaw.iWidth;
					_DepthRaw->Height= requestBuffer->depthRaw.iHeight;
					_DepthRaw->CopyDepthData(requestBuffer->depthRaw);
				}

				_focalLength = (float)requestBuffer->focalLength;

				System::Threading::Monitor::Exit(updateLock);

				// unlock request buffer
				MV6D_ResultCode result = MV6D_UnlockRequest(h6D, requestBuffer);
				CheckResult(result, InvalidOperationException::typeid, 14);
			}
		}

		CameraImage^ MvBlueSirius::CalcChannelImpl(String^ channelName)
		{
			if (ChannelNames::Color == channelName)
			{
				if (nullptr == _currentColorImage)
				{
					_currentColorImage = CalcColorImage(_Color);
				}
				return _currentColorImage;
			}
			if (ChannelNames::Left == channelName)
			{
				if (nullptr == _currentMasterImage)
				{
					_currentMasterImage = CalcFloatImage(_Master);
				}
				return _currentMasterImage;
			}
			if (ChannelNames::Right == channelName)
			{
				if (nullptr == _currentSlaveImage)
				{
					_currentSlaveImage = CalcFloatImage(_Slave);
				}
				return _currentSlaveImage;
			}
			if (CustomChannelNames::DepthMapped == channelName)
			{
				if (nullptr == _currentDepthMappedImage)
				{
					_currentDepthMappedImage = CalcFloatImage(_DepthMapped);
				}
				return _currentDepthMappedImage;
			}
			if (CustomChannelNames::DepthRaw == channelName)
			{
				if (nullptr == _currentDepthRawImage)
				{
					_currentDepthRawImage = CalcFloatImage(_DepthRaw);
				}
				return _currentDepthRawImage;
			}
			if (CustomChannelNames::PointCloudMapped == channelName)
			{
				if (nullptr == _currentPointCloudMapped)
				{
					FloatCameraImage^ depthImg = (FloatCameraImage^)CalcChannelImpl((System::String^)CustomChannelNames::DepthMapped);
					_currentPointCloudMapped = CalcPointCloud(depthImg);
				}
				return _currentPointCloudMapped;
			}
			if (CustomChannelNames::DistanceMapped == channelName)
			{
				if (nullptr == _currentDistanceImageMapped)
				{
					FloatCameraImage^ fImg = (FloatCameraImage^)CalcChannelImpl((System::String^)CustomChannelNames::PointCloudMapped);
					Point3fCameraImage^ pts3D = CalcPointCloud(fImg);
					_currentDistanceImageMapped = CalcDistances(pts3D);
				}
				return _currentDistanceImageMapped;
			}
			if (ChannelNames::Distance == channelName)
			{
				if (nullptr == _currentDistanceImage)
				{
					FloatCameraImage^ fImg = (FloatCameraImage^)CalcChannelImpl(ChannelNames::PointCloud);
					Point3fCameraImage^ pts3D = CalcPointCloud(fImg);
					_currentDistanceImage = CalcDistances(pts3D);
				}
				return _currentDistanceImage;
			}
			if (ChannelNames::PointCloud == channelName)
			{
				if (nullptr == _currentPointCloud)
				{
					FloatCameraImage^ depthImg = (FloatCameraImage^)CalcChannelImpl((System::String^)CustomChannelNames::DepthRaw);
					_currentPointCloud = CalcPointCloud(depthImg);
				}
				return _currentPointCloud;
			}

			// this should not happen, because Camera checks if the channel is active.
			return nullptr;
		}

		IProjectiveTransformation^ MvBlueSirius::GetIntrinsics(String^ channelName)
		{
			if (MetriCam2::ChannelNames::Distance == channelName || MetriCam2::ChannelNames::Amplitude == channelName)
			{
				return gcnew ProjectiveTransformationZhang(_DepthRaw->Width, _DepthRaw->Height, FocalLength, FocalLength, _DepthRaw->Width / 2.0f, _DepthRaw->Height / 2.0f, 0, 0, 0, 0, 0);
			}
			return Camera::GetIntrinsics(channelName);
		}

		ColorCameraImage ^ MvBlueSirius::CalcColorImage(ImageData^ image)
		{
			System::Threading::Monitor::Enter(updateLock);

			ColorCameraImage^ cImage = gcnew ColorCameraImage(image->Width, image->Height);
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

			System::Threading::Monitor::Exit(updateLock);

			return cImage;
		}

		FloatCameraImage ^ MvBlueSirius::CalcFloatImage(ImageData^ image)
		{
			System::Threading::Monitor::Enter(updateLock);

			FloatCameraImage^ fImage = gcnew FloatCameraImage(image->Width, image->Height);
			int i = 0;
			for (unsigned int y = 0; y < image->Height; y++)
			{
				for (unsigned int x = 0; x < image->Width; x++)
				{
					fImage[y, x] = image->Data[i++];
				}
			}

			System::Threading::Monitor::Exit(updateLock);

			return fImage;
		}

		FloatCameraImage ^ MvBlueSirius::CalcDistances(Point3fCameraImage^ image)
		{
			System::Threading::Monitor::Enter(updateLock);

			FloatCameraImage^ fImage = gcnew FloatCameraImage(image->Width, image->Height);
			int i = 0;
			for (unsigned int y = 0; y < image->Height; y++)
			{
				for (unsigned int x = 0; x < image->Width; x++)
				{
					fImage[y, x] = image[y, x].GetLength();
				}
			}

			System::Threading::Monitor::Exit(updateLock);

			return fImage;
		}

		Point3fCameraImage ^ MvBlueSirius::CalcPointCloud(FloatCameraImage^ depthImage)
		{
			System::Threading::Monitor::Enter(updateLock);


			Point3fCameraImage^ PointCloud = DepthImageToPointCloud(depthImage, FocalLength);

			System::Threading::Monitor::Exit(updateLock);

			return PointCloud;
		}


		Point3fCameraImage^ MvBlueSirius::DepthImageToPointCloud(FloatCameraImage^ depthImage, float focalLength)
		{
			int depthWidth = depthImage->Width;
			int depthHeight = depthImage->Height;

			Point3fCameraImage^ pointCloud = gcnew Point3fCameraImage(depthWidth, depthHeight);

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

					float wx = ((x - halfWidth) / focalLength) * wz;
					float wy = ((y - halfHeight) / focalLength) * wz;
					pointCloud[y, x] = Point3f(wx, wy, wz);
				}
			}

			return pointCloud;
		}
	}
}