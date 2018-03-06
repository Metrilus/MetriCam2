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
			: focalLength(0.0f), updateLock(gcnew Object())
		{
			modelName = "mvBlueSirius";

			int major = 0;
			int minor = 0;
			int patch = 0;
			const char* versionString = MV6D_GetBuildVersion(&major, &minor, &patch);
			log->InfoFormat("mv6D - {0}.{1}.{2} - Build \"{3}\"", major, minor, patch, gcnew String(versionString));

			pRawColorData = nullptr;
			colorBufferSizeInBytes = 0;
			colorWidth = 0;
			colorHeight = 0;
			currentColorImage = nullptr;

			pRawDepthMappedData = nullptr;
			depthMappedBufferNumElements = 0;
			depthMappedWidth = 0;
			depthMappedHeight = 0;
			currentDepthMappedImage = nullptr;

			pRawDepthRawData = nullptr;
			depthRawBufferNumElements = 0;
			depthRawWidth = 0;
			depthRawHeight = 0;
			currentDepthRawImage = nullptr;
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
			SelectChannel(ChannelNames::Color);
		}

		void MvBlueSirius::DisconnectImpl()
		{
			MV6D_ResultCode result = MV6D_DeviceClose(h6D);
			CheckResult(result, InvalidOperationException::typeid, 12);
			result = MV6D_Close(h6D);
			CheckResult(result, InvalidOperationException::typeid, 13);

			System::Threading::Monitor::Enter(updateLock);
			FreeColorBuffer();
			FreeDepthMappedBuffer();
			FreeDepthRawBuffer();
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
					currentColorImage = nullptr;
					colorWidth = requestBuffer->colorMapped.iWidth;
					colorHeight = requestBuffer->colorMapped.iHeight;
					CopyColorData(requestBuffer->colorMapped);
				}

				// get depth mapped image
				if (requestBuffer->depthMapped.pData)
				{
					currentDepthMappedImage = nullptr;
					depthMappedWidth = requestBuffer->depthMapped.iWidth;
					depthMappedHeight = requestBuffer->depthMapped.iHeight;
					CopyDepthMappedData(requestBuffer->depthMapped);
				}

				// get depth raw image
				if (requestBuffer->depthRaw.pData)
				{
					currentDistanceImage = nullptr;
					currentDistanceImageMapped = nullptr;
					currentPointCloud = nullptr;
					currentPointCloudMapped = nullptr;

					currentDepthRawImage = nullptr;
					depthRawWidth = requestBuffer->depthRaw.iWidth;
					depthRawHeight = requestBuffer->depthRaw.iHeight;
					CopyDepthRawData(requestBuffer->depthRaw);
				}

				focalLength = (float)requestBuffer->focalLength;

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
				return CalcColor();
			}
			if (CustomChannelNames::DepthMapped == channelName)
			{
				return CalcDepthMapped();
			}
			if (CustomChannelNames::DepthRaw == channelName)
			{
				return CalcDepthRaw();
			}
			if (CustomChannelNames::PointCloudMapped == channelName)
			{
				return CalcPointCloudFromDepthMapped();
			}
			if (CustomChannelNames::DistanceMapped == channelName)
			{
				return CalcDistancesMapped();
			}
			if (ChannelNames::Distance == channelName)
			{
				return CalcDistances();
			}
			if (ChannelNames::PointCloud == channelName)
			{
				return CalcPointCloudFromDepthRaw();
			}

			// this should not happen, because Camera checks if the channel is active.
			return nullptr;
		}

		IProjectiveTransformation^ MvBlueSirius::GetIntrinsics(String^ channelName)
		{
			if (MetriCam2::ChannelNames::Distance == channelName || MetriCam2::ChannelNames::Amplitude == channelName)
			{
				return gcnew ProjectiveTransformationZhang(depthRawWidth, depthRawHeight, FocalLength, FocalLength, depthRawWidth / 2.0f, depthRawHeight / 2.0f, 0, 0, 0, 0, 0);
			}
			return Camera::GetIntrinsics(channelName);
		}

		ColorCameraImage ^ MvBlueSirius::CalcColor()
		{
			System::Threading::Monitor::Enter(updateLock);

			if (nullptr == currentColorImage)
			{
				currentColorImage = gcnew ColorCameraImage(colorWidth, colorHeight);
				BitmapData^ bitmapData = currentColorImage->Data->LockBits(System::Drawing::Rectangle(0, 0, colorWidth, colorHeight), ImageLockMode::WriteOnly, currentColorImage->Data->PixelFormat);
				int colorIdx = 0;
				for (unsigned int y = 0; y < colorHeight; y++)
				{
					unsigned char* linePtr = (unsigned char*)(bitmapData->Scan0.ToPointer()) + bitmapData->Stride * y;
					for (unsigned int x = 0; x < colorWidth; x++)
					{
						*linePtr++ = pRawColorData[colorIdx++];
						*linePtr++ = pRawColorData[colorIdx++];
						*linePtr++ = pRawColorData[colorIdx++];
						*linePtr++ = 255;
					}
				}
				currentColorImage->Data->UnlockBits(bitmapData);
			}

			System::Threading::Monitor::Exit(updateLock);

			return currentColorImage;
		}

		FloatCameraImage ^ MvBlueSirius::CalcDepthMapped()
		{
			// TODO: possible optimization: do this in CopyDepthMappedData
			System::Threading::Monitor::Enter(updateLock);

			if (nullptr == currentDepthMappedImage)
			{
				currentDepthMappedImage = gcnew FloatCameraImage(depthMappedWidth, depthMappedHeight);
				int i = 0;
				for (unsigned int y = 0; y < depthMappedHeight; y++)
				{
					for (unsigned int x = 0; x < depthMappedWidth; x++)
					{
						currentDepthMappedImage[y, x] = pRawDepthMappedData[i++];
					}
				}
			}

			System::Threading::Monitor::Exit(updateLock);

			return currentDepthMappedImage;
		}

		FloatCameraImage ^ MvBlueSirius::CalcDepthRaw()
		{
			// TODO: possible optimization: do this in CopyDepthRawData
			System::Threading::Monitor::Enter(updateLock);

			if (nullptr == currentDepthRawImage)
			{
				currentDepthRawImage = gcnew FloatCameraImage(depthRawWidth, depthRawHeight);
				int i = 0;
				for (unsigned int y = 0; y < depthRawHeight; y++)
				{
					for (unsigned int x = 0; x < depthRawWidth; x++)
					{
						currentDepthRawImage[y, x] = pRawDepthRawData[i++];
					}
				}
			}

			System::Threading::Monitor::Exit(updateLock);

			return currentDepthRawImage;
		}

		FloatCameraImage ^ MvBlueSirius::CalcDistances()
		{
			System::Threading::Monitor::Enter(updateLock);

			if (nullptr == currentDistanceImage)
			{
				// Compute distances
				Point3fCameraImage^ pts3D = CalcPointCloudFromDepthRaw();

				currentDistanceImage = gcnew FloatCameraImage(depthRawWidth, depthRawHeight);

				for (unsigned int y = 0; y < depthRawHeight; y++)
				{
					for (unsigned int x = 0; x < depthRawWidth; x++)
					{
						currentDistanceImage[y, x] = pts3D[y, x].GetLength();
					}
				}
			}

			System::Threading::Monitor::Exit(updateLock);

			return currentDistanceImage;
		}

		FloatCameraImage ^ MvBlueSirius::CalcDistancesMapped()
		{
			System::Threading::Monitor::Enter(updateLock);

			if (nullptr == currentDistanceImageMapped)
			{
				// Compute distances
				Point3fCameraImage^ pts3D = CalcPointCloudFromDepthMapped();

				currentDistanceImageMapped = gcnew FloatCameraImage(depthRawWidth, depthRawHeight);

				for (unsigned int y = 0; y < depthRawHeight; y++)
				{
					for (unsigned int x = 0; x < depthRawWidth; x++)
					{
						currentDistanceImageMapped[y, x] = pts3D[y, x].GetLength();
					}
				}
			}

			System::Threading::Monitor::Exit(updateLock);

			return currentDistanceImageMapped;
		}

		Point3fCameraImage ^ MvBlueSirius::CalcPointCloudFromDepthRaw()
		{
			log->Error("CalcPointCloud is incomplete.");
			System::Threading::Monitor::Enter(updateLock);

			if (nullptr == currentPointCloud)
			{
				FloatCameraImage^ depthRawImage = CalcDepthRaw();
				currentPointCloud = DepthImageToPointCloud(depthRawImage, FocalLength);
			}

			System::Threading::Monitor::Exit(updateLock);

			return currentPointCloud;
		}

		Point3fCameraImage ^ MvBlueSirius::CalcPointCloudFromDepthMapped()
		{
			log->Error("CalcPointCloud is incomplete.");
			System::Threading::Monitor::Enter(updateLock);

			if (nullptr == currentPointCloudMapped)
			{
				FloatCameraImage^ depthMappedImage = CalcDepthMapped();
				currentPointCloudMapped = DepthImageToPointCloud(depthMappedImage, FocalLength);
			}

			System::Threading::Monitor::Exit(updateLock);

			return currentPointCloudMapped;
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

		FloatCameraImage ^ MvBlueSirius::CalcDepthRawIR()
		{
			throw gcnew System::NotImplementedException();
			// TODO: insert return statement here
		}

		FloatCameraImage ^ MvBlueSirius::CalcFlow()
		{
			throw gcnew System::NotImplementedException();
			// TODO: insert return statement here
		}

		void MvBlueSirius::CopyColorData(MV6D_ColorBuffer colorBuffer)
		{
			int sizeInBytes = colorWidth * colorHeight * 3;
			ResizeColorBuffer(sizeInBytes);
			int pxIdx = 0;
			int i = 0;
			for (unsigned int y = 0; y < colorHeight; y++)
			{
				for (unsigned int x = 0; x < colorWidth; x++)
				{
					pRawColorData[i++] = colorBuffer.pData[pxIdx].b;
					pRawColorData[i++] = colorBuffer.pData[pxIdx].g;
					pRawColorData[i++] = colorBuffer.pData[pxIdx].r;
					pxIdx++;
				}
			}
		}

		void MvBlueSirius::CopyDepthMappedData(MV6D_DepthBuffer depthBuffer)
		{
			int width = depthBuffer.iWidth;
			int height = depthBuffer.iHeight;
			int numElements = width * height;
			int sizeInBytes = numElements * sizeof(float);
			ResizeDepthMappedBuffer(sizeInBytes);
			memcpy_s((void*)pRawDepthMappedData, depthMappedBufferNumElements, (void*)depthBuffer.pData, sizeInBytes);
		}

		void MvBlueSirius::CopyDepthRawData(MV6D_DepthBuffer depthBuffer)
		{
			int width = depthBuffer.iWidth;
			int height = depthBuffer.iHeight;
			int numElements = width * height;
			int sizeInBytes = numElements * sizeof(float);
			ResizeDepthRawBuffer(sizeInBytes);
			memcpy_s((void*)pRawDepthRawData, depthRawBufferNumElements, (void*)depthBuffer.pData, sizeInBytes);
		}

		void MvBlueSirius::ResizeColorBuffer(int sizeInBytes)
		{
			if (nullptr != pRawColorData)
			{
				// Buffer exists
				if (colorBufferSizeInBytes == sizeInBytes)
				{
					// Buffer has correct size
					return;
				}

				FreeColorBuffer();
			}

			pRawColorData = new unsigned char[sizeInBytes];
			colorBufferSizeInBytes = sizeInBytes;
		}

		void MvBlueSirius::ResizeDepthMappedBuffer(int numElements)
		{
			if (nullptr != pRawDepthMappedData)
			{
				// Buffer exists
				if (depthMappedBufferNumElements == numElements)
				{
					// Buffer has correct size
					return;
				}

				FreeDepthMappedBuffer();
			}

			pRawDepthMappedData = new float[numElements];
			depthMappedBufferNumElements = numElements;
		}

		void MvBlueSirius::ResizeDepthRawBuffer(int numElements)
		{
			if (nullptr != pRawDepthRawData)
			{
				// Buffer exists
				if (depthRawBufferNumElements == numElements)
				{
					// Buffer has correct size
					return;
				}

				FreeDepthRawBuffer();
			}

			pRawDepthRawData = new float[numElements];
			depthRawBufferNumElements = numElements;
		}
	}
}