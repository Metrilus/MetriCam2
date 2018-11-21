// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

#include "stdafx.h"
#include <string>
// #include <msclr\marshal_cppstd.h>

#include "OrbbecOpenNI.h"

// TODO:
// * support different resolutions (not only VGA) for channels ZImage and Intensity and check whether IR gain/exposure set feature is still working.
// * At least the mode 1280x1024 seems to be not compatible with the IR exposure set feature. For QVGA there seem to be problems to set the exposure when the Intensity channel is activated.

MetriCam2::Cameras::AstraOpenNI::AstraOpenNI()
{
	bool initSucceeded = OpenNIInit();
	if (!initSucceeded)
	{
		log->Error("Could not initialize OpenNI");
		throw gcnew System::ApplicationException("Could not initialize OpenNI" + Environment::NewLine + gcnew String(openni::OpenNI::getExtendedError()));
	}

	// Init to most reasonable values; update during ConnectImpl
	_emitterEnabled = true;
	_irFlooderEnabled = false;
}

// In C++/CLI, a Dispose() method is automatically created when implementing the deterministic destructor.
MetriCam2::Cameras::AstraOpenNI::~AstraOpenNI()
{
	if (_isDisposed)
	{
		return;
	}

	// dispose managed data
	// ...

	// call finalizer
	this->!AstraOpenNI();
	_isDisposed = true;
}

// Finalizer
MetriCam2::Cameras::AstraOpenNI::!AstraOpenNI() {
	// free unmanaged data
	try
	{
		if (IsConnected)
		{
			Disconnect(true);
		}
	}
	catch (...) {}
	OpenNIShutdown();
}

void MetriCam2::Cameras::AstraOpenNI::LogOpenNIError(String^ status) 
{
	log->Error(status + Environment::NewLine + gcnew String(openni::OpenNI::getExtendedError()));
}

bool MetriCam2::Cameras::AstraOpenNI::OpenNIInit() 
{
	int counter = System::Threading::Interlocked::Increment(_openNIInitCounter);
	log->DebugFormat("OpenNIInit - counter incremented to {0}.", counter);
	if (counter > 1) {
		// OpenNI is already intialized
		return true;
	}

	// Freshly initialize OpenNI
	openni::Status rc = openni::STATUS_OK;
	rc = openni::OpenNI::initialize();
	if (openni::Status::STATUS_OK != rc) 
	{
		System::Threading::Interlocked::Decrement(_openNIInitCounter);
		LogOpenNIError("Initialization of OpenNI failed.");
		return false;
	}

	return true;
}

