// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

#include "stdafx.h"
#include <string>
// #include <msclr\marshal_cppstd.h>

#include "OrbbecOpenNI.h"

#define MAX_DEVICES 20  // There is no limitations, we choose 20 as "reasonable" value in real use case.

//Adpated from SimpleViewer of experimental interface
#define IR_Exposure_MAX 0x1000
#define IR_Exposure_MIN 0x0
#define IR_Exposure_SCALE 256
#define IR_Gain_MIN 0x08
#define IR_Gain_MAX 0x60

// TODO:
// * support different resolutions (not only VGA) for channels ZImage and Intensity and check whether IR gain/exposure set feature is still working.
// * At least the mode 1280x1024 seems to be not compatible with the IR exposure set feature. For QVGA there seem to be problems to set the exposure when the Intensity channel is activated.

MetriCam2::Cameras::AstraOpenNI::AstraOpenNI()
{
	camData = new OrbbecNativeCameraData();
	camData->openNICam = new cmd();

	camData->depth = new openni::VideoStream();
	camData->ir = new openni::VideoStream();
	camData->color = new openni::VideoStream();

	// Init to most reasonable values; update during ConnectImpl
	_emitterEnabled = true;
	_irFlooderEnabled = false;
}

MetriCam2::Cameras::AstraOpenNI::~AstraOpenNI()
{
	//TODO: clean up camData->openNICam, camData->depth and camData->ir
	delete camData;
}

void MetriCam2::Cameras::AstraOpenNI::LogOpenNIError(String^ status) 
{
	log->Error(status + "\n" + gcnew String(openni::OpenNI::getExtendedError()));
}

bool MetriCam2::Cameras::AstraOpenNI::OpenNIInit() 
{
	int counter = System::Threading::Interlocked::Increment(openNIInitCounter);
	if (counter > 1) {
		// OpenNI is already intialized
		return true;
	}

	// Freshly initialize OpenNI
	openni::Status rc = openni::STATUS_OK;
	rc = openni::OpenNI::initialize();
	if (openni::Status::STATUS_OK != rc) 
	{
		LogOpenNIError("Initialization of OpenNI failed.");
		return false;
	}

	return true;
}

bool MetriCam2::Cameras::AstraOpenNI::OpenNIShutdown() 
{
	int counter = System::Threading::Interlocked::Decrement(openNIInitCounter);

	if (0 != counter) 
	{
		// Someone is still using OpenNI
		return true;
	}

	openni::Status rc = openni::STATUS_OK;
	openni::OpenNI::shutdown();
	if (openni::Status::STATUS_OK != rc) 
	{
		LogOpenNIError("Shutdown of OpenNI failed");
		return false;
	}
	return true;
}

