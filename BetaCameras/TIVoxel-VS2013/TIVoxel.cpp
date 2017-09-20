// This is the main DLL file.
#include "stdafx.h"

#include "TIVoxel.h"
#include "Filter/HDRFilter.h"
#include "Filter/DenoiseFilter.h"
#include "Configuration.h"

using namespace System;
using namespace System::Resources;
using namespace System::IO;
using namespace System::Reflection;
using namespace MetriCam2::Cameras;
using namespace System::Runtime::InteropServices;

static TIVoxel::TIVoxel()
{
	Voxel::logger.setDefaultLogLevel(Voxel::LOG_INFO);
	const char* strLib;
	const char* strConf;
	const char* strFW;
	/*
	String^ libPath = Directory::GetCurrentDirectory() + Path::DirectorySeparatorChar + "lib" + Path::DirectorySeparatorChar;
	String^ libConf = Directory::GetCurrentDirectory() + Path::DirectorySeparatorChar + "conf" + Path::DirectorySeparatorChar;
	String^ libFW = Directory::GetCurrentDirectory() + Path::DirectorySeparatorChar + "fw" + Path::DirectorySeparatorChar;
	
	strLib = (const char*)(Marshal::StringToHGlobalAnsi(libPath)).ToPointer();
	strConf = (const char*)(Marshal::StringToHGlobalAnsi(libConf)).ToPointer();
	strFW = (const char*)(Marshal::StringToHGlobalAnsi(libFW)).ToPointer();

	Voxel::Configuration::addLibPath(strLib);
	Voxel::Configuration::addConfPath(strConf);
	Voxel::Configuration::addFirmwarePath(strFW);
	*/
	sys = new Voxel::CameraSystem();
	connectedVoxelObjects = gcnew List<TIVoxel^>();
}

