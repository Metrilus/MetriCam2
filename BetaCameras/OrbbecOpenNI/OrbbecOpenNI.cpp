// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

#include "stdafx.h"
#include <string>
// #include <msclr\marshal_cppstd.h>

#include "OrbbecOpenNI.h"

#define MAX_DEVICES 20  // There is no limitations, we choose 20 as "reasonable" value in real use case.

// TODO:
// * support different resolutions (not only VGA) for channels ZImage and Intensity and check whether IR gain/exposure set feature is still working.
// * At least the mode 1280x1024 seems to be not compatible with the IR exposure set feature. For QVGA there seem to be problems to set the exposure when the Intensity channel is activated.

MetriCam2::Cameras::AstraOpenNI::AstraOpenNI()
{
	_pCamData = new OrbbecNativeCameraData();
	_pCamData->openNICam = new cmd();

	_pCamData->depth = new openni::VideoStream();
	_pCamData->ir = new openni::VideoStream();
	_pCamData->color = new openni::VideoStream();

	// Init to most reasonable values; update during ConnectImpl
	_emitterEnabled = true;
	_irFlooderEnabled = false;
}

MetriCam2::Cameras::AstraOpenNI::~AstraOpenNI()
{
	//TODO: clean up camData->openNICam, camData->depth and camData->ir
	delete _pCamData;
}

void MetriCam2::Cameras::AstraOpenNI::LogOpenNIError(String^ status) 
{
	log->Error(status + "\n" + gcnew String(openni::OpenNI::getExtendedError()));
}

bool MetriCam2::Cameras::AstraOpenNI::OpenNIInit() 
{
	int counter = System::Threading::Interlocked::Increment(_openNIInitCounter);
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
	int counter = System::Threading::Interlocked::Decrement(_openNIInitCounter);

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

	const char* deviceURI = openni::ANY_DEVICE;
	if (!String::IsNullOrWhiteSpace(SerialNumber))
	{
		System::Collections::Generic::Dictionary<String^, String^>^ serialsToUris = GetSerialToUriMappingOfAttachedCameras();
		if (!serialsToUris->ContainsKey(SerialNumber))
		{
			throw gcnew MetriCam2::Exceptions::ConnectionFailedException(String::Format("No camera with requested S/N ({0}) found.", SerialNumber));
		}
		msclr::interop::marshal_context marshalContext;
		deviceURI = marshalContext.marshal_as<const char*>(serialsToUris[SerialNumber]);
	}

	int rc = _pCamData->openNICam->init(deviceURI);
	if (rc != openni::Status::STATUS_OK)
	{
		throw gcnew MetriCam2::Exceptions::ConnectionFailedException(String::Format("Could not init connection to device {0}.", SerialNumber));
	}
	VendorID = _pCamData->openNICam->m_vid;
	ProductID = _pCamData->openNICam->m_pid;
	_pCamData->openNICam->ldp_set(true); //Ensure eye-safety by turning on the proximity sensor

	// Start depth stream
	_pCamData->openNICam->device.setImageRegistrationMode(openni::IMAGE_REGISTRATION_OFF);

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

	_irGain = GetIRGain();
	// Turn Emitter on if any depth channel is active.
	// (querying from device here would return wrong value)
	EmitterEnabled = (IsChannelActive(ChannelNames::ZImage) || IsChannelActive(ChannelNames::Point3DImage));
	IRFlooderEnabled = false; // Default to IR flooder off.
}

void MetriCam2::Cameras::AstraOpenNI::SetEmitterStatus(bool on)
{
	if (_pCamData->openNICam->m_vid != 0x1d27) //Check if our device is not an Asus-Carmine device
	{
		if (_pCamData->openNICam->ldp_set(on) != openni::STATUS_OK)
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

	if (_pCamData->openNICam->emitter_set(on) != openni::STATUS_OK)
	{
		LogOpenNIError("Emitter set failed");
	}
}

String^ MetriCam2::Cameras::AstraOpenNI::GetEmitterStatus()
{
	LaserStatus status;
	if (_pCamData->openNICam->emitter_get(status) != openni::STATUS_OK)
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

	if (_pCamData->openNICam->ir_flood_set(on) != openni::STATUS_OK)
	{
		LogOpenNIError("ir flooder set failed");
	}
}

String^ MetriCam2::Cameras::AstraOpenNI::GetIRFlooderStatus()
{
	IrFloodLedStatus status;
	if (_pCamData->openNICam->ir_flood_get(status) != openni::STATUS_OK)
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

	if (!_pCamData->openNICam->ir_gain_set(value))
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
	_pCamData->openNICam->ir_gain_get();
	return _pCamData->openNICam->m_I2CReg;
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

	if (_pCamData->openNICam->ir_exposure_set(irExposure) != openni::STATUS_OK)
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
		_pCamData->ir->start();
		if (_pCamData->ir->isValid())
		{
			VideoMode videomode = _pCamData->ir->getVideoMode();
			videomode.setPixelFormat(openni::PIXEL_FORMAT_GRAY16);
			videomode.setResolution(640, 480);
			_pCamData->ir->setVideoMode(videomode);
		}
		_pCamData->ir->stop();
	}
	if (!IsChannelActive(ChannelNames::ZImage))
	{
		_pCamData->depth->start();
		if (_pCamData->depth->isValid())
		{
			VideoMode videoMode = _pCamData->depth->getVideoMode();
			videoMode.setResolution(640, 480);
			_pCamData->depth->setVideoMode(videoMode);
		}
		_pCamData->depth->stop();
	}
}