bool MetriCam2::Cameras::AstraOpenNI::OpenNIShutdown() 
{
	int counter = System::Threading::Interlocked::Decrement(_openNIInitCounter);
	log->DebugFormat("OpenNIShutdown - counter decremented to {0}.", counter);

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
	const char* deviceUri;
	char serialNumber[12]; // Astra serial number has 12 characters
	System::Collections::Generic::Dictionary<String^, String^>^ serialToURI = gcnew System::Collections::Generic::Dictionary<String^, String^>();

	openni::Array<openni::DeviceInfo> deviceList;
	openni::OpenNI::enumerateDevices(&deviceList);
	int devicesCount = deviceList.getSize();

	for (int i = 0; i < devicesCount; i++) {
		// Open device by Uri
		openni::Device* device = new openni::Device;
		deviceUri = deviceList[i].getUri();
		rc = device->open(deviceUri);
		if (openni::Status::STATUS_OK != rc) {
			// CheckOpenNIError(rc, "Couldn't open device : ", deviceUris[i]);
			log->WarnFormat("GetSerialToUriMappingOfAttachedCameras: Couldn't open device {0}", gcnew String(deviceUri));
			continue;
		}

		// Read serial number
		int data_size = sizeof(serialNumber);
		device->getProperty(openni::OBEXTENSION_ID_SERIALNUMBER, serialNumber, &data_size);

		// Close device
		device->close();
		delete device;

		serialToURI[gcnew String(serialNumber)] = gcnew String(deviceUri);
	}

	OpenNIShutdown();
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
	_pCamData = new OrbbecNativeCameraData();

	const char* deviceURI = openni::ANY_DEVICE;
	if (!String::IsNullOrWhiteSpace(SerialNumber))
	{
		System::Collections::Generic::Dictionary<String^, String^>^ serialsToUris = GetSerialToUriMappingOfAttachedCameras();
		if (!serialsToUris->ContainsKey(SerialNumber))
		{
			auto msg = String::Format("No camera with requested S/N ({0}) found.", SerialNumber);
			log->Warn(msg);
			throw gcnew MetriCam2::Exceptions::ConnectionFailedException(msg);
		}
		deviceURI = marshalContext.marshal_as<const char*>(serialsToUris[SerialNumber]);
	}

	int rc = _pCamData->device.open(deviceURI);
	if (rc != openni::Status::STATUS_OK)
	{
		auto msg = String::Format("{0}: Could not init connection to device {1}.", Name, SerialNumber);
		log->Warn(msg);
		throw gcnew MetriCam2::Exceptions::ConnectionFailedException(msg);
	}
	// Read serial number
	char serialNumber[12];
	int data_size = sizeof(serialNumber);
	Device.getProperty(openni::OBEXTENSION_ID_SERIALNUMBER, serialNumber, &data_size);
	SerialNumber = gcnew String(serialNumber);

	openni::DeviceInfo dInfo = Device.getDeviceInfo();
	VendorID = dInfo.getUsbVendorId();
	ProductID = dInfo.getUsbProductId();

	char deviceType[32] = { 0 };
	int size = 32;
	Device.getProperty(openni::OBEXTENSION_ID_DEVICETYPE, deviceType, &size);
	DeviceType = gcnew String(deviceType);
	if (DeviceType->StartsWith("Orbbec "))
	{
		Model = DeviceType->Substring(7);
	}

	SetProximitySensorStatus(true); // Ensure eye-safety by turning on the proximity sensor

	// Start depth stream
	Device.setImageRegistrationMode(openni::IMAGE_REGISTRATION_OFF);

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

	// Turn Emitter on if any depth channel is active.
	// (querying from device here would return wrong value)
	// (do not use properties as they check against their current value which might be wrong)
	bool emitterEnabled = (IsChannelActive(ChannelNames::ZImage) || IsChannelActive(ChannelNames::Point3DImage));
	SetEmitterStatus(emitterEnabled);
	bool irFlooderEnabled = false; // Default to IR flooder off.
	SetIRFlooderStatus(irFlooderEnabled);
}

bool MetriCam2::Cameras::AstraOpenNI::GetEmitterStatus()
{
	// Reading the emitter status does not work yet. Check in future version of experimental SDK.
	return _emitterEnabled;
	//int laser_en = 0;
	//int size = 4;
	//Device.getProperty(openni::OBEXTENSION_ID_LASER_EN, (uint8_t*)&laser_en, &size);
	//return (bool)laser_en;
}

void MetriCam2::Cameras::AstraOpenNI::SetEmitterStatus(bool on)
{
	const int laser_en = on ? 0x01 : 0x00;
	Device.setProperty(openni::OBEXTENSION_ID_LASER_EN, (uint8_t*)&laser_en, 4);
	_emitterEnabled = on;
	log->DebugFormat("Emitter state set to: {0}", _emitterEnabled.ToString());
}

void MetriCam2::Cameras::AstraOpenNI::SetEmitterStatusAndWait(bool on)
{
	SetEmitterStatus(on);
	if (on)
	{
		WaitUntilNextValidFrame();
	}
	else
	{
		WaitUntilNextInvalidFrame();
	}
}

bool MetriCam2::Cameras::AstraOpenNI::GetProximitySensorStatus()
{
	int ldp_en = 0;
	int size = 4;
	Device.getProperty(openni::OBEXTENSION_ID_LDP_EN, (uint8_t*)&ldp_en, &size);
	return (bool)ldp_en;
}

void MetriCam2::Cameras::AstraOpenNI::SetProximitySensorStatus(bool on)
{
	const int ldp_en = on ? 0x01 : 0x00;
	Device.setProperty(openni::OBEXTENSION_ID_LDP_EN, (uint8_t*)&ldp_en, 4);
	log->DebugFormat("Proximity sensor state set to: {0}", on.ToString());
}