System::Collections::Generic::Dictionary<String^, String^>^ MetriCam2::Cameras::AstraOpenNI::GetSerialToUriMappingOfAttachedCameras()
{
	bool initSucceeded = OpenNIInit();
	if (!initSucceeded) 
	{
		// Error already logged
		return nullptr;
	}

	openni::Status rc = openni::STATUS_OK;

	openni::VideoStream    depthStreams[MAX_DEVICES];
	const char*            deviceUris[MAX_DEVICES];
	char                   serialNumbers[MAX_DEVICES][12]; // Astra serial number has 12 numbers

														   // Enumerate devices
	openni::Array<openni::DeviceInfo> deviceList;
	openni::OpenNI::enumerateDevices(&deviceList);
	int devicesCount = deviceList.getSize();

	if (devicesCount >= MAX_DEVICES)
	{
		log->Error("The number of supported devices is limited.");
		OpenNIShutdown();
		return nullptr;
	}

	System::Collections::Generic::Dictionary<String^, String^>^ serialToURI = gcnew System::Collections::Generic::Dictionary<String^, String^>();

	for (int i = 0; i < devicesCount; i++) {
		// Open device by Uri
		openni::Device* device = new openni::Device;
		deviceUris[i] = deviceList[i].getUri();

		rc = device->open(deviceUris[i]);
		if (openni::Status::STATUS_OK != rc) {
			// CheckOpenNIError(rc, "Couldn't open device : ", deviceUris[i]);
			System::Diagnostics::Debug::WriteLine("GetSerialNumberOfAttachedCameras: cannot open device");
			continue;
		}

		rc = depthStreams[i].create(*device, openni::SENSOR_DEPTH);
		if (openni::Status::STATUS_OK != rc) {
			// CheckOpenNIError(rc, "Couldn't create stream on device : ", deviceUris[i]);
			System::Diagnostics::Debug::WriteLine("GetSerialNumberOfAttachedCameras: cannot create device");
			continue;
		}

		// Read serial number
		int data_size = sizeof(serialNumbers[i]);
		device->getProperty((int)ONI_DEVICE_PROPERTY_SERIAL_NUMBER, (void *)serialNumbers[i], &data_size);

		rc = depthStreams[i].start();
		if (openni::Status::STATUS_OK != rc) {
			// CheckOpenNIError(rc, "Couldn't create stream on device : ", serialNumbers[i]);
			System::Diagnostics::Debug::WriteLine("GetSerialNumberOfAttachedCameras: cannot start depth stream");
			continue;
		}

		if (!depthStreams[i].isValid())
		{
			// printf("SimpleViewer: No valid streams. Exiting\n");
			System::Diagnostics::Debug::WriteLine("GetSerialNumberOfAttachedCameras: depth stream not valid");
			openni::OpenNI::shutdown();
			continue;
		}

		// Close depth stream and device
		depthStreams[i].stop();
		depthStreams[i].destroy();
		device->close();

		serialToURI[gcnew String(serialNumbers[i])] = gcnew String(deviceUris[i]);
	}

	return serialToURI;
}

void MetriCam2::Cameras::AstraOpenNI::LoadAllAvailableChannels()
{
	//log->EnterMethod();
	ChannelRegistry^ cr = ChannelRegistry::Instance;
	Channels->Clear();
	// Channels->Add(cr->RegisterCustomChannel((String^)CustomChannelNames::Infrared, UShortCameraImage::typeid))
	Channels->Add(cr->RegisterChannel(ChannelNames::ZImage));
	Channels->Add(cr->RegisterChannel(ChannelNames::Intensity));
	Channels->Add(cr->RegisterChannel(ChannelNames::Point3DImage));
	Channels->Add(cr->RegisterChannel(ChannelNames::Color));
	//log->LeaveMethod();
}

void MetriCam2::Cameras::AstraOpenNI::ConnectImpl()
{
	bool initSucceeded = OpenNIInit();
	if (!initSucceeded) 
	{
		// Error already logged
		return;
	}

	serialToUriDictionary = GetSerialToUriMappingOfAttachedCameras();

	const char* deviceURI = openni::ANY_DEVICE;
	if (nullptr != SerialNumber && 0 != SerialNumber->Length)
	{
		deviceURI = oMarshalContext.marshal_as<const char*>(serialToUriDictionary[SerialNumber]);
	}

	int rc = camData->openNICam->init(deviceURI);
	camData->openNICam->ldp_set(true); //Ensure eye-safety by turning on the proximity sensor

	// Start depth stream
	camData->openNICam->device.setImageRegistrationMode(openni::IMAGE_REGISTRATION_OFF);

	InitDepthStream();
	InitIRStream();
	InitColorStream();

	if (ActiveChannels->Count == 0)
	{
		ActivateChannel(ChannelNames::ZImage);
		ActivateChannel(ChannelNames::Point3DImage);
		//ActivateChannel(ChannelNames::Color); //Do not activate channel color by default in order to avoid running into bandwidth problems (e.g. when multiple cameras ares used over USB hubs)
		//ActivateChannel(ChannelNames::Intensity); // Channel intensity cannot be activated, if depth/3D data channel is active
		if (String::IsNullOrWhiteSpace(SelectedChannel))
		{
			SelectChannel(ChannelNames::ZImage);
		}
	}

	irGain = GetIRGain();
	_emitterEnabled = GetEmitterStatus() == "On";
	_irFlooderEnabled = GetIRFlooderStatus() == "On";
}

