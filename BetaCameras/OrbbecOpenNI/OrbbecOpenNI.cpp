// This is the main DLL file.

#include "stdafx.h"
#include <string>

#include "OrbbecOpenNI.h"

#define MAX_DEVICES 20  // There is no limitations, we choose 20 as "reasonable" value in real use case.

// TODO:
// * static retrieval of list of serial numbers
// * custom infrared channel
// * switching emitter on and off

MetriCam2::Cameras::AstraOpenNI::AstraOpenNI()
{
	camData = new OrbbecNativeCameraData();
	log = gcnew MetriLog();

	camData->device = new openni::Device();
	camData->depth = new openni::VideoStream();
	camData->ir = new openni::VideoStream();
}

MetriCam2::Cameras::AstraOpenNI::~AstraOpenNI()
{
	delete camData->device;
	delete camData->depth;
	delete camData->ir;
	delete camData;
}

void MetriCam2::Cameras::AstraOpenNI::LogOpenNIError(String^ status) {
	log->Error(status + "\n" + gcnew String(openni::OpenNI::getExtendedError()));
}

bool MetriCam2::Cameras::AstraOpenNI::OpenNIInit() {
	int counter = System::Threading::Interlocked::Increment(openNIInitCounter);
	if (counter > 1) {
		// OpenNI is already intialized
		return true;
	}

	// Freshly initialize OpenNI
	openni::Status rc = openni::STATUS_OK;
	rc = openni::OpenNI::initialize();
	if (openni::Status::STATUS_OK != rc) {
		LogOpenNIError("Initialization of OpenNI failed.");
		return false;
	}

	return true;
}

bool MetriCam2::Cameras::AstraOpenNI::OpenNIShutdown() {
	int counter = System::Threading::Interlocked::Decrement(openNIInitCounter);

	if (0 != counter) {
		// Someone is still using OpenNI
		return true;
	}
	
	openni::Status rc = openni::STATUS_OK;
	openni::OpenNI::shutdown();
	if (openni::Status::STATUS_OK != rc) {
		LogOpenNIError("Shutdown of OpenNI failed");
		return false;
	}
	return true;
}

array<String^, 1>^ MetriCam2::Cameras::AstraOpenNI::GetSerialNumbersOfAttachedCameras()
{
	bool initSucceeded = OpenNIInit();
	if (!initSucceeded) {
		// Error already logged
		return nullptr;
	}

	openni::VideoStream    depthStreams[MAX_DEVICES];
	const char*            deviceUris[MAX_DEVICES];
	char                   serialNumbers[MAX_DEVICES][12]; // Astra serial number has 12 numbers

	// Enumerate devices
	openni::Status rc = openni::STATUS_OK;
	openni::Array<openni::DeviceInfo> deviceList;
	openni::OpenNI::enumerateDevices(&deviceList);
	int devicesCount = deviceList.getSize();

	if (devicesCount >= MAX_DEVICES)
	{
		log->Error("The number of supported devices is limited.");
		OpenNIShutdown();
		return nullptr;
	}

	array<String^>^ serialNumbersRet = gcnew array<String^>(devicesCount);
	for (int i = 0; i < devicesCount; i++) {
		// Open device by Uri
		openni::Device* device = new openni::Device;
		deviceUris[i] = deviceList[i].getUri();

		rc = device->open(deviceUris[i]);
		if (openni::Status::STATUS_OK != rc) {
			LogOpenNIError("Could not open device " + gcnew String(deviceUris[i]));
			OpenNIShutdown();
			return nullptr;
		}

		rc = depthStreams[i].create(*device, openni::SENSOR_DEPTH);
		if (openni::Status::STATUS_OK != rc) {
			LogOpenNIError("Couldn't create stream on device " + gcnew String(deviceUris[i]));
			OpenNIShutdown();
			return nullptr;
		}

		// Read serial number
		int data_size = sizeof(serialNumbers[i]);
		device->getProperty((int)ONI_DEVICE_PROPERTY_SERIAL_NUMBER, (void *)serialNumbers[i], &data_size);

		rc = depthStreams[i].start();
		if (openni::Status::STATUS_OK != rc) {
			LogOpenNIError("Couldn't create stream on device " + gcnew String(serialNumbers[i]));
			OpenNIShutdown();
			return nullptr;
		}

		if (!depthStreams[i].isValid())
		{
			log->Error("Depth stream is not valid on device" + gcnew String(serialNumbers[i]));
			OpenNIShutdown();
			return nullptr;
		}

		// Close depth stream and device
		depthStreams[i].stop();
		depthStreams[i].destroy();
		device->close();

		serialNumbersRet[i] = gcnew String(serialNumbers[i]);
	}

	OpenNIShutdown();
	return serialNumbersRet;
}