bool MetriCam2::Cameras::AstraOpenNI::GetIRFlooderStatus()
{
	// Reading the IrFlood status does not work yet. Check in future version of experimental SDK.
	return _irFlooderEnabled;
	//int status = 0;
	//int size = 4;
	//Device.getProperty(XN_MODULE_PROPERTY_IRFLOOD_STATE, &status, &size);
	//return (bool)status;
}

void MetriCam2::Cameras::AstraOpenNI::SetIRFlooderStatus(bool on)
{
	const int status = on ? 0x01 : 0x00;
	Device.setProperty(XN_MODULE_PROPERTY_IRFLOOD_STATE, status);
	_irFlooderEnabled = on;
	log->DebugFormat("IR flooder state set to: {0}", _irFlooderEnabled.ToString());
}

int MetriCam2::Cameras::AstraOpenNI::GetIRGain()
{
#if USE_I2C_GAIN
	std::vector<std::string> cmd_r;
	const char *argv_r[4] = {};
	int i;
	XnControlProcessingData I2C;

	argv_r[0] = "i2c";
	argv_r[1] = "read";
	argv_r[2] = "1";
	argv_r[3] = "0x35";
	for (i = 0; i < 4; i++)
	{
		cmd_r.push_back(argv_r[i]);
	}

	unsigned short gain = read_i2c(Device, cmd_r, I2C);
	return gain;
#else
	int gain = 0;
	int size = 4;
	Device.getProperty(openni::OBEXTENSION_ID_IR_GAIN, (uint8_t*)&gain, &size);
	return gain;
#endif
}

void MetriCam2::Cameras::AstraOpenNI::SetIRGain(int value)
{
	if (value < IR_Gain_MIN)
	{
		value = IR_Gain_MIN;
	}
	else if (value > IR_Gain_MAX)
	{
		value = IR_Gain_MAX;
	}
#if USE_I2C_GAIN
	std::string buf = string_format("0x%x", value);
	std::vector<std::string> cmd_r;
	const char *argv_r[5] = {};
	int i;
	XnControlProcessingData I2C;

	argv_r[0] = "i2c";
	argv_r[1] = "write";
	argv_r[2] = "1";
	argv_r[3] = "0x35";
	argv_r[4] = buf.c_str();

	for (i = 0; i < 5; i++)
	{
		cmd_r.push_back(argv_r[i]);
	}

	write_i2c(Device, cmd_r, I2C);
	log->DebugFormat("IR gain is set to: {0}", gcnew String(buf.c_str()));
#else
	int gain = value;
	int size = 4;
	Device.setProperty(openni::OBEXTENSION_ID_IR_GAIN, (uint8_t*)&gain, size);
#endif
}

int MetriCam2::Cameras::AstraOpenNI::GetIRExposure()
{
	int exposure = 0;
	int size = 4;
	Device.getProperty(openni::OBEXTENSION_ID_IR_EXP, (uint8_t*)&exposure, &size);
	return exposure;
}

void MetriCam2::Cameras::AstraOpenNI::SetIRExposure(int value)
{
	if (value < IR_Exposure_MIN)
	{
		value = IR_Exposure_MIN;
	}
	else if (value > IR_Exposure_MAX)
	{
		value = IR_Exposure_MAX;
	}
	int exposure = value;
	int size = 4;
	Device.setProperty(openni::OBEXTENSION_ID_IR_EXP, (uint8_t*)&exposure, size);
}

void MetriCam2::Cameras::AstraOpenNI::DisconnectImpl()
{
	DepthStream.destroy();
	IrStream.destroy();
	ColorStream.destroy();
	delete _pCamData;
}