void MetriCam2::Cameras::AstraOpenNI::SetEmitterStatus(bool on)
{
	if (camData->openNICam->m_vid != 0x1d27) //Check if our device is not an Asus-Carmine device
	{
		if (camData->openNICam->ldp_set(on) != openni::STATUS_OK)
		{
			LogOpenNIError("LDP set failed");
		}
		System::Threading::Thread::Sleep(100); //Is required, otherwise turning off the emitter did not work in some cases, also not when just waiting 50ms
	}

	// Try to activate next code block in future version of experimental SDK (class "cmd"). Currently, the LDP status is alwas unknown
	//LDPStatus status;
	//LDPStatus statusToSet = on ? LDPStatus::LDP_ON : LDPStatus::LDP_OFF;
	////We need to be sure that the proximity sensor status was set properly
	//do
	//{
	//	camData->openNICam->ldp_get(status);
	//	System::Threading::Thread::Sleep(1);
	//}
	//while (status != statusToSet);

	if (camData->openNICam->emitter_set(on) != openni::STATUS_OK)
	{
		LogOpenNIError("Emitter set failed");
	}
}

String^ MetriCam2::Cameras::AstraOpenNI::GetEmitterStatus()
{
	LaserStatus status;
	if (camData->openNICam->emitter_get(status) != openni::STATUS_OK)
	{
		LogOpenNIError("Emitter get failed");
	}
	String^ statusString = "Unknown";
	if (status == LaserStatus::LASER_OFF)
	{
		statusString = "Off";
	}
	else if (status == LaserStatus::LASER_ON)
	{
		statusString = "On";
	}
	log->DebugFormat("Emitter status is: {0}", statusString);
	return statusString;
}

void MetriCam2::Cameras::AstraOpenNI::SetIRFlooderStatus(bool on)
{
	// Try to activate next code block in future version of experimental SDK (class "cmd"). Currently, the IrFloodLedStatus status is alwas unknown
	//IrFloodLedStatus statusToSet = on ? IrFloodLedStatus::IR_LED_ON : IrFloodLedStatus::IR_LED_OFF;
	//camData->openNICam->ir_flood_set(statusToSet);
	////We need to be sure that the proximity sensor status was set properly
	//IrFloodLedStatus status;
	//do
	//{
	//	camData->openNICam->ir_flood_get(status);
	//	System::Threading::Thread::Sleep(1);
	//}
	//while (status != statusToSet);

	if (camData->openNICam->ir_flood_set(on) != openni::STATUS_OK)
	{
		LogOpenNIError("ir flooder set failed");
	}
}

String^ MetriCam2::Cameras::AstraOpenNI::GetIRFlooderStatus()
{
	IrFloodLedStatus status;
	if (camData->openNICam->ir_flood_get(status) != openni::STATUS_OK)
	{
		LogOpenNIError("ir_flood_get failed");
	}
	String^ statusString = "Unknown";
	if (status == IrFloodLedStatus::IR_LED_OFF)
	{
		statusString = "Off";
	}
	else if (status == IrFloodLedStatus::IR_LED_ON)
	{
		statusString = "On";
	}
	log->DebugFormat("IR flooder status is: {0}", statusString);
	return statusString;
}

void MetriCam2::Cameras::AstraOpenNI::SetIRGain(char valueChar)
{
	const char* value;
	if (valueChar < IR_Gain_MIN)
	{
		value = "0x08";
	}
	else if (valueChar > IR_Gain_MAX)
	{
		value = "0x60";
	}
	else
	{	
		value = (char*)Marshal::StringToHGlobalAnsi("0x" + Convert::ToString((int)valueChar, 16)).ToPointer();
	}	

	if (!camData->openNICam->ir_gain_set(value))
	{
		LogOpenNIError("Set IR gain failed");
	}
	else
	{
		log->DebugFormat("IR gain is set to: {0}", gcnew String(value));
	}
}