void TIVoxel::ConnectImpl()
{
	System::Threading::Monitor::Enter(settingsLock);
	modelName = "TinTin";
	Voxel::DevicePtr device;
	try
	{
/*		if (this->SerialNumber != nullptr && !this->SerialNumber->Equals(""))
		{
			device = GetDeviceBySerialNumber(this->SerialNumber);
			if (device == NULL)
			{
				ExceptionBuilder::Throw(MetriCam2::Exceptions::ConnectionFailedException::typeid, this, "error_connectionFailed", "Could not find device with serial number " + this->SerialNumber);
				return;
			}
		}
		else
*/		{
			// get number of voxel devices
			Voxel::Vector<Voxel::DevicePtr> &devices = sys->scan();
			int numDevices = devices.size();
			if (numDevices == 0)
			{
				ExceptionBuilder::Throw(MetriCam2::Exceptions::ConnectionFailedException::typeid, this, "error_connectionFailed", "No devices found.");
				return;
			}
			Voxel::DevicePtr toConnect;
			for (int i = 0; i < numDevices; i++)
			{
				if (devices[i].get()->interfaceID() == Voxel::Device::USB)
				{
					Voxel::USBDevice &usb = (Voxel::USBDevice&)*devices[i];
					if (usb.vendorID() == (uint16_t)1105 && usb.productID() == (uint16_t)37125)
					{
						toConnect = devices[i];
						break;
					}
				}
			}
			device = toConnect; // first camera
		}

		// set initial register values
		//VoxelInit();
		cam = sys->connect(device).get();


		if (m_CameraProfile != Profile::None)
		{
			cam->setCameraProfile((int)m_CameraProfile);
			m_CameraProfile = (Profile)cam->getCurrentCameraProfileID();
		}
		if (cam == NULL || !(cam->isInitialized()))
		{
			ExceptionBuilder::Throw(MetriCam2::Exceptions::ConnectionFailedException::typeid, this, "error_connectionFailed", "Failed to open camera.");
			return;
		}
		
		this->SerialNumber = marshal_as<String^>((device)->serialNumber());
		if (this->SerialNumber->Equals("Serial_No._Placeholder"))
		{
			this->SerialNumber = "sn-not-programmed";
		}

		IsConnected = true; // set this early, because it is used by parameters later in the connect process

		connectedVoxelObjects->Add(this);

		Voxel::FrameSize s;
		cam->getFrameSize(s);
		m_height = s.height;
		m_width = s.width;

		Voxel::Map<Voxel::String, Voxel::ParameterPtr> params = cam->getParameters();

		configurationParameters = gcnew List<String^>();
		for (auto it = params.begin(); it != params.end(); ++it)
		{
			String^ paramName = msclr::interop::marshal_as<String^>(it->first);
			if (log->IsDebugEnabled)
			{
				// TODO: get and display additional properties of the param
				log->DebugFormat("Adding config parameter {0}", paramName);
			}
			configurationParameters->Add(paramName);
		}

		bool success = cam->registerCallback(Voxel::DepthCamera::FrameType::FRAME_RAW_FRAME_PROCESSED, this->onNewDepthFrame);
		if (!success)
		{
			log->Error("Could not register callback.");
			IsConnected = false;
			ExceptionBuilder::Throw(MetriCam2::Exceptions::ConnectionFailedException::typeid, this, "error_connectionFailed", "Could not register callback.");
			return;
		}
		else
		{
			log->Debug("Callback registered successfully.");
		}
		cam->start();

		ActivateChannel(CHANNEL_NAME_AMPLITUDE);
		ActivateChannel(CHANNEL_NAME_DISTANCE);
		ActivateChannel(CHANNEL_NAME_AMBIENT);
		ActivateChannel(CHANNEL_NAME_PHASE);
		
		SetDisableOffsetCorr(false);
		SetDisableTempCorr(true);
		
		if ("" == SelectedChannel)
		{
			SelectChannel(CHANNEL_NAME_AMPLITUDE);
		}

		SetParameterByName("coeff_sensor", (int)0);
		SetParameterByName("coeff_illum", (int)0);
		SetParameterByName("tillum_calib", (uint)0);
		SetParameterByName("tsensor_calib", (uint)0);
		SetIndFreqDataEn(true);
		SetIndFreqDataSel(true);
		m_baseModulationFrequency = this->GetBaseModulationFrequency();
		m_dealiasingModulationFrequency = this->GetDealiasingModulationFrequency();
		m_enableDealiasing = GetEnableDealiasing();
		m_amplitudeThreshold = this->GetAmplitudeThreshold();
		m_illuminationPowerPercentage = GetIlluminationPowerPercentage();
		m_integrationDutyCycle = this->GetIntegrationDutyCycle();
		m_phaseOffsetBase = this->GetPhaseOffsetBase();
		m_phaseOffsetDealiasing = this->GetPhaseOffsetDealiasing();
		m_subFrames = this->GetSubFrames();
		m_dealiased_ph_mask = this->GetDealiased_ph_mask();
	}
	catch (MetriCam2::Exceptions::ConnectionFailedException^ cfe)
	{
		// this exception was fired by us, don't log it
		throw;
	}
	catch (Exception^ ex)
	{
		// this exception was unexpected, log it and fire our own one
		log->Error(ex->Message);
		ExceptionBuilder::Throw(MetriCam2::Exceptions::ConnectionFailedException::typeid, this, "error_connectionFailed", "Unexpected error: " + ex->Message);
		return;
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

List<KeyValuePair<int, String^>>^ TIVoxel::GetCameraProfiles()
{
	if (!IsConnected)
		return nullptr;
	const Voxel::Map<int, Voxel::String> profileNames = cam->getCameraProfileNames();
	List<KeyValuePair<int, String^>>^ result = gcnew List<KeyValuePair<int, String^>>();

	for (auto &p : profileNames)
	{
		if (p.first >= 128)
		{
			result->Add(*(gcnew KeyValuePair<int, String^>(p.first, gcnew String(marshal_as<String^>(p.second)))));
		}
		else
		{
			result->Add(*(gcnew KeyValuePair<int, String^>(p.first, gcnew String(marshal_as<String^>(p.second + " (HW)")))));
		}
	}
	return result;
}

bool TIVoxel::WritePT(ProjectiveTransformationZhang^ proj, int profileId)
{
	Voxel::ConfigurationFile* configFile = cam->configFile.getCameraProfile(profileId);

	const char* str;
	bool success = false;
	str = (const char*)(Marshal::StringToHGlobalAnsi(proj->Fx.ToString())).ToPointer();
	success = configFile->set("calib", "fx", Voxel::String(str));
	str = (const char*)(Marshal::StringToHGlobalAnsi(proj->Fy.ToString())).ToPointer();
	success = configFile->set("calib", "fy", Voxel::String(str));

	str = (const char*)(Marshal::StringToHGlobalAnsi(proj->Cx.ToString())).ToPointer();
	success = configFile->set("calib", "cx", Voxel::String(str));
	str = (const char*)(Marshal::StringToHGlobalAnsi(proj->Cy.ToString())).ToPointer();
	success = configFile->set("calib", "cy", Voxel::String(str));

	str = (const char*)(Marshal::StringToHGlobalAnsi(proj->K1.ToString())).ToPointer();
	success = configFile->set("calib", "k1", Voxel::String(str));
	str = (const char*)(Marshal::StringToHGlobalAnsi(proj->K2.ToString())).ToPointer();
	success = configFile->set("calib", "k2", Voxel::String(str));
	str = (const char*)(Marshal::StringToHGlobalAnsi(proj->K3.ToString())).ToPointer();
	success = configFile->set("calib", "k3", Voxel::String(str));

	str = (const char*)(Marshal::StringToHGlobalAnsi(proj->P1.ToString())).ToPointer();
	success = configFile->set("calib", "p1", Voxel::String(str));
	str = (const char*)(Marshal::StringToHGlobalAnsi(proj->P2.ToString())).ToPointer();
	success = configFile->set("calib", "p2", Voxel::String(str));

	if (configFile->getLocation() == Voxel::ConfigurationFile::IN_CAMERA)
	{
		success = cam->configFile.writeToHardware();
	}
	else
	{
		success = configFile->write();
	}

	return success;
}

Object^ TIVoxel::GetParameterByName(String^ name)
{
	Voxel::ParameterPtr param = cam->getParam(msclr::interop::marshal_as<std::string>(name));

	Voxel::BoolParameter *boolParam = dynamic_cast<Voxel::BoolParameter *>(param.get());
	Voxel::IntegerParameter *intParam = dynamic_cast<Voxel::IntegerParameter *>(param.get());
	Voxel::UnsignedIntegerParameter *uintParam = dynamic_cast<Voxel::UnsignedIntegerParameter *>(param.get());
	Voxel::FloatParameter *floatParam = dynamic_cast<Voxel::FloatParameter *>(param.get());
	Voxel::EnumParameter *enumParam = dynamic_cast<Voxel::EnumParameter *>(param.get());

	if (boolParam)
	{
		bool value;

		if (!boolParam->get(value, true))
		{
			ExceptionBuilder::Throw(MetriCam2::Exceptions::ParameterNotSupportedException::typeid, this, "error_getParameter", "Could not convert value of parameter " + name + ".");
			return false;
		}

		log->Debug(name + " = " + value.ToString());

		const Voxel::Vector<Voxel::String> &meaning = boolParam->valueMeaning();

		if (meaning.size() == 2 && meaning[value].size())
		{
			Voxel::String meaningValue = meaning[value];
			String^ meaning = msclr::interop::marshal_as<String^>(meaningValue);
			log->Debug(" (" + meaning + ")");
		}

		return value;
	}
	else if (intParam)
	{
		int value;
		if (!intParam->get(value, true))
		{
			ExceptionBuilder::Throw(MetriCam2::Exceptions::ParameterNotSupportedException::typeid, this, "error_getParameter", "Could not convert value of parameter " + name + ".");
			return false;
		}

		String^ unit = msclr::interop::marshal_as<String^>(intParam->unit());
		log->Debug(name + " = " + value.ToString() + " " + unit);

		return value;
	}
	else if (uintParam)
	{
		uint value;
		if (!uintParam->get(value, true))
		{
			ExceptionBuilder::Throw(MetriCam2::Exceptions::ParameterNotSupportedException::typeid, this, "error_getParameter", "Could not convert value of parameter " + name + ".");
			return false;
		}

		String^ unit = msclr::interop::marshal_as<String^>(uintParam->unit());
		log->Debug(name + " = " + value.ToString() + " " + unit);

		return value;
	}
	else if (floatParam)
	{
		float value;
		if (!floatParam->get(value, true))
		{
			ExceptionBuilder::Throw(MetriCam2::Exceptions::ParameterNotSupportedException::typeid, this, "error_getParameter", "Could not convert value of parameter " + name + ".");
			return false;
		}

		String^ unit = msclr::interop::marshal_as<String^>(floatParam->unit());
		log->Debug(name + " = " + value.ToString() + " " + msclr::interop::marshal_as<String^>(floatParam->unit()));

		return value;
	}
	else if (enumParam)
	{
		int value;
		if (!enumParam->get(value, true))
		{
			ExceptionBuilder::Throw(MetriCam2::Exceptions::ParameterNotSupportedException::typeid, this, "error_getParameter", "Could not convert value of parameter " + name + ".");
			return false;
		}
		log->Debug(name + " = " + value.ToString());

		const Voxel::Vector<Voxel::String> &meaning = enumParam->valueMeaning();

		if (meaning.size() > value && meaning[value].size())
		{
			Voxel::String meaningValue = meaning[value];
			String^ meaning = msclr::interop::marshal_as<String^>(meaningValue);
			log->Debug(" (" + meaning + ")");
		}

		return value;
	}
	else
	{
		ExceptionBuilder::Throw(MetriCam2::Exceptions::ParameterNotSupportedException::typeid, this, "error_getParameter", "Could not convert value of parameter " + name + ". Unsupported parameter type.");
		return false;
	}
}

void TIVoxel::SetParameterByName(String^ name, Object^ value)
{
	Voxel::ParameterPtr param = cam->getParam(msclr::interop::marshal_as<std::string>(name));
	if (!param)
	{
		log->ErrorFormat("No valid parameter with name = '{0}'", name);
		return;
	}
	if (param->ioType() == Voxel::Parameter::IO_READ_ONLY)
	{
		log->ErrorFormat("Parameter '{0}' is read-only", name);
		return;
	}

	Voxel::BoolParameter *boolParam = dynamic_cast<Voxel::BoolParameter *>(param.get());
	Voxel::IntegerParameter *intParam = dynamic_cast<Voxel::IntegerParameter *>(param.get());
	Voxel::UnsignedIntegerParameter *uintParam = dynamic_cast<Voxel::UnsignedIntegerParameter *>(param.get());
	Voxel::FloatParameter *floatParam = dynamic_cast<Voxel::FloatParameter *>(param.get());
	Voxel::EnumParameter *enumParam = dynamic_cast<Voxel::EnumParameter *>(param.get());

	if (boolParam)
	{
		bool val = (bool)value;
		if (!boolParam->set(val))
		{
			ExceptionBuilder::Throw(MetriCam2::Exceptions::ParameterNotSupportedException::typeid, this, "Could not set parameter " + name + " to " + val + ".");
		}
		return;
	}
	else if (intParam)
	{
		int val = (int)value;
		if (!intParam->set(val))
		{
			ExceptionBuilder::Throw(MetriCam2::Exceptions::ParameterNotSupportedException::typeid, this, "Could not set parameter " + name + " to " + val + ".");
		}
		return;
	}
	else if (uintParam)
	{
		uint val = (uint)value;
		if (!uintParam->set(val))
		{
			ExceptionBuilder::Throw(MetriCam2::Exceptions::ParameterNotSupportedException::typeid, this, "Could not set parameter " + name + " to " + val + ".");
		}
		return;
	}
	else if (floatParam)
	{
		float val = (float)value;
		if (!floatParam->set(val))
		{
			ExceptionBuilder::Throw(MetriCam2::Exceptions::ParameterNotSupportedException::typeid, this, "Could not set parameter " + name + " to " + val + ".");
		}
		return;
	}
	else if (enumParam)
	{
		int val = (System::UInt32)value;
		if (!enumParam->set(val))
		{
			ExceptionBuilder::Throw(MetriCam2::Exceptions::ParameterNotSupportedException::typeid, this, "Could not set parameter " + name + " to " + val + ".");
		}
		return;
	}

	ExceptionBuilder::Throw(MetriCam2::Exceptions::ParameterNotSupportedException::typeid, this, "error_setParameter", "Could not set parameter " + name + ". Parameter type is unsupported.");
	return;
}

void TIVoxel::onNewDepthFrame(Voxel::DepthCamera &dc, const Voxel::Frame &frame, Voxel::DepthCamera::FrameType c)
{
	const Voxel::ToFRawFrame *callbackFrame = dynamic_cast<const Voxel::ToFRawFrame *>(&frame);

	if (!callbackFrame)
	{
		std::cout << "Null frame captured? or not of type ToFRawFrame" << std::endl;
		return;
	}

	String^ searchedID = marshal_as<String^>(dc.id());


	for each(TIVoxel^ voxel in connectedVoxelObjects)
	{
		String^ voxelID = marshal_as<String^>(((voxel->cam))->id());

		if (voxelID == searchedID)
		{
			Voxel::ToFRawFramePtr current = Voxel::ToFRawFrame::typeCast(callbackFrame->copy());

			voxel->AdoptCameraData(current->amplitude(), current->phase(), current->ambient(), current->amplitudeWordWidth(), current->phaseWordWidth(), current->ambientWordWidth());

			//voxel->AdoptFlagData(current->flags(), current->flagsWordWidth());

			voxel->updateResetEvent->Set();

			break;
		}
	}
}

Voxel::DevicePtr* TIVoxel::GetDeviceBySerialNumber(String^ _serial)
{
	int id = -1; // -> failure

	Voxel::Vector<Voxel::DevicePtr> &devices = sys->scan();

	int numCameras = devices.size();
	if (numCameras == 0)
	{
		return NULL;
	}
	if (_serial == nullptr || _serial->Equals(""))
	{
		return &devices[0];
	}

	for (int i = 0; i < numCameras; ++i)
	{
		if (_serial->Equals(marshal_as<String^>(devices[i]->serialNumber())))
		{
			return &devices[i];
		}
	}

	return NULL;
}
	

void TIVoxel::DisconnectImpl()
{
	try
	{
		cam->stop();
	}
	catch (Exception^ ex){}
	System::Threading::Thread::Sleep(200);
	try
	{
		Voxel::DepthCameraPtr* ptr = new Voxel::DepthCameraPtr(cam);
		sys->disconnect(*ptr);
	}
	catch (Exception^ ex){}
	connectedVoxelObjects->Remove(this);
}

void TIVoxel::UpdateImpl()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		updateResetEvent->WaitOne();

		GetSensorTemperature();
		GetIlluminationTemperature();

		currentPhases = phaseData;
		currentAmplitudes = amplitudeData;
		currentAmbient = ambientData;
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::LoadAllAvailableChannels()
{
	ChannelRegistry^ cr = ChannelRegistry::Instance;

	Channels->Clear();
	Channels->Add(cr->RegisterChannel(CHANNEL_NAME_AMPLITUDE));
	Channels->Add(cr->RegisterChannel(CHANNEL_NAME_DISTANCE));
	Channels->Add(cr->RegisterCustomChannel(CHANNEL_NAME_AMBIENT, FloatCameraImage::typeid));
	Channels->Add(cr->RegisterCustomChannel(CHANNEL_NAME_PHASE, UShortCameraImage::typeid));
}

CameraImage^ TIVoxel::CalcChannelImpl(String^ channelName)
{
	if (channelName == CHANNEL_NAME_AMBIENT)
	{
		return CalcAmbient();
	}
	else if (channelName == CHANNEL_NAME_AMPLITUDE)
	{
		return CalcAmplitude();
	}
	else if (channelName == CHANNEL_NAME_DISTANCE)
	{
		return CalcDistance();
	}
	else if (channelName == CHANNEL_NAME_PHASE)
	{
		return CalcPhase();
	}
	return nullptr;
}

CameraImage^ TIVoxel::CalcAmplitude()
{
	FloatCameraImage^ result = gcnew FloatCameraImage(m_width, m_height);
	float* resultPtr = result->Data;
	ByteCameraImage^ localAmplitudes = currentAmplitudes;

	unsigned short int *rawConfidence = (unsigned short int*)localAmplitudes->Data;
	for (int i = 0; i < m_width*m_height; i++)
	{
		//covert data
		*resultPtr++ = (float)*rawConfidence++;
	}
	GC::KeepAlive(localAmplitudes);

	return result;
}

CameraImage^ TIVoxel::CalcAmbient()
{
	FloatCameraImage^ result = gcnew FloatCameraImage(m_width, m_height);
	float* resultPtr = result->Data;
	ByteCameraImage^ localAmbient = currentAmbient;

	byte* rawAmbient = (byte*)localAmbient->Data;
	for (int i = 0; i < m_width*m_height; i++)
	{
		//convert data
		*resultPtr++ = *rawAmbient++;
	}
	GC::KeepAlive(localAmbient);

	return result;
}

CameraImage^ TIVoxel::CalcPhase()
{
	UShortCameraImage^ result = gcnew UShortCameraImage(m_width, m_height);
	unsigned short int* resultPtr = result->Data;
	ByteCameraImage^ localPhases = currentPhases;

	unsigned short int *rawPhases = (unsigned short int*)localPhases->Data;
	for (int i = 0; i < m_width*m_height; i++)
	{
		//covert data
		*resultPtr++ = *rawPhases++;
	}
	GC::KeepAlive(localPhases);

	return result;
}

CameraImage^ TIVoxel::CalcDistance()
{
	const float v_light = Camera::SpeedOfLight;
	const float range = (v_light / (2.0f * (float)EffectiveModulationFrequency)); // FIXME: use GetUnambiguousRange or something here.
	const float scaling = range / 4096.0f; // 12-bit phase data

	FloatCameraImage^ result = gcnew FloatCameraImage(m_width, m_height);
	float* resultPtr = result->Data;
	ByteCameraImage^ localPhases = currentPhases;

	unsigned short int *rawPhases = (unsigned short int*)localPhases->Data;
	for (int i = 0; i < m_width*m_height; i++)
	{
		//covert data
		*resultPtr++ = *rawPhases++ * scaling;
	}
	GC::KeepAlive(localPhases);

	return result;
}

TIVoxel::TIVoxel() : m_width(320), m_height(240), m_devnum(0)
{
	updateResetEvent = gcnew AutoResetEvent(false);
}

TIVoxel::~TIVoxel()
{
/*	if (sys != NULL)
		delete sys;
*/
}

void TIVoxel::VoxelInit()
{
}

array<String^, 1>^ TIVoxel::ScanForCameras()
{
	array<String^, 1>^ res = nullptr;
	int cameras = 0;

	Voxel::Vector<Voxel::DevicePtr> &devices = sys->scan();

	cameras = devices.size();

	if (cameras == 0)
	{
		return nullptr;
	}

	res = gcnew array<String^, 1>(cameras);
	for (int i = 0; i < cameras; ++i)
	{
		Voxel::String str;
		res[i] = gcnew String(marshal_as<String^>(devices[i]->serialNumber()));
	}

	return res;
}

void TIVoxel::SetCoeffIllum(int val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		if (!IsConnected)
		{
			log->DebugFormat("Skipping SetCoeffIllumBase because camera is not connected.");
			return;
		}
		m_coeffIllum = val;
		SetParameterByName("coeff_illum", val);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetTillum_calib(uint val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		if (!IsConnected)
		{
			log->DebugFormat("Skipping SetTillum_calib because camera is not connected.");
			return;
		}
		m_tillum_calib = val;
		SetParameterByName("tillum_calib", val);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetCoeffSensor(int val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		if (!IsConnected)
		{
			log->DebugFormat("Skipping SetCoeffSensorBase because camera is not connected.");
			return;
		}
		m_coeffSensor = val;
		SetParameterByName("coeff_sensor", val);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetTsensor_calib(uint val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		if (!IsConnected)
		{
			log->DebugFormat("Skipping SetTsensor_calib because camera is not connected.");
			return;
		}
		m_tsensor_calib = val;
		SetParameterByName("tsensor_calib", val);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetCalib_prec_high(bool val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		if (!IsConnected)
		{
			log->DebugFormat("Skipping SetCalib_prec_high because camera is not connected.");
			return;
		}
		m_calib_prec_high = val;
		SetParameterByName("calib_prec", val);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetDisableOffsetCorr(bool val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		if (!IsConnected)
		{
			log->DebugFormat("Skipping SetDisableOffsetCorr because camera is not connected.");
			return;
		}
		m_disable_offset_corr = val;
		SetParameterByName("disable_offset_corr", val);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetDisableTempCorr(bool val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		if (!IsConnected)
		{
			log->DebugFormat("Skipping SetDisableTempCorr because camera is not connected.");
			return;
		}
		m_disable_temp_corr = val;
		SetParameterByName("disable_temp_corr", val);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetBaseModulationFrequency(int val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		m_baseModulationFrequency = val;
		if (!IsConnected)
		{
			log->DebugFormat("SetModulationFrequency: skipping, because camera is not connected.");
			return;
		}
		SetParameterByName("mod_freq1", (float)val / (float)1000000.0f);
		GetBaseModulationFrequency();
		bool valBool = (bool)GetParameterByName("mod_pll_update");
		SetParameterByName("mod_pll_update", true);
		System::Threading::Thread::Sleep(50);
		SetParameterByName("mod_pll_update", false);
		System::Threading::Thread::Sleep(50);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetDealiasingModulationFrequency(int val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		m_dealiasingModulationFrequency = val;
		if (!IsConnected)
		{
			log->DebugFormat("SetDealiasingModulationFrequency: skipping, because camera is not connected.");
			return;
		}
		SetParameterByName("mod_freq2", (float)val / (float)1000000.0f);
		GetDealiasingModulationFrequency();
		bool valBool = (bool)GetParameterByName("mod_pll_update");
		SetParameterByName("mod_pll_update", true);
		System::Threading::Thread::Sleep(50);
		SetParameterByName("mod_pll_update", false);
		System::Threading::Thread::Sleep(50);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetEnableDealiasing(bool val)
{
	m_enableDealiasing = val;
	SetTgEnable(false);
	//	2. Set the base frequency as described in modulation clock generator section.
	SetBaseModulationFrequency(m_baseModulationFrequency);
	if (val)
	{
		//Procedure for enabling the de - aliasing mode
		//	2b. Set the de - aliasing modulation frequency as described in modulation clock generator section.
		SetDealiasingModulationFrequency(m_dealiasingModulationFrequency);
		//	3. Set the de - aliasing coefficients as explained below.
		// TODO
		//	4. Set the phase calibration parameters for each frequency as described in phase offset correction
		//	section.
		SetPhaseOffsetBase(m_phaseOffsetBase);
		SetPhaseOffsetDealiasing(m_phaseOffsetDealiasing);
		//	5. Set sub_frame_cnt_max count to a maximum of 2 to meet the relation between sub_frame_cnt_max
		//	and quad_cnt_max.Note that quad_cnt_max is treated to be equal to 6 when de - aliasing is
		//	enabled.
		// TODO
		//	6. Set dealias_en parameter to 1
		SetDealiasEn(1);
	}
	else
	{
		//Procedure for disabling the de - aliasing mode
		//	3. Set the phase calibration parameters for the base frequency as described in phase offset correction
		//	section.
		SetPhaseOffsetBase(m_phaseOffsetBase);
		//	4. Set the sub_frame_cnt_max and quad_cnt_max parameters to meet the frame rate requirements
		// TODO
		//	5. Set dealias_en parameter to 0
		SetDealiasEn(0);
	}
	SetTgEnable(true);
}

int TIVoxel::GetBaseModulationFrequency()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		m_baseModulationFrequency = (int)((float)GetParameterByName("mod_freq1") * 1000000);
		return m_baseModulationFrequency;
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

int TIVoxel::GetDealiasingModulationFrequency()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		m_dealiasingModulationFrequency = (int)((float)GetParameterByName("mod_freq2") * 1000000);
		return m_dealiasingModulationFrequency;
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

bool TIVoxel::GetEnableDealiasing()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		m_enableDealiasing = (bool)GetParameterByName("dealias_en");
		return m_enableDealiasing;
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetAmplitudeThreshold(uint val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		if (IsVoxelA)
		{
			SetParameterByName("confidence_threshold", val);
		}
		else
		{
			SetParameterByName("amplitude_threshold", val);
		}
		m_amplitudeThreshold = val;
	}
	catch (Exception^ ex)
	{
		m_amplitudeThreshold = val;
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetIntegrationDutyCycle(uint val)
{
	int regVal = 64 * val / 100;

	bool setFailed;
	bool regOverflow;

	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		m_integrationDutyCycle = val;
		if (!IsConnected)
		{
			return;
		}
		// Sollte der overflow-Test nicht _nach_ dem Schreiben passieren?
		regOverflow = false;
		SetParameterByName("intg_duty_cycle", val);
		setFailed = (bool)GetParameterByName("intg_duty_cycle_set_failed");
		m_integrationDutyCycle = (uint)GetParameterByName("intg_duty_cycle");
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}

	if (regOverflow)
	{
		ExceptionBuilder::Throw(InvalidOperationException::typeid, this, "error_setParameter", "Integration Duty Cycle beyond limit. Change it to a lower value.");
	}
}

void TIVoxel::SetPhaseOffsetBase(int val)
{
	int regVal = val;
	if (regVal < 0)
	{
		regVal += 4096;
	}

	m_phaseOffsetBase = val;
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		SetParameterByName("phase_corr_1", val);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetPhaseOffsetDealiasing(int val)
{
	int regVal = val;
	if (regVal < 0)
	{
		regVal += 4096;
	}

	m_phaseOffsetDealiasing = val;
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		SetParameterByName("phase_corr_2", val);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

uint TIVoxel::GetIlluminationPowerPercentage()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		m_illuminationPowerPercentage = (uint)GetParameterByName("illum_power_percentage");
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
	return m_illuminationPowerPercentage;
}

void TIVoxel::SetDealiased_ph_mask(int val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		SetParameterByName("dealiased_ph_mask", val);
		m_dealiased_ph_mask = val;
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetIndFreqDataEn(bool val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		SetParameterByName("ind_freq_data_en", val);
		m_ind_freq_data_en = val;
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetIndFreqDataSel(bool val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		SetParameterByName("ind_freq_data_sel", val);
		m_ind_freq_data_sel = val;
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetIlluminationPowerPercentage(uint val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		SetParameterByName("illum_power_percentage", val);
	}
	finally
	{
		m_illuminationPowerPercentage = val;
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetTgEnable(bool val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		SetParameterByName("tg_enable", val);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetDealiasEn(bool val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		SetParameterByName("dealias_en", val);
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetHDRFilter(bool val) {
	
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		if (val == 1) {
			Voxel::FilterPtr f = sys->createFilter("Voxel::HDRFilter", Voxel::DepthCamera::FrameType::FRAME_RAW_FRAME_PROCESSED);
			m_hdr_filter_id = cam->addFilter(f, Voxel::DepthCamera::FrameType::FRAME_RAW_FRAME_PROCESSED);
			std::cout << "HDR Filter created and added." << std::endl;
		}
		else {
			Voxel::FilterPtr f = cam->getFilter(m_hdr_filter_id, Voxel::DepthCamera::FrameType::FRAME_RAW_FRAME_PROCESSED);
			cam->removeFilter(m_hdr_filter_id, Voxel::DepthCamera::FrameType::FRAME_RAW_FRAME_PROCESSED);
			m_hdr_filter_id = -1;
			std::cout << "HDR Filter removed." << std::endl;
		}
	}
	catch (Exception^ ex)
	{
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetHdrScale(uint val)
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		SetParameterByName("hdr_scale", val);
		m_hdrScale = val;
		
	}
	catch (Exception^ ex)
	{
		m_hdrScale = val;
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetQuads(unsigned int val)
{
	if (val != 4 && val != 6)
	{
		ExceptionBuilder::Throw(InvalidOperationException::typeid, this, "error_setParameter", "Quads must be 4 or 6!");
	}

	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		SetParameterByName("quad_cnt_max", val);
		m_quadCntMax = val;
	}
	catch (Exception^ ex)
	{
		m_quadCntMax = val;
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::SetSubFrames(uint val)
{
	if (val != 1 && val != 2 && val != 4)
	{
		ExceptionBuilder::Throw(InvalidOperationException::typeid, this, "error_setParameter", "Subframes must be 1, 2 or 4!");
	}

	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		SetParameterByName("sub_frame_cnt_max", val);
		m_subFrames = val;
	}
	catch (Exception^ ex)
	{
		m_subFrames = val;
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

uint TIVoxel::GetAmplitudeThreshold()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		if (IsVoxelA)
		{
			return (uint)GetParameterByName("confidence_threshold");
		}
		return (uint)GetParameterByName("amplitude_threshold");
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

int TIVoxel::GetSensorTemperature()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		return (int)GetParameterByName("tsensor");//temp_out2
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

int TIVoxel::GetIlluminationTemperature()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		return (int)GetParameterByName("tillum");//temp_out1
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

uint TIVoxel::GetIntegrationDutyCycle()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		return (uint)GetParameterByName("intg_duty_cycle");
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

int TIVoxel::GetPhaseOffsetBase()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		return (int)GetParameterByName("phase_corr_1");
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

int TIVoxel::GetPhaseOffsetDealiasing()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		return (int)GetParameterByName("phase_corr_2");
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

uint TIVoxel::GetHdrScale()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		return (uint)(int)GetParameterByName("hdr_scale");// two casts are needed here.
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

unsigned int TIVoxel::GetQuads()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		return (unsigned int)GetParameterByName("quad_cnt_max");// two casts are needed here.
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

uint TIVoxel::GetSubFrames()
{
	System::Threading::Monitor::Enter(settingsLock);
	try
	{
		return (uint)(int)GetParameterByName("sub_frame_cnt_max");// two casts are needed here.
	}
	finally
	{
		System::Threading::Monitor::Exit(settingsLock);
	}
}

void TIVoxel::AdoptCameraData(byte* amplitudes, byte* phases, byte* ambient, int amplitudesWidth, int phasesWidth, int ambientWidth)
{
	ByteCameraImage^ localPhases = gcnew ByteCameraImage(phasesWidth  * this->Width * this->Height, 1);
	memcpy(localPhases->Data, phases, localPhases->Width);
	ByteCameraImage^ localAmplitudes = gcnew ByteCameraImage(amplitudesWidth  * this->Width * this->Height, 1);
	memcpy(localAmplitudes->Data, amplitudes, localAmplitudes->Width);
	ByteCameraImage^ localAmbient = gcnew ByteCameraImage(ambientWidth * this->Width * this->Height, 1);
	memcpy(localAmbient->Data, ambient, localAmbient->Width);
	this->phaseData = localPhases;
	this->amplitudeData = localAmplitudes;
	this->ambientData = localAmbient;
}

void TIVoxel::AdoptFlagData(byte* flags, int flagsWidth)
{
	ByteCameraImage^ localFlags = gcnew ByteCameraImage(flagsWidth * this->Width * this->Height, 1);
	memcpy(localFlags->Data, flags, localFlags->Width);

	byte flag0 = localFlags->Data[0] >> 2;
	// TODO somehow make this information available in MC2.
}