void MetriCam2::Cameras::AstraOpenNI::UpdateImpl()
{
	const int NumRequestedStreams = 3;
	openni::VideoStream** ppStreams = new openni::VideoStream*[NumRequestedStreams];
	for (size_t i = 0; i < NumRequestedStreams; i++)
	{
		ppStreams[i] = NULL;
	}

	const int DepthIdx = 0, IrIdx = 1, ColorIdx = 2;
	if (IsChannelActive(ChannelNames::ZImage) || IsChannelActive(ChannelNames::Point3DImage))
	{
		ppStreams[DepthIdx] = &DepthStream;
	}
	if (IsChannelActive(ChannelNames::Intensity))
	{
		ppStreams[IrIdx] = &IrStream;
	}
	if (IsChannelActive(ChannelNames::Color))
	{
		ppStreams[ColorIdx] = &ColorStream;
	}

	int iter = 0;
	bool gotAllRequestedStreams = false;
	while (!gotAllRequestedStreams)
	{
		int changedIndex;
		openni::Status rc = openni::OpenNI::waitForAnyStream(ppStreams, NumRequestedStreams, &changedIndex, 5000);
		if (openni::STATUS_OK != rc)
		{
			if (openni::STATUS_TIME_OUT == rc)
			{
				log->ErrorFormat("{0} {1}: Wait failed: timeout", Name, SerialNumber);
			}
			else
			{
				log->ErrorFormat("{0} {1}: Wait failed: rc={2}", Name, SerialNumber, (int)rc);
			}
			return;
		}
		ppStreams[changedIndex] = NULL;

		gotAllRequestedStreams = true;
		for (size_t i = 0; i < NumRequestedStreams; i++)
		{
			if (ppStreams[i] != NULL)
			{
				gotAllRequestedStreams = false;
				break;
			}
		}
	}
}

Metrilus::Util::CameraImage ^ MetriCam2::Cameras::AstraOpenNI::CalcChannelImpl(String ^ channelName)
{
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
	return nullptr;
}

void MetriCam2::Cameras::AstraOpenNI::InitDepthStream()
{
	// Create depth stream reader
	openni::Status rc = DepthStream.create(Device, openni::SENSOR_DEPTH);
	DepthStream.setMirroringEnabled(false);
	if (openni::STATUS_OK != rc)
	{
		String^ msg = "Couldn't create depth stream:" + Environment::NewLine + gcnew String(openni::OpenNI::getExtendedError());
		log->Error(msg);
		throw gcnew Exception(msg);
	}
}

void MetriCam2::Cameras::AstraOpenNI::InitIRStream()
{
	//Init IR stream
	openni::Status rc = IrStream.create(Device, openni::SENSOR_IR);
	IrStream.setMirroringEnabled(false);
	if (openni::STATUS_OK != rc)
	{
		log->Error("Couldn't create IR stream:" + Environment::NewLine + gcnew String(openni::OpenNI::getExtendedError()));
		return;
	}
}