void MetriCam2::Cameras::AstraOpenNI::LoadAllAvailableChannels()
{
	log->EnterMethod();
	ChannelRegistry^ cr = ChannelRegistry::Instance;
	Channels->Clear();
	// Channels->Add(cr->RegisterCustomChannel((String^)CustomChannelNames::Infrared, UShortCameraImage::typeid));
	//Channels->Add(cr->RegisterChannel(ChannelNames::Color));
	Channels->Add(cr->RegisterChannel(ChannelNames::ZImage));
	Channels->Add(cr->RegisterChannel(ChannelNames::Point3DImage));
	log->LeaveMethod();
}

void MetriCam2::Cameras::AstraOpenNI::ConnectImpl()
{
	bool initSucceeded = OpenNIInit();
	if (!initSucceeded) {
		// Error already logged
		return;
	}

	openni::Status rc = openni::STATUS_OK;
	log->Info("After initialization:\n" + gcnew String(openni::OpenNI::getExtendedError()));
	const char* deviceURI = openni::ANY_DEVICE;
	if (0 != SerialNumber->Length) {
		deviceURI = oMarshalContext.marshal_as<const char*>(SerialNumber);
	}

	rc = camData->device->open(deviceURI);
	if (rc != openni::STATUS_OK)
	{
		log->Error("Device open failed:\n" + gcnew String(openni::OpenNI::getExtendedError()));
		OpenNIShutdown();
		return;
	}

	// Start depth stream
	camData->device->setImageRegistrationMode(openni::IMAGE_REGISTRATION_OFF);

	// Create depth stream reader
	rc = camData->depth->create(*(camData->device), openni::SENSOR_DEPTH);
	camData->depth->setMirroringEnabled(false);
	if (openni::STATUS_OK != rc) {
		log->Error("Couldn't find depth stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
		OpenNIShutdown();
		return;
	}

	// Start depth stream
	rc = camData->depth->start();
	if (openni::STATUS_OK != rc)
	{
		log->Error("Couldn't start depth stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
		camData->depth->destroy();
		OpenNIShutdown();
		return;
	}

	if (!camData->depth->isValid())
	{
		log->Error("No valid depth stream. Exiting\n");
		camData->depth->destroy();
		OpenNIShutdown();
		return;
	}

	openni::VideoMode depthVideoMode = camData->depth->getVideoMode();
	camData->depthWidth = depthVideoMode.getResolutionX();
	camData->depthHeight = depthVideoMode.getResolutionY();

	//Start IR stream
	rc = camData->ir->create(*(camData->device), openni::SENSOR_IR);
	camData->ir->setMirroringEnabled(false);
	if (openni::STATUS_OK != rc) {
		log->Error("Couldn't find IR stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
		OpenNIShutdown();
		return;
	}

	openni::VideoMode irVideoMode = camData->ir->getVideoMode();
	irVideoMode.setResolution(640, 480);
	camData->ir->setVideoMode(irVideoMode);
	if (openni::STATUS_OK != rc) {
		log->Error("Couldn't find IR stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
		OpenNIShutdown();
		return;
	}

	rc = camData->ir->start();
	if (openni::STATUS_OK != rc)
	{
		log->Error("Couldn't start IR stream:\n" + gcnew String(openni::OpenNI::getExtendedError()));
		camData->ir->destroy();
		OpenNIShutdown();
		return;
	}

	if (!camData->ir->isValid())
	{
		log->Error("No valid IR stream. Exiting\n");
		OpenNIShutdown();
		return;
	}

	irVideoMode = camData->ir->getVideoMode();
	camData->irWidth = irVideoMode.getResolutionX();
	camData->irHeight = irVideoMode.getResolutionY();
}

void MetriCam2::Cameras::AstraOpenNI::DisconnectImpl()
{
	camData->depth->destroy();
	camData->ir->destroy();
	OpenNIShutdown();
}

void MetriCam2::Cameras::AstraOpenNI::UpdateImpl()
{
	openni::VideoStream** m_streams = new openni::VideoStream*[2];
	m_streams[0] = camData->depth;
	m_streams[1] = camData->ir;

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
	if (channelName->Equals(ChannelNames::ZImage)) {
		return CalcZImage();
	}
	else if (channelName->Equals(ChannelNames::Color)) {
		return CalcColor();
	}
	else if (channelName->Equals(ChannelNames::Point3DImage)) {
		return CalcPoint3fImage();
	}

	log->LeaveMethod();
	return nullptr;
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
	throw gcnew System::NotImplementedException();
	// TODO: insert return statement here
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
			//TODO: Wait until Orbbec devices support this feature
			openni::CoordinateConverter::convertDepthToWorld(*(camData->depth), x, y, *pDepth, &a, &b, &c);
			depthDataMeters[y, x] = Point3f(a, b, c);
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