unsigned int MetriCam2::Cameras::AstraOpenNI::GetIRExposure()
{
	unsigned int exposure;
	if (_pCamData->openNICam->ir_exposure_get(exposure) != openni::STATUS_OK)
	{
		LogOpenNIError("Get IR exposure failed");
	}
	return exposure;
}

void MetriCam2::Cameras::AstraOpenNI::DisconnectImpl()
{
	_pCamData->depth->destroy();
	_pCamData->ir->destroy();
	_pCamData->color->destroy();
	OpenNIShutdown();
}

void MetriCam2::Cameras::AstraOpenNI::UpdateImpl()
{
	openni::VideoStream** ppStreams = new openni::VideoStream*[2];
	ppStreams[0] = _pCamData->depth;
	ppStreams[1] = _pCamData->ir;
	//ppStreams[2] = _pCamData->color;

	int changedIndex;
	openni::Status rc = openni::OpenNI::waitForAnyStream(ppStreams, 2, &changedIndex);

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
	openni::Status rc = _pCamData->depth->create(_pCamData->openNICam->device, openni::SENSOR_DEPTH);
	_pCamData->depth->setMirroringEnabled(false);
	if (openni::STATUS_OK != rc)
	{
		throw gcnew Exception("Couldn't find depth stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
	}
}

void MetriCam2::Cameras::AstraOpenNI::InitIRStream()
{
	//Init IR stream
	openni::Status rc = _pCamData->ir->create(_pCamData->openNICam->device, openni::SENSOR_IR);
	_pCamData->ir->setMirroringEnabled(false);
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
	openni::Status rc = _pCamData->color->create(_pCamData->openNICam->device, openni::SENSOR_COLOR);
	_pCamData->color->setMirroringEnabled(false);
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

		openni::VideoMode depthVideoMode = _pCamData->depth->getVideoMode();
		depthVideoMode.setResolution(640, 480);
		_pCamData->depth->setVideoMode(depthVideoMode);

		// Start depth stream
		rc = _pCamData->depth->start();
		if (openni::STATUS_OK != rc)
		{
			log->Error("Couldn't start depth stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
			_pCamData->depth->destroy();
			openni::OpenNI::shutdown();
			return;
		}

		if (!_pCamData->depth->isValid())
		{
			log->Error("No valid depth stream. Exiting\n");
			openni::OpenNI::shutdown();
			return;
		}

		depthVideoMode = _pCamData->depth->getVideoMode();
		_pCamData->depthWidth = depthVideoMode.getResolutionX();
		_pCamData->depthHeight = depthVideoMode.getResolutionY();

		if (this->IsConnected)
		{
			//Activating the depth channel resets the IR gain to the default value -> we need to restore the value that was set before.
			SetIRGain(_irGain);
		}
	}
	else if (channelName->Equals(ChannelNames::Intensity))
	{	
		if (IsChannelActive(ChannelNames::ZImage) || IsChannelActive(ChannelNames::Point3DImage) || IsChannelActive(ChannelNames::Color))
		{
			throw gcnew Exception("IR and depth/color are not allowed to be active at the same time. Please deactivate channel \"ZImage\", \"Point3DImage\" and \"Color\" before activating channel \"Intensity\"");
		}

		//Changing the exposure is not possible if both depth and ir streams have been running parallel in one session.

		openni::VideoMode irVideoMode = _pCamData->ir->getVideoMode();
		irVideoMode.setResolution(640, 480);
		_pCamData->ir->setVideoMode(irVideoMode);

		rc = _pCamData->ir->start();
		if (openni::STATUS_OK != rc)
		{
			log->Error("Couldn't start IR stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
			_pCamData->ir->destroy();
			openni::OpenNI::shutdown();
			return;
		}

		if (!_pCamData->ir->isValid())
		{
			log->Error("No valid IR stream. Exiting\n");
			openni::OpenNI::shutdown();
			return;
		}

		irVideoMode = _pCamData->ir->getVideoMode();
		_pCamData->irWidth = irVideoMode.getResolutionX();
		_pCamData->irHeight = irVideoMode.getResolutionY();
	}
	else if (channelName->Equals(ChannelNames::Color))
	{
		if (IsChannelActive(ChannelNames::Intensity))
		{
			throw gcnew Exception("IR and color are not allowed to be active at the same time. Please deactivate channel \"Intensity\" before activating channel \"Color\"");
		}

		openni::VideoMode colorVideoMode = _pCamData->color->getVideoMode();
		//Setting the resolution to 1280/640 does not work, even if we start only the color channel (image is corrupted)
		/*colorVideoMode.setResolution(1280, 960);
		colorVideoMode.setFps(7);*/
		colorVideoMode.setResolution(640, 480);
		_pCamData->color->setVideoMode(colorVideoMode);

		rc = _pCamData->color->start();
		if (openni::STATUS_OK != rc)
		{
			log->Error("Couldn't start color stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
			_pCamData->color->destroy();
			openni::OpenNI::shutdown();
			return;
		}

		if (!_pCamData->color->isValid())
		{
			log->Error("No valid color stream. Exiting\n");
			openni::OpenNI::shutdown();
			return;
		}

		colorVideoMode = _pCamData->color->getVideoMode();
		_pCamData->colorWidth = colorVideoMode.getResolutionX();
		_pCamData->colorHeight = colorVideoMode.getResolutionY();
	}

	log->LeaveMethod();
}

void MetriCam2::Cameras::AstraOpenNI::DeactivateChannelImpl(String^ channelName)
{
	if (channelName->Equals(ChannelNames::ZImage) || channelName->Equals(ChannelNames::Point3DImage))
	{
		_pCamData->depth->stop();
	}
	else if (channelName->Equals(ChannelNames::Intensity))
	{
		_pCamData->ir->stop();
	}
	else if (channelName->Equals(ChannelNames::Color))
	{
		_pCamData->color->stop();
	}
}

FloatCameraImage ^ MetriCam2::Cameras::AstraOpenNI::CalcZImage()
{
	openni::VideoFrameRef depthFrame;
	_pCamData->depth->readFrame(&depthFrame);

	if (!depthFrame.isValid())
	{
		log->Error("Depth frame is not valid...\n");
		return nullptr;
	}

	const openni::DepthPixel* pDepthRow = (const openni::DepthPixel*)depthFrame.getData();
	int rowSize = depthFrame.getStrideInBytes() / sizeof(openni::DepthPixel);
	FloatCameraImage^ depthDataMeters = gcnew FloatCameraImage(depthFrame.getWidth(), depthFrame.getHeight());

	for (int y = 0; y < depthFrame.getHeight(); ++y)
	{
		const openni::DepthPixel* pDepth = pDepthRow;
		for (int x = 0; x < depthFrame.getWidth(); ++x, ++pDepth)
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
	openni::VideoFrameRef colorFrame;
	_pCamData->color->readFrame(&colorFrame);

	if (!colorFrame.isValid())
	{
		log->Error("Color frame is not valid...\n");
		return nullptr;
	}

	Bitmap^ bitmap = gcnew Bitmap(_pCamData->colorWidth, _pCamData->colorHeight, System::Drawing::Imaging::PixelFormat::Format24bppRgb);

	System::Drawing::Rectangle^ imageRect = gcnew System::Drawing::Rectangle(0, 0, _pCamData->colorWidth, _pCamData->colorHeight);

	System::Drawing::Imaging::BitmapData^ bmpData = bitmap->LockBits(*imageRect, System::Drawing::Imaging::ImageLockMode::WriteOnly, bitmap->PixelFormat);

	const unsigned char* source = (unsigned char*)colorFrame.getData();
	unsigned char* target = (unsigned char*)(void*)bmpData->Scan0;
	for (int y = 0; y < _pCamData->colorHeight; y++)
	{
		const unsigned char* sourceLine = source + y * colorFrame.getStrideInBytes();
		for (int x = 0; x < _pCamData->colorWidth; x++)
		{
			target[2] = *sourceLine++;
			target[1] = *sourceLine++;
			target[0] = *sourceLine++;
			target += 3;
		}
	}

	bitmap->UnlockBits(bmpData);

	ColorCameraImage^ image = gcnew ColorCameraImage(bitmap);

	return image;
}

Point3fCameraImage ^ MetriCam2::Cameras::AstraOpenNI::CalcPoint3fImage()
{
	openni::VideoFrameRef depthFrame;
	_pCamData->depth->readFrame(&depthFrame);

	if (!depthFrame.isValid())
	{
		log->Error("Depth frame is not valid...\n");
		return nullptr;
	}

	const openni::DepthPixel* pDepthRow = (const openni::DepthPixel*)depthFrame.getData();
	int rowSize = depthFrame.getStrideInBytes() / sizeof(openni::DepthPixel);
	Point3fCameraImage^ depthDataMeters = gcnew Point3fCameraImage(depthFrame.getWidth(), depthFrame.getHeight());

	for (int y = 0; y < depthFrame.getHeight(); ++y)
	{
		const openni::DepthPixel* pDepth = pDepthRow;

		for (int x = 0; x < depthFrame.getWidth(); ++x, ++pDepth)
		{
			float a = -1;
			float b = -1;
			float c = -1;
			openni::CoordinateConverter::convertDepthToWorld(*(_pCamData->depth), x, y, *pDepth, &a, &b, &c);

			depthDataMeters[y, x] = Point3f(a * 0.001f, b * 0.001f, c * 0.001f);
		}
		pDepthRow += rowSize;
	}
	return depthDataMeters;
}

FloatCameraImage ^ MetriCam2::Cameras::AstraOpenNI::CalcIRImage()
{
	openni::VideoFrameRef irFrame;
	_pCamData->ir->readFrame(&irFrame);

	if (!irFrame.isValid())
	{
		log->Error("IR frame is not valid...\n");
		return nullptr;
	}

	const openni::Grayscale16Pixel* pIRRow = (const openni::Grayscale16Pixel*)irFrame.getData();
	int rowSize = irFrame.getStrideInBytes() / sizeof(openni::Grayscale16Pixel);
	FloatCameraImage^ irDataMeters = gcnew FloatCameraImage(irFrame.getWidth(), irFrame.getHeight(), 0.0f);

	// Compensate for offset bug: Translate infrared frame by 8 pixels in vertical direction to match infrared with depth image.
	// Leave first 8 rows black. Constructor of FloatCameraImage assigns zero to every pixel as initial value by default.
	int yTranslation = 8;

	for (int y = 0; y < irFrame.getHeight() - yTranslation; ++y)
	{
		const openni::Grayscale16Pixel* pIR = pIRRow;

		for (int x = 0; x < irFrame.getWidth(); ++x, ++pIR)
		{
			irDataMeters[y + yTranslation, x] = (float)*pIR;
		}
		pIRRow += rowSize;
	}
	return irDataMeters;
}

Metrilus::Util::IProjectiveTransformation^ MetriCam2::Cameras::AstraOpenNI::GetIntrinsics(String^ channelName)
{
	log->Info("Trying to load projective transformation from file.");
	try
	{
		return Camera::GetIntrinsics(channelName);
	}
	catch (...) 
	{ 
		/* empty */ 
	}

	log->Info("Projective transformation file not found.");
	log->Info("Using Orbbec factory intrinsics as projective transformation.");

	ParamsResult res = _pCamData->openNICam->get_cmos_params(0);

	if (channelName->Equals(ChannelNames::Intensity) || channelName->Equals(ChannelNames::ZImage))
	{
		if (res.error)
		{
			//Extracted from 3-D coordinates
			return gcnew Metrilus::Util::ProjectiveTransformationZhang(640, 480, 570.3422f, 570.3422f, 320, 240, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
		}
		return gcnew Metrilus::Util::ProjectiveTransformationZhang(
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

	if (channelName->Equals(ChannelNames::Color))
	{
		if (res.error)
		{
			// Extracted from file in Orbbec calibration tool
			return gcnew Metrilus::Util::ProjectiveTransformationZhang(640, 480, 512.408f, 512.999f, 327.955f, 236.763f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
		}
		return gcnew Metrilus::Util::ProjectiveTransformationZhang(
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

	log->Error(String::Format("Unsupported channel in GetIntrinsics(): {0}", channelName));
	return nullptr;
}

Metrilus::Util::RigidBodyTransformation^ MetriCam2::Cameras::AstraOpenNI::GetExtrinsics(String^ channelFromName, String^ channelToName)
{
	log->Info("Trying to load extrinsics from file.");
	try
	{
		return Camera::GetExtrinsics(channelFromName, channelToName);
	}
	catch (...)
	{
		/* empty */
	}

	log->Info("Extrinsices file not found.");
	log->Info("Using Orbbec factory extrinsics as projective transformation.");

	ParamsResult res = _pCamData->openNICam->get_cmos_params(0);

	Metrilus::Util::RotationMatrix^ rotMat;
	Point3f translation;

	if (res.error)
	{
		translation = Point3f(-0.0242641f, -0.000439535f, -0.000577864f);

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
	if (channelFromName->Equals(ChannelNames::Color) && (channelToName->Equals(ChannelNames::Intensity) || channelToName->Equals(ChannelNames::ZImage)))
	{
		// Extracted from file in Orbbec calibration tool
		return depthToColor->GetInverted();
	}

	log->Error("Unsupported channel combination in GetExtrinsics().");
	return nullptr;
}