void MetriCam2::Cameras::AstraOpenNI::InitColorStream()
{
	//Init color stream
	openni::Status rc = ColorStream.create(Device, openni::SENSOR_COLOR);
	ColorStream.setMirroringEnabled(false);
	if (openni::STATUS_OK != rc)
	{
		log->Error("Couldn't create color stream:" + Environment::NewLine + gcnew String(openni::OpenNI::getExtendedError()));
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

		auto irGainBefore = GetIRGain();

		openni::VideoMode depthVideoMode = DepthStream.getVideoMode();
		depthVideoMode.setResolution(640, 480);
		DepthStream.setVideoMode(depthVideoMode);

		// Start depth stream
		rc = DepthStream.start();
		if (openni::STATUS_OK != rc)
		{
			log->Error("Couldn't start depth stream:" + Environment::NewLine + gcnew String(openni::OpenNI::getExtendedError()));
			DepthStream.destroy();
			return;
		}

		if (!DepthStream.isValid())
		{
			log->Error("No valid depth stream. Exiting.");
			return;
		}

		depthVideoMode = DepthStream.getVideoMode();
		_pCamData->depthWidth = depthVideoMode.getResolutionX();
		_pCamData->depthHeight = depthVideoMode.getResolutionY();

		if (this->IsConnected)
		{
			if (GetIRGain() != irGainBefore)
			{
				// Activating the depth channel resets the IR gain to the default value -> we need to restore the value that was set before.
				SetIRGain(irGainBefore);
			}
		}
	}
	else if (channelName->Equals(ChannelNames::Intensity))
	{	
		if (IsChannelActive(ChannelNames::ZImage) || IsChannelActive(ChannelNames::Point3DImage) || IsChannelActive(ChannelNames::Color))
		{
			throw gcnew Exception("IR and depth/color are not allowed to be active at the same time. Please deactivate channel \"ZImage\", \"Point3DImage\" and \"Color\" before activating channel \"Intensity\"");
		}

		//Changing the exposure is not possible if both depth and ir streams have been running parallel in one session.

		openni::VideoMode irVideoMode = IrStream.getVideoMode();
		irVideoMode.setResolution(640, 480);
		IrStream.setVideoMode(irVideoMode);

		rc = IrStream.start();
		if (openni::STATUS_OK != rc)
		{
			log->Error("Couldn't start IR stream:" + Environment::NewLine + gcnew String(openni::OpenNI::getExtendedError()));
			IrStream.destroy();
			return;
		}

		if (!IrStream.isValid())
		{
			log->Error("No valid IR stream. Exiting.");
			return;
		}

		irVideoMode = IrStream.getVideoMode();
		_pCamData->irWidth = irVideoMode.getResolutionX();
		_pCamData->irHeight = irVideoMode.getResolutionY();
	}
	else if (channelName->Equals(ChannelNames::Color))
	{
		if (IsChannelActive(ChannelNames::Intensity))
		{
			throw gcnew Exception("IR and color are not allowed to be active at the same time. Please deactivate channel \"Intensity\" before activating channel \"Color\"");
		}

		openni::VideoMode colorVideoMode = ColorStream.getVideoMode();
		//Setting the resolution to 1280/640 does not work, even if we start only the color channel (image is corrupted)
		/*colorVideoMode.setResolution(1280, 960);
		colorVideoMode.setFps(7);*/
		colorVideoMode.setResolution(640, 480);
		ColorStream.setVideoMode(colorVideoMode);

		rc = ColorStream.start();
		if (openni::STATUS_OK != rc)
		{
			log->Error("Couldn't start color stream:" + Environment::NewLine + gcnew String(openni::OpenNI::getExtendedError()));
			ColorStream.destroy();
			return;
		}

		if (!ColorStream.isValid())
		{
			log->Error("No valid color stream. Exiting.");
			return;
		}

		colorVideoMode = ColorStream.getVideoMode();
		_pCamData->colorWidth = colorVideoMode.getResolutionX();
		_pCamData->colorHeight = colorVideoMode.getResolutionY();
	}

	log->LeaveMethod();
}

void MetriCam2::Cameras::AstraOpenNI::DeactivateChannelImpl(String^ channelName)
{
	if (channelName->Equals(ChannelNames::ZImage) || channelName->Equals(ChannelNames::Point3DImage))
	{
		DepthStream.stop();
	}
	else if (channelName->Equals(ChannelNames::Intensity))
	{
		IrStream.stop();
	}
	else if (channelName->Equals(ChannelNames::Color))
	{
		ColorStream.stop();
	}
}

