// This is the main DLL file.
#include "stdafx.h"
#include "Orbbec.h"
#include <cmath>

using namespace System::Drawing;
using namespace System::Drawing::Imaging;
using namespace astra;

MetriCam2::Cameras::Astra::Astra()
{
	modelName = "Astra";

	enableImplicitThreadSafety = true;
}

MetriCam2::Cameras::Astra::~Astra()
{
	log->EnterMethod();
	log->LeaveMethod();
}

void MetriCam2::Cameras::Astra::LoadAllAvailableChannels()
{
	log->EnterMethod();
	ChannelRegistry^ cr = ChannelRegistry::Instance;
	Channels->Clear();
	Channels->Add(cr->RegisterCustomChannel((String^)CustomChannelNames::Infrared, UShortCameraImage::typeid));
	//Channels->Add(cr->RegisterChannel(ChannelNames::Color));
	Channels->Add(cr->RegisterChannel(ChannelNames::ZImage));
	Channels->Add(cr->RegisterChannel(ChannelNames::Point3DImage));
	log->LeaveMethod();
}

void MetriCam2::Cameras::Astra::ConnectImpl()
{
	log->EnterMethod();

	wrapper = new OrbbecWrapper();

	if(depthQvga)
	{
		wrapper->DepthWidth = 320;
		wrapper->DepthHeight = 240;
	}

	if (ActiveChannels->Count == 0)
	{
		ActivateChannel((String^)CustomChannelNames::Infrared);
		ActivateChannel(ChannelNames::ZImage);
		ActivateChannel(ChannelNames::Point3DImage);
//		ActivateChannel(ChannelNames::Color);
	}

	if (IsChannelActive((String^)CustomChannelNames::Infrared))
	{
		wrapper->StartInfraredStream();
	}

	if (IsChannelActive(ChannelNames::ZImage))
	{
		wrapper->StartDepthStream();
	}

	if (IsChannelActive(ChannelNames::Point3DImage))
	{
		wrapper->StartPointStream();
	}


	if (String::IsNullOrWhiteSpace(SelectedChannel))
	{
		SelectChannel((String^)MetriCam2::ChannelNames::ZImage);
	}
	log->LeaveMethod();
}

void MetriCam2::Cameras::Astra::DisconnectImpl()
{
	log->EnterMethod();

	wrapper->StopInfraredStream();
	wrapper->StopDepthStream();
	wrapper->StopPointStream();

	delete wrapper;
	wrapper = nullptr;

	log->LeaveMethod();
}

void MetriCam2::Cameras::Astra::UpdateImpl()
{
	wrapper->Update();
}

Metrilus::Util::CameraImage ^ MetriCam2::Cameras::Astra::CalcChannelImpl(String ^ channelName)
{
	log->EnterMethod();
	if (ChannelNames::Color == channelName)
	{
		return CalcColor();
	}
	if (CustomChannelNames::Infrared == channelName)
	{
		return CalcInfrared();
	}
	if (ChannelNames::ZImage == channelName)
	{
		return CalcZImage();
	}
	if (ChannelNames::Point3DImage == channelName)
	{
		return CalcPoint3fImage();
	}

	log->LeaveMethod();

	// this should not happen, because Camera checks if the channel is active.
	return nullptr;
}

Point3fCameraImage^ MetriCam2::Cameras::Astra::CalcPoint3fImage()
{
	log->EnterMethod();
	astra::PointFrame pointFrame = wrapper->GetPointFrame();
	Point3fCameraImage^ image = gcnew Point3fCameraImage(pointFrame.width(), pointFrame.height());
	int width = pointFrame.width();
	int height = pointFrame.height();
	float factor = 1 / 1000.0f;

	for (int y = 0; y < height; y++)
	{
		for (int x = 0; x < width; x++)
		{
			int idx = y*width + x;
			astra::Vector3f v = pointFrame.data()[idx];
			image[y, x] = Point3f(v.x * factor, -v.y * factor, v.z * factor);
		}
	}
	log->LeaveMethod();
	return image;
}

UShortCameraImage ^ MetriCam2::Cameras::Astra::CalcInfrared()
{
	log->EnterMethod();
	astra::InfraredFrame16 infraredFrame = wrapper->GetInfraredFrame();
	UShortCameraImage^ image = gcnew UShortCameraImage(infraredFrame.width(), infraredFrame.height());

	// If there was no bug, copying the data would be sufficient.
	// infraredFrame.copy_to(image->Data);

	// Compensate for offset bug: Translate infrared frame by 8 pixels in vertical direction to match infrared with depth image.
	// Leave first 8 rows black. Constructor of UShortCameraImage assigns zero to every pixel as initial value by default.
	int translation = 8;

	// Copy the rest translated by 8 pixels in vertical direction. Cut off the last 8 rows
	for (int y = translation; y < infraredFrame.height(); y++) {
		for (int x = 0; x < infraredFrame.width(); x++) {
			int idx = (y - translation) * infraredFrame.width() + x;
			image[y, x] = infraredFrame.data()[idx];
		}
	}

	log->LeaveMethod();
	return image;
}

FloatCameraImage ^ MetriCam2::Cameras::Astra::CalcZImage()
{
	log->EnterMethod();
	
	astra::DepthFrame depthFrame = wrapper->GetDepthFrame();

	int width = depthFrame.width();
	int height = depthFrame.height();

	ShortCameraImage^ image = gcnew ShortCameraImage(width, height);
	depthFrame.copy_to(image->Data);

	FloatCameraImage^ depths = gcnew FloatCameraImage(image->Width, image->Height);

	float factor = 1 / 1000.0f;

	for (int y = 0; y < height; y++)
	{ 
		for (int x = 0; x < width; x++)
		{
			depths[y, x] = image[y, x] * factor;
		}
	}
	log->LeaveMethod();
	return depths;
}

ColorCameraImage ^ MetriCam2::Cameras::Astra::CalcColor()
{
	log->EnterMethod();
	astra::ColorFrame colorFrame = wrapper->GetColorFrame();
	Bitmap^ bitmap = gcnew Bitmap(colorFrame.width(), colorFrame.height(), PixelFormat::Format24bppRgb);

	Rectangle^ imageRect = gcnew Rectangle(0, 0, bitmap->Width, bitmap->Height);
	BitmapData^ bmpData = bitmap->LockBits(*imageRect, ImageLockMode::WriteOnly, bitmap->PixelFormat);
	
	colorFrame.copy_to((astra::RgbPixel*)(void*)(bmpData->Scan0));

	bitmap->UnlockBits(bmpData);

	ColorCameraImage^ image = gcnew ColorCameraImage(bitmap);

	log->LeaveMethod();
	return image;
}

Metrilus::Util::IProjectiveTransformation^ MetriCam2::Cameras::Astra::GetIntrinsics(String^ channelName)
{
	Metrilus::Util::IProjectiveTransformation^ result = nullptr;

	log->Info("Trying to load projective transformation from file.");
	try
	{
		result = Camera::GetIntrinsics(channelName);
	}
	catch (...) { /* empty */ }

	if (result == nullptr)
	{
		log->Info("Projective transformation file not found.");
		log->Info("Using Orbbec factory intrinsics as projective transformation.");

		result = gcnew Metrilus::Util::ProjectiveTransformationZhang(wrapper->Width, wrapper->Height, wrapper->FocalLengthX, wrapper->FocalLengthY, wrapper->Width * 0.5f, wrapper->Height * 0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
	}
	return result;
}