unsigned short MetriCam2::Cameras::AstraOpenNI::GetIRGain()
{
	camData->openNICam->ir_gain_get();
	return camData->openNICam->m_I2CReg;
}

void MetriCam2::Cameras::AstraOpenNI::SetIRExposure(unsigned int value)
{
	unsigned int irExposure;
	if (value < IR_Exposure_MIN)
	{
		irExposure = IR_Exposure_MIN;
	}
	else if (value > IR_Exposure_MAX)
	{
		irExposure = IR_Exposure_MAX;
	}
	else
	{
		irExposure = value;
	}

	if (camData->openNICam->ir_exposure_set(irExposure) != openni::STATUS_OK)
	{
		LogOpenNIError("Set IR exposure failed");
	}
	else
	{
		log->DebugFormat("IR exposure is set to: {0}", irExposure.ToString());
	}
	//camData->depth->stop();	
	if (!IsChannelActive(ChannelNames::Intensity))
	{
		camData->ir->start();
		if (camData->ir->isValid())
		{
			VideoMode videomode = camData->ir->getVideoMode();
			videomode.setPixelFormat(openni::PIXEL_FORMAT_GRAY16);
			videomode.setResolution(640, 480);
			camData->ir->setVideoMode(videomode);
		}
		camData->ir->stop();
	}
	if (!IsChannelActive(ChannelNames::ZImage))
	{
		camData->depth->start();
		if (camData->depth->isValid())
		{
			VideoMode videoMode = camData->depth->getVideoMode();
			videoMode.setResolution(640, 480);
			camData->depth->setVideoMode(videoMode);
		}
		camData->depth->stop();
	}
}

unsigned int MetriCam2::Cameras::AstraOpenNI::GetIRExposure()
{
	unsigned int exposure;
	if (camData->openNICam->ir_exposure_get(exposure) != openni::STATUS_OK)
	{
		LogOpenNIError("Get IR exposure failed");
	}
	return exposure;
}

void MetriCam2::Cameras::AstraOpenNI::DisconnectImpl()
{
	// TODO
	camData->depth->destroy();
	camData->ir->destroy();
	OpenNIShutdown();
}

void MetriCam2::Cameras::AstraOpenNI::UpdateImpl()
{
	openni::VideoStream** m_streams = new openni::VideoStream*[2];
	m_streams[0] = camData->depth;
	m_streams[1] = camData->ir;
	//m_streams[2] = camData->color;

	int changedIndex;
	openni::Status rc = openni::OpenNI::waitForAnyStream(m_streams, 2, &changedIndex);

	if (openni::STATUS_OK != rc)
	{
		log->Error("Wait failed\n");
		return;
	}
}

Metrilus::Util::CameraImage ^ MetriCam2::Cameras::AstraOpenNI::CalcChannelImpl(String ^ channelName)
{
	log->EnterMethod();
	if (channelName->Equals(ChannelNames::ZImage))
	{
		return CalcZImage();
	}
	else if (channelName->Equals(ChannelNames::Intensity))
	{
		return CalcIRImage();
	}
	else if (channelName->Equals(ChannelNames::Color))
	{
		return CalcColor();
	}
	else if (channelName->Equals(ChannelNames::Point3DImage))
	{
		return CalcPoint3fImage();
	}

	log->LeaveMethod();
	return nullptr;
}