FloatCameraImage ^ MetriCam2::Cameras::AstraOpenNI::CalcZImage()
{
	if (!DepthStream.isValid())
	{
		return nullptr;
	}
	openni::VideoFrameRef depthFrame;
	DepthStream.readFrame(&depthFrame);

	if (!depthFrame.isValid())
	{
		log->Error("Depth frame is not valid...");
		return nullptr;
	}

	const openni::DepthPixel* pDepthRow = (const openni::DepthPixel*)depthFrame.getData();
	const int rowSize = depthFrame.getStrideInBytes() / sizeof(openni::DepthPixel);
	FloatCameraImage^ depthDataMeters = gcnew FloatCameraImage(depthFrame.getWidth(), depthFrame.getHeight());
	depthDataMeters->ChannelName = ChannelNames::ZImage;

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
	if (!ColorStream.isValid())
	{
		return nullptr;
	}
	openni::VideoFrameRef colorFrame;
	ColorStream.readFrame(&colorFrame);

	if (!colorFrame.isValid())
	{
		log->Error("Color frame is not valid...");
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
	image->ChannelName = ChannelNames::Color;

	return image;
}

Point3fCameraImage ^ MetriCam2::Cameras::AstraOpenNI::CalcPoint3fImage()
{
	if (!DepthStream.isValid())
	{
		return nullptr;
	}
	openni::VideoFrameRef depthFrame;
	DepthStream.readFrame(&depthFrame);

	if (!depthFrame.isValid())
	{
		log->Error("Depth frame is not valid...");
		return nullptr;
	}

	const openni::DepthPixel* pDepthRow = (const openni::DepthPixel*)depthFrame.getData();
	const int rowSize = depthFrame.getStrideInBytes() / sizeof(openni::DepthPixel);
	Point3fCameraImage^ pointsImage = gcnew Point3fCameraImage(depthFrame.getWidth(), depthFrame.getHeight());
	pointsImage->ChannelName = ChannelNames::Point3DImage;

	for (int y = 0; y < depthFrame.getHeight(); ++y)
	{
		const openni::DepthPixel* pDepth = pDepthRow;

		for (int x = 0; x < depthFrame.getWidth(); ++x, ++pDepth)
		{
			float a = -1;
			float b = -1;
			float c = -1;
			openni::CoordinateConverter::convertDepthToWorld(DepthStream, x, y, *pDepth, &a, &b, &c);

			pointsImage[y, x] = Point3f(a, b, c) * 0.001f;
		}
		pDepthRow += rowSize;
	}
	return pointsImage;
}

FloatCameraImage ^ MetriCam2::Cameras::AstraOpenNI::CalcIRImage()
{
	if (!IrStream.isValid())
	{
		return nullptr;
	}
	openni::VideoFrameRef irFrame;
	IrStream.readFrame(&irFrame);

	if (!irFrame.isValid())
	{
		log->Error("IR frame is not valid...");
		return nullptr;
	}

	const openni::Grayscale16Pixel* pIRRow = (const openni::Grayscale16Pixel*)irFrame.getData();
	const int rowSize = irFrame.getStrideInBytes() / sizeof(openni::Grayscale16Pixel);
	FloatCameraImage^ irData = gcnew FloatCameraImage(irFrame.getWidth(), irFrame.getHeight(), 0.0f);
	irData->ChannelName = ChannelNames::Intensity;

	// Compensate for offset between IR and Distance images:
	// Translate infrared frame by 16 pixels in vertical direction to match infrared with depth image.
	const int yTranslation = 16;

	// skip first yTranslation rows
	if (yTranslation > 0)
	{
		pIRRow += rowSize * yTranslation;
	}
	int dataY = yTranslation;
	int imgY = 0;
	for (; imgY < irFrame.getHeight() && dataY < irFrame.getHeight(); ++imgY, ++dataY)
	{
		const openni::Grayscale16Pixel* pIR = pIRRow;
		for (int x = 0; x < irFrame.getWidth(); ++x, ++pIR)
		{
			irData[imgY, x] = (float)*pIR;
		}
		pIRRow += rowSize;
	}
	return irData;
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
	OBCameraParams params;
	int dataSize = sizeof(OBCameraParams);
	Device.getProperty(openni::OBEXTENSION_ID_CAM_PARAMS, (uint8_t*)&params, &dataSize);

	Metrilus::Util::ProjectiveTransformationZhang^ pt = nullptr;

	if (channelName->Equals(ChannelNames::Intensity) || channelName->Equals(ChannelNames::ZImage))
	{
		pt = gcnew Metrilus::Util::ProjectiveTransformationZhang(
			640, 480,
			params.l_intr_p[0], params.l_intr_p[1],
			params.l_intr_p[2], params.l_intr_p[3],
			params.l_k[0], params.l_k[1], params.l_k[2],
			params.l_k[3], params.l_k[4]);
	}

	if (channelName->Equals(ChannelNames::Color))
	{
		pt = gcnew Metrilus::Util::ProjectiveTransformationZhang(
			640, 480,
			params.r_intr_p[0], params.r_intr_p[1],
			params.r_intr_p[2], params.r_intr_p[3],
			params.r_k[0], params.r_k[1], params.r_k[2],
			params.r_k[3], params.r_k[4]);
	}

	if (nullptr == pt)
	{
		log->Error(String::Format("Unsupported channel in GetIntrinsics(): {0}", channelName));
	}
	else
	{
		pt->CameraSerial = SerialNumber;
	}

	return pt;
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
	OBCameraParams params;
	int dataSize = sizeof(OBCameraParams);
	Device.getProperty(openni::OBEXTENSION_ID_CAM_PARAMS, (uint8_t*)&params, &dataSize);

	Point3f translation = Point3f(params.r2l_t[0] / 1000, params.r2l_t[1] / 1000, params.r2l_t[2] / 1000);
	Metrilus::Util::RotationMatrix^ rotMat = gcnew Metrilus::Util::RotationMatrix(
		Point3f(params.r2l_r[0], params.r2l_r[3], params.r2l_r[6]),
		Point3f(params.r2l_r[1], params.r2l_r[4], params.r2l_r[7]),
		Point3f(params.r2l_r[2], params.r2l_r[5], params.r2l_r[8]));

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

	log->ErrorFormat("Unsupported channel combination in GetExtrinsics(): {0} -> {1}", channelFromName, channelToName);
	return nullptr;
}

void MetriCam2::Cameras::AstraOpenNI::WaitUntilNextValidFrame()
{
	int numFramesWaited = 0;
	FloatCameraImage^ frame;
	if (IsChannelActive(ChannelNames::ZImage))
	{
		do
		{
			Update();
			frame = (FloatCameraImage^)CalcChannel(ChannelNames::ZImage);
			numFramesWaited++;
		} while (!IsDepthFrameValid_NumberNonZeros(frame, 30));
	}
	else if (IsChannelActive(ChannelNames::Intensity))
	{

	}
	log->DebugFormat("Waited for {0} frames until first valid frame", numFramesWaited);
}

void MetriCam2::Cameras::AstraOpenNI::WaitUntilNextInvalidFrame()
{
	int numFramesWaited = 0;
	FloatCameraImage^ frame;
	if (IsChannelActive(ChannelNames::ZImage))
	{
		do
		{
			Update();
			frame = (FloatCameraImage^)CalcChannel(ChannelNames::ZImage);
			numFramesWaited++;
		} while (IsDepthFrameValid_NumberNonZeros(frame, 30));
	}
	else if (IsChannelActive(ChannelNames::Intensity))
	{

	}
	log->DebugFormat("Waited for {0} frames until first invalid frame", numFramesWaited);
}

[MethodImpl(MethodImplOptions::AggressiveInlining)]
bool MetriCam2::Cameras::AstraOpenNI::IsDepthFrameValid_MinimumMean(FloatCameraImage^ img)
{
	return IsDepthFrameValid_MinimumMean(img, 0.0f);
}
[MethodImpl(MethodImplOptions::AggressiveInlining)]
bool MetriCam2::Cameras::AstraOpenNI::IsDepthFrameValid_MinimumMean(FloatCameraImage^ img, float threshold)
{
	float sum = 0;
	for (int y = 0; y < img->Height; y++)
	{
		for (int x = 0; x < img->Width; x++)
		{
			sum += img[y, x];
		}
	}
	return sum > threshold;
}

[MethodImpl(MethodImplOptions::AggressiveInlining)]
bool MetriCam2::Cameras::AstraOpenNI::IsDepthFrameValid_NumberNonZeros(FloatCameraImage^ img)
{
	return IsDepthFrameValid_NumberNonZeros(img, 25);
}
[MethodImpl(MethodImplOptions::AggressiveInlining)]
bool MetriCam2::Cameras::AstraOpenNI::IsDepthFrameValid_NumberNonZeros(FloatCameraImage^ img, int thresholdPercentage)
{
	int numPixels = img->Height * img->Width;
	int numNonZeros = 0;
	for (int y = 0; y < img->Height; y++)
	{
		for (int x = 0; x < img->Width; x++)
		{
			if (img[y, x] > 0.0f)
			{
				numNonZeros++;
			}
		}
	}
	int ratio = (int)(numNonZeros * 100.0f / numPixels);
	return ratio > thresholdPercentage;
}

#if USE_I2C_GAIN
bool atoi2(const char* str, int* pOut)
{
	int output = 0;
	int base = 10;
	int start = 0;

	if (strlen(str) > 1 && str[0] == '0' && str[1] == 'x')
	{
		start = 2;
		base = 16;
	}

	for (size_t i = start; i < strlen(str); i++)
	{
		output *= base;
		if (str[i] >= '0' && str[i] <= '9')
			output += str[i] - '0';
		else if (base == 16 && str[i] >= 'a' && str[i] <= 'f')
			output += 10 + str[i] - 'a';
		else if (base == 16 && str[i] >= 'A' && str[i] <= 'F')
			output += 10 + str[i] - 'A';
		else
			return false;
	}
	*pOut = output;
	return true;
}

unsigned short read_i2c(openni::Device& device, std::vector<std::string>& Command, XnControlProcessingData& I2C)
{
	if (Command.size() != 4)
	{
		std::cout << "Usage: " << Command[0] << " " << Command[1] << " <cmos> <register>" << std::endl;
		return true;
	}

	int nRegister;
	if (!atoi2(Command[3].c_str(), &nRegister))
	{
		printf("Don't understand %s as a register\n", Command[3].c_str());
		return true;
	}
	I2C.nRegister = (unsigned short)nRegister;

	int nParam = 0;

	int command;
	if (!atoi2(Command[2].c_str(), &command))
	{
		std::cout << "cmos must be 0/1" << std::endl;
		return true;
	}

	if (command == 1)
		nParam = XN_MODULE_PROPERTY_DEPTH_CONTROL;
	else if (command == 0)
		nParam = XN_MODULE_PROPERTY_IMAGE_CONTROL;
	else
	{
		std::cout << "cmos must be 0/1" << std::endl;
		return true;
	}

	if (device.getProperty(nParam, &I2C) != openni::STATUS_OK)
	{
		std::cout << "getProperty failed!" << std::endl;
		return false;
	}

	std::cout << "I2C(" << command << ")[0x" << std::hex << I2C.nRegister << "] = 0x" << std::hex << I2C.nValue << std::endl;

	return I2C.nValue;
}

bool write_i2c(openni::Device& device, std::vector<std::string>& Command, XnControlProcessingData& I2C)
{
	if (Command.size() != 5)
	{
		std::cout << "Usage: " << Command[0] << " " << Command[1] << " <cmos> <register> <value>" << std::endl;
		return true;
	}

	int nRegister, nValue;
	if (!atoi2(Command[3].c_str(), &nRegister))
	{
		printf("Don't understand %s as a register\n", Command[3].c_str());
		return true;
	}
	if (!atoi2(Command[4].c_str(), &nValue))
	{
		printf("Don't understand %s as a value\n", Command[4].c_str());
		return true;
	}
	I2C.nRegister = (unsigned short)nRegister;
	I2C.nValue = (unsigned short)nValue;

	int nParam = 0;

	int command;
	if (!atoi2(Command[2].c_str(), &command))
	{
		printf("cmos should be 0 (depth) or 1 (image)\n");
		return true;
	}

	if (command == 1)
		nParam = XN_MODULE_PROPERTY_DEPTH_CONTROL;
	else if (command == 0)
		nParam = XN_MODULE_PROPERTY_IMAGE_CONTROL;
	else
	{
		std::cout << "cmos must be 0/1" << std::endl;
		return true;
	}

	openni::Status rc = device.setProperty(nParam, I2C);
	if (rc != openni::STATUS_OK)
	{
		printf("%s\n", openni::OpenNI::getExtendedError());
	}

	return true;
}

template<typename ... Args>
std::string string_format(const std::string& format, Args ... args)
{
	size_t size = snprintf(nullptr, 0, format.c_str(), args ...) + 1; // Extra space for '\0'
	std::unique_ptr<char[]> buf(new char[size]);
	snprintf(buf.get(), size, format.c_str(), args ...);
	return std::string(buf.get(), buf.get() + size - 1); // We don't want the '\0' inside
}
#endif