void MetriCam2::Cameras::AstraOpenNI::InitDepthStream()
{
	// Create depth stream reader
	openni::Status rc = camData->depth->create(camData->openNICam->device, openni::SENSOR_DEPTH);
	camData->depth->setMirroringEnabled(false);
	if (openni::STATUS_OK != rc)
	{
		throw gcnew Exception("Couldn't find depth stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
	}
}

void MetriCam2::Cameras::AstraOpenNI::InitIRStream()
{
	//Init IR stream
	openni::Status rc = camData->ir->create(camData->openNICam->device, openni::SENSOR_IR);
	camData->ir->setMirroringEnabled(false);
	if (openni::STATUS_OK != rc)
	{
		log->Error("Couldn't find IR stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
		openni::OpenNI::shutdown();
		return;
	}
}

void MetriCam2::Cameras::AstraOpenNI::InitColorStream()
{
	//Init color stream
	openni::Status rc = camData->color->create(camData->openNICam->device, openni::SENSOR_COLOR);
	camData->color->setMirroringEnabled(false);
	if (openni::STATUS_OK != rc)
	{
		log->Error("Couldn't find color stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
		openni::OpenNI::shutdown();
		return;
	}
}

void MetriCam2::Cameras::AstraOpenNI::ActivateChannelImpl(String^ channelName)
{
	log->EnterMethod();

	openni::Status rc;

	if (channelName->Equals(ChannelNames::ZImage) || channelName->Equals(ChannelNames::Point3DImage))
	{
		if (IsChannelActive(ChannelNames::Intensity))
		{
			throw gcnew Exception("IR and depth are not allowed to be active at the same time. Please deactivate channel \"Intensity\" before activating channel \"ZImage\" or \"Point3DImage\"");
		}

		openni::VideoMode depthVideoMode = camData->depth->getVideoMode();
		depthVideoMode.setResolution(640, 480);
		camData->depth->setVideoMode(depthVideoMode);

		// Start depth stream
		rc = camData->depth->start();
		if (openni::STATUS_OK != rc)
		{
			log->Error("Couldn't start depth stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
			camData->depth->destroy();
			openni::OpenNI::shutdown();
			return;
		}

		if (!camData->depth->isValid())
		{
			log->Error("No valid depth stream. Exiting\n");
			openni::OpenNI::shutdown();
			return;
		}

		depthVideoMode = camData->depth->getVideoMode();
		camData->depthWidth = depthVideoMode.getResolutionX();
		camData->depthHeight = depthVideoMode.getResolutionY();

		if (this->IsConnected)
		{
			//Activating the depth channel resets the IR gain to the default value -> we need to restore the value that was set before.
			SetIRGain(irGain);
		}
	}
	else if (channelName->Equals(ChannelNames::Intensity))
	{	
		if (IsChannelActive(ChannelNames::ZImage) || IsChannelActive(ChannelNames::Point3DImage) || IsChannelActive(ChannelNames::Color))
		{
			throw gcnew Exception("IR and depth/color are not allowed to be active at the same time. Please deactivate channel \"ZImage\", \"Point3DImage\" and \"Color\" before activating channel \"Intensity\"");
		}

		//Changing the exposure is not possible if both depth and ir streams have been running parallel in one session.

		openni::VideoMode irVideoMode = camData->ir->getVideoMode();
		irVideoMode.setResolution(640, 480);
		camData->ir->setVideoMode(irVideoMode);		

		rc = camData->ir->start();
		if (openni::STATUS_OK != rc)
		{
			log->Error("Couldn't start IR stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
			camData->ir->destroy();
			openni::OpenNI::shutdown();
			return;
		}

		if (!camData->ir->isValid())
		{
			log->Error("No valid IR stream. Exiting\n");
			openni::OpenNI::shutdown();
			return;
		}

		irVideoMode = camData->ir->getVideoMode();
		camData->irWidth = irVideoMode.getResolutionX();
		camData->irHeight = irVideoMode.getResolutionY();
	}
	else if (channelName->Equals(ChannelNames::Color))
	{
		if (IsChannelActive(ChannelNames::Intensity))
		{
			throw gcnew Exception("IR and color are not allowed to be active at the same time. Please deactivate channel \"Intensity\" before activating channel \"Color\"");
		}

		openni::VideoMode colorVideoMode = camData->color->getVideoMode();
		//Setting the resolution to 1280/640 does not work, even if we start only the color channel (image is corrupted)
		/*colorVideoMode.setResolution(1280, 960);
		colorVideoMode.setFps(7);*/
		colorVideoMode.setResolution(640, 480);
		camData->color->setVideoMode(colorVideoMode);

		rc = camData->color->start();
		if (openni::STATUS_OK != rc)
		{
			log->Error("Couldn't start color stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
			camData->color->destroy();
			openni::OpenNI::shutdown();
			return;
		}

		if (!camData->color->isValid())
		{
			log->Error("No valid color stream. Exiting\n");
			openni::OpenNI::shutdown();
			return;
		}

		colorVideoMode = camData->color->getVideoMode();
		camData->colorWidth = colorVideoMode.getResolutionX();
		camData->colorHeight = colorVideoMode.getResolutionY();
	}

	log->LeaveMethod();
}

void MetriCam2::Cameras::AstraOpenNI::DeactivateChannelImpl(String^ channelName)
{
	if (channelName->Equals(ChannelNames::ZImage) || channelName->Equals(ChannelNames::Point3DImage))
	{
		camData->depth->stop();
	}
	else if (channelName->Equals(ChannelNames::Intensity))
	{
		camData->ir->stop();
	}
	else if (channelName->Equals(ChannelNames::Color))
	{
		camData->color->stop();
	}
}

FloatCameraImage ^ MetriCam2::Cameras::AstraOpenNI::CalcZImage()
{
	openni::VideoFrameRef		m_depthFrame;
	camData->depth->readFrame(&m_depthFrame);

	if (!m_depthFrame.isValid())
	{
		log->Error("Depth frame is not valid...\n");
		return nullptr;
	}

	const openni::DepthPixel* pDepthRow = (const openni::DepthPixel*)m_depthFrame.getData();
	int rowSize = m_depthFrame.getStrideInBytes() / sizeof(openni::DepthPixel);
	FloatCameraImage^ depthDataMeters = gcnew FloatCameraImage(m_depthFrame.getWidth(), m_depthFrame.getHeight());

	for (int y = 0; y < m_depthFrame.getHeight(); ++y)
	{
		const openni::DepthPixel* pDepth = pDepthRow;
		for (int x = 0; x < m_depthFrame.getWidth(); ++x, ++pDepth)
		{
			// Normalize to meters
			depthDataMeters[y, x] = (float)*pDepth * 0.001f;
		}
		pDepthRow += rowSize;
	}
	return depthDataMeters;
}

ColorCameraImage ^ MetriCam2::Cameras::AstraOpenNI::CalcColor()
{
	openni::VideoFrameRef m_colorFrame;
	camData->color->readFrame(&m_colorFrame);

	if (!m_colorFrame.isValid())
	{
		log->Error("Color frame is not valid...\n");
		return nullptr;
	}

	//const RGB888Pixel* pImageRow = (const RGB888Pixel*)m_colorFrame.getData();

	Bitmap^ bitmap = gcnew Bitmap(camData->colorWidth, camData->colorHeight, System::Drawing::Imaging::PixelFormat::Format24bppRgb);

	System::Drawing::Rectangle^ imageRect = gcnew System::Drawing::Rectangle(0, 0, camData->colorWidth, camData->colorHeight);

	System::Drawing::Imaging::BitmapData^ bmpData = bitmap->LockBits(*imageRect, System::Drawing::Imaging::ImageLockMode::WriteOnly, bitmap->PixelFormat);

	const unsigned char* source = (unsigned char*)m_colorFrame.getData();
	unsigned char* target = (unsigned char*)(void*)bmpData->Scan0;
	for (int y = 0; y < camData->colorHeight; y++)
	{
		const unsigned char* sourceLine = source + y*m_colorFrame.getStrideInBytes();
		for (int x = 0; x < camData->colorWidth; x++)
		{
			target[2] = *sourceLine++;
			target[1] = *sourceLine++;
			target[0] = *sourceLine++;
			target += 3;
		}
	}


	//memcpy((void*)bmpData->Scan0, pImageRow, camData->colorWidth * camData->colorHeight * 3);

	bitmap->UnlockBits(bmpData);

	ColorCameraImage^ image = gcnew ColorCameraImage(bitmap);

	return image;
}

Point3fCameraImage ^ MetriCam2::Cameras::AstraOpenNI::CalcPoint3fImage()
{
	openni::VideoFrameRef		m_depthFrame;
	camData->depth->readFrame(&m_depthFrame);

	if (!m_depthFrame.isValid())
	{
		log->Error("Depth frame is not valid...\n");
		return nullptr;
	}

	const openni::DepthPixel* pDepthRow = (const openni::DepthPixel*)m_depthFrame.getData();
	int rowSize = m_depthFrame.getStrideInBytes() / sizeof(openni::DepthPixel);
	Point3fCameraImage^ depthDataMeters = gcnew Point3fCameraImage(m_depthFrame.getWidth(), m_depthFrame.getHeight());

	for (int y = 0; y < m_depthFrame.getHeight(); ++y)
	{
		const openni::DepthPixel* pDepth = pDepthRow;

		for (int x = 0; x < m_depthFrame.getWidth(); ++x, ++pDepth)
		{
			float a = -1;
			float b = -1;
			float c = -1;
			openni::CoordinateConverter::convertDepthToWorld(*(camData->depth), x, y, *pDepth, &a, &b, &c);

			depthDataMeters[y, x] = Point3f(a * 0.001f, b * 0.001f, c * 0.001f);
		}
		pDepthRow += rowSize;
	}
	return depthDataMeters;
}

FloatCameraImage ^ MetriCam2::Cameras::AstraOpenNI::CalcIRImage()
{
	openni::VideoFrameRef		m_IRFrame;
	camData->ir->readFrame(&m_IRFrame);

	if (!m_IRFrame.isValid())
	{
		log->Error("IR frame is not valid...\n");
		return nullptr;
	}

	const openni::Grayscale16Pixel* pIRRow = (const openni::Grayscale16Pixel*)m_IRFrame.getData();
	int rowSize = m_IRFrame.getStrideInBytes() / sizeof(openni::Grayscale16Pixel);
	FloatCameraImage^ irDataMeters = gcnew FloatCameraImage(m_IRFrame.getWidth(), m_IRFrame.getHeight(), 0.0f);

	// Compensate for offset bug: Translate infrared frame by 8 pixels in vertical direction to match infrared with depth image.
	// Leave first 8 rows black. Constructor of FloatCameraImage assigns zero to every pixel as initial value by default.
	int yTranslation = 8;

	for (int y = 0; y < m_IRFrame.getHeight() - yTranslation; ++y)
	{
		const openni::Grayscale16Pixel* pIR = pIRRow;

		for (int x = 0; x < m_IRFrame.getWidth(); ++x, ++pIR)
		{
			irDataMeters[y + yTranslation, x] = (float)*pIR;
		}
		pIRRow += rowSize;
	}
	return irDataMeters;
}

Metrilus::Util::IProjectiveTransformation^ MetriCam2::Cameras::AstraOpenNI::GetIntrinsics(String^ channelName)
{
	Metrilus::Util::IProjectiveTransformation^ result = nullptr;

	log->Info("Trying to load projective transformation from file.");
	try
	{
		result = Camera::GetIntrinsics(channelName);
	}
	catch (...) 
	{ 
		/* empty */ 
	}

	if (result == nullptr)
	{
		log->Info("Projective transformation file not found.");
		log->Info("Using Orbbec factory intrinsics as projective transformation.");

		ParamsResult res = camData->openNICam->get_cmos_params(0);

		if (channelName->Equals(ChannelNames::Intensity) || channelName->Equals(ChannelNames::ZImage))
		{
			if (res.error)
			{
				//Extracted from 3-D coordinates
				result = gcnew Metrilus::Util::ProjectiveTransformationZhang(640, 480, 570.3422f, 570.3422f, 320, 240, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
			}
			else
			{
				result = gcnew Metrilus::Util::ProjectiveTransformationZhang(
					640,
					480,
					res.params.l_intr_p[0],
					res.params.l_intr_p[1],
					res.params.l_intr_p[2],
					res.params.l_intr_p[3],
					res.params.l_k[0],
					res.params.l_k[1],
					res.params.l_k[2],
					res.params.l_k[3],
					res.params.l_k[4]);
			}
			
		}
		else if (channelName->Equals(ChannelNames::Color))
		{
			if (res.error)
			{
				// Extracted from file in Orbbec calibration tool
				result = gcnew Metrilus::Util::ProjectiveTransformationZhang(640, 480, 512.408f, 512.999, 327.955f, 236.763f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
			}
			else
			{
				result = gcnew Metrilus::Util::ProjectiveTransformationZhang(
					640,
					480,
					res.params.r_intr_p[0],
					res.params.r_intr_p[1],
					res.params.r_intr_p[2],
					res.params.r_intr_p[3],
					res.params.r_k[0],
					res.params.r_k[1],
					res.params.r_k[2],
					res.params.r_k[3],
					res.params.r_k[4]);
			}
			
		}
		else
		{
			log->Error("Unsupported channel in GetIntrinsics().");
			return nullptr;
		}

	}
	return result;
}

Metrilus::Util::RigidBodyTransformation^ MetriCam2::Cameras::AstraOpenNI::GetExtrinsics(String^ channelFromName, String^ channelToName)
{
	Metrilus::Util::RigidBodyTransformation^ result = nullptr;

	log->Info("Trying to load extrinsics from file.");
	try
	{
		result = Camera::GetExtrinsics(channelFromName, channelToName);
	}
	catch (...)
	{
		/* empty */
	}

	if (result == nullptr)
	{
		log->Info("Extrinsices file not found.");
		log->Info("Using Orbbec factory extrinsics as projective transformation.");

		ParamsResult res = camData->openNICam->get_cmos_params(0);

		Metrilus::Util::RotationMatrix^ rotMat;
		Point3f translation;

		if (res.error)
		{
			translation = Point3f(-0.0242641f, -0.000439535f, -0.000577864);

			//Extracted from file in Orbbec calibration tool
			rotMat = gcnew Metrilus::Util::RotationMatrix(
				Point3f(0.999983f, -0.00264698f, 0.00526572f),
				Point3f(0.00264383f, 0.999996f, 0.000603628f),
				Point3f(-0.0052673f, -0.000589696f, 0.999986f));
		}
		else
		{
			translation = Point3f(res.params.r2l_t[0] / 1000, res.params.r2l_t[1] / 1000, res.params.r2l_t[2] / 1000);

			rotMat = gcnew Metrilus::Util::RotationMatrix(
				Point3f(res.params.r2l_r[0], res.params.r2l_r[3], res.params.r2l_r[6]),
				Point3f(res.params.r2l_r[1], res.params.r2l_r[4], res.params.r2l_r[7]),
				Point3f(res.params.r2l_r[2], res.params.r2l_r[5], res.params.r2l_r[8]));
		}

		//TODO: Compare with own calibration, since IR-to-depth shift (in y-direction) can have an effect on the transformation
		Metrilus::Util::RigidBodyTransformation^ depthToColor = gcnew Metrilus::Util::RigidBodyTransformation(rotMat, translation);

		if ((channelFromName->Equals(ChannelNames::Intensity) || channelFromName->Equals(ChannelNames::ZImage)) && channelToName->Equals(ChannelNames::Color))
		{			
			return depthToColor;
		}
		else if (channelFromName->Equals(ChannelNames::Color) && (channelToName->Equals(ChannelNames::Intensity) || channelToName->Equals(ChannelNames::ZImage)))
		{
			// Extracted from file in Orbbec calibration tool
			return depthToColor->GetInverted();
		}
		else
		{
			log->Error("Unsupported channel combination in GetExtrinsics().");
			return nullptr;
		}

	}
	return result;
}
