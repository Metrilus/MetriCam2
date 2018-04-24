// TiOpt9220.h

#pragma once

#include <CameraSystem.h>
//#include <Common.h>
#include "Common.h"
#include "Logger.h"
#include "UVCStreamer.h"
//#include <DepthCamera.h>
#include <msclr\marshal_cppstd.h>

using namespace System;
using namespace System::Threading;
using namespace System::Collections::Generic;
using namespace MetriCam2;
using namespace MetriCam2::Enums;
using namespace MetriCam2::Attributes;
using namespace Metrilus::Util;
using namespace System::Collections::Generic;
using msclr::interop::marshal_as;

namespace MetriCam2
{
namespace Cameras 
{
	public ref class TIVoxel : Camera
	{
	public:
		enum class Profile
		{
			None = 0,
			LensOnly = 128,
			ShortRange = 129,
			LongRange = 130,
			HighAmbient = 131,
			NoCalibration = 132
		};
		static String^ CHANNEL_NAME_AMPLITUDE = "Amplitude";
		static String^ CHANNEL_NAME_DISTANCE = "Distance";
		static String^ CHANNEL_NAME_AMBIENT = "Ambient";
		static String^ CHANNEL_NAME_PHASE = "Phase";

		static TIVoxel();
		TIVoxel();
		~TIVoxel();

		/*
		 * Scan for available cameras.
		 */
		static array<String^, 1>^ ScanForCameras();

		/*
		 * Non-configuration parameters!
		 */
		[Description("Width", "Width of images")]
		[AccessState(ConnectionStates::Connected)]
		[Unit(Unit::Pixel)]
		property int Width
		{
			inline int get() { return m_width; }
		}

		[Description("Height", "Height of images")]
		[AccessState(ConnectionStates::Connected)]
		[Unit(Unit::Pixel)]
		property int Height
		{
			inline int get() { return m_height; }
		}

#if !NETSTANDARD2_0
		property System::Drawing::Icon^ CameraIcon
		{
			System::Drawing::Icon^ get() override
			{
				System::Reflection::Assembly^ assembly = System::Reflection::Assembly::GetExecutingAssembly();
				System::IO::Stream^ iconStream = assembly->GetManifestResourceStream("TexasInstrumentsIcon.ico");
				return gcnew System::Drawing::Icon(iconStream);
			}
		}
#endif

		/// <summary>
		/// Gets the unambiguous range for one or two modulation frequencies.
		/// Range = C / 2 * f_{mod}
		/// </summary>
		/// <returns></returns>
		property uint UnambiguousRange
		{
			inline uint get()
			{
				return (uint)GetParameterByName("unambiguous_range");
			}
		}

		property float DistanceToPhaseScale
		{
			inline float get() { return (1.0f / UnambiguousRange) * 2.0f * 3.14159265359f; }
		}
		property bool IsVoxelA
		{
			inline bool get()
			{
				if (nullptr == configurationParameters)
				{
					return false; // be optimistic...
				}
				return ! configurationParameters->Contains("amplitude_threshold");
			}
		}
		/*
		 * Configuration parameters!
		 * These functions are not save, to use it correctly see the Attributes 
		 * to know when it should be called and when not.
		 */

		[Description("Phase Offset Base", "Phase offset for base frequency")]
		[AccessState(ConnectionStates::Connected, ConnectionStates::Connected)]
		[Range(-2048, 2047)]
		property int PhaseOffsetBase
		{
			inline int get() { return m_phaseOffsetBase; }
			inline void set(int val) { SetPhaseOffsetBase(val); }
		}

		[Description("Phase Offset Dealiasing", "Phase offset for dealising frequency")]
		[AccessState(ConnectionStates::Connected, ConnectionStates::Connected)]
		[Range(-2048, 2047)]
		property int PhaseOffsetDealiasing
		{
			inline int get() { return m_phaseOffsetDealiasing; }
			inline void set(int val) { SetPhaseOffsetDealiasing(val); }
		}

		[Description("Amplitude Threshold")]
		[AccessState(ConnectionStates::Connected, ConnectionStates::Connected)]
		[Range(0u, 4095u)]
		property uint AmplitudeThreshold
		{
			inline uint get() { return m_amplitudeThreshold; }
			inline void set(uint val) { SetAmplitudeThreshold(val); }
		}

		[Description("Illumination Power")]
		[Unit(Unit::Percent)]
		[AccessState(
			ConnectionStates::Connected | ConnectionStates::Disconnected,
			ConnectionStates::Connected | ConnectionStates::Disconnected)]
		[Range(0u, 100u)]
		property uint IlluminationPowerPercentage
		{
			inline uint get() { return m_illuminationPowerPercentage; }
			inline void set(uint val) { SetIlluminationPowerPercentage(val); }
		}

		[Description("Integration Duty Cycle")]
		[Unit(Unit::Percent)]
		[AccessState(
			ConnectionStates::Connected | ConnectionStates::Disconnected,
			ConnectionStates::Connected | ConnectionStates::Disconnected)]
		[Range(0u, 20u)] // 20 translates to 31% in voxel viewer, which is the upper limit for laser safety.
		property uint IntegrationDutyCycle
		{
			inline uint get() { return m_integrationDutyCycle; }
			inline void set(uint val) { SetIntegrationDutyCycle(val); }
		}

		[Description("Effective Modulation Frequency")]
		[AccessState(ConnectionStates::Connected)]
		property int EffectiveModulationFrequency
		{
			inline int get()
			{
				if (!m_enableDealiasing)
				{
					return BaseModulationFrequency;
				}
				
				if (Ind_Freq_dat_en)
				{
					if (!Ind_Freq_dat_sel)
					{
						return BaseModulationFrequency;
					}
					else
					{
						return DealiasingModulationFrequency;
					}
				}
				
				return GCD(BaseModulationFrequency, DealiasingModulationFrequency); 
			}
		}

		// ModulationFrequency Values: 14.4, 16.0, 18.0, 20.5, 24.0, 28.8, 36.0, 48.0
		property List<int>^ BaseModulationFrequencyList
		{
			inline List<int>^ get()
			{
				List<int>^ allowedValues = gcnew List<int>();
				allowedValues->Add(14400000);
				allowedValues->Add(16000000);
				allowedValues->Add(18000000);
				allowedValues->Add(20500000);
				allowedValues->Add(24000000);
				allowedValues->Add(28800000);
				allowedValues->Add(36000000);
				allowedValues->Add(40000000);
				allowedValues->Add(48000000);
				allowedValues->Add(60000000);
				return allowedValues;
			}
		}

		[Description("Base Modulation Frequency")]
		[Unit("Hz")]
		[AccessState(
			ConnectionStates::Connected | ConnectionStates::Disconnected,
			ConnectionStates::Connected | ConnectionStates::Disconnected)]
		[AllowedValueList("BaseModulationFrequencyList", nullptr)]
		property int BaseModulationFrequency
		{
			inline int get() { return m_baseModulationFrequency; }
			inline void set(int val) { SetBaseModulationFrequency(val); }
		}

		// ModulationFrequency Values: 14.4, 16.0, 18.0, 20.5, 24.0, 28.8, 36.0, 48.0
		property List<int>^ DealiasingModulationFrequencyList
		{
			inline List<int>^ get()
			{
				List<int>^ allowedValues = gcnew List<int>();
				allowedValues->Add(14400000);
				allowedValues->Add(16000000);
				allowedValues->Add(18000000);
				allowedValues->Add(20500000);
				allowedValues->Add(24000000);
				allowedValues->Add(28800000);
				allowedValues->Add(36000000);
				allowedValues->Add(48000000);
				allowedValues->Add(60000000);
				allowedValues->Add(80000000);
				return allowedValues;
			}
		}

		[Description("Dealiasing Modulation Frequency")]
		[Unit("Hz")]
		[AccessState(
			ConnectionStates::Connected | ConnectionStates::Disconnected,
			ConnectionStates::Connected | ConnectionStates::Disconnected)]
		[AllowedValueList("DealiasingModulationFrequencyList", nullptr)]
		property int DealiasingModulationFrequency
		{
			inline int get() { return m_dealiasingModulationFrequency; }
			inline void set(int val) { SetDealiasingModulationFrequency(val); }
		}

		[Description("Enable Dealiasing")]
		[AccessState(ConnectionStates::Connected)]
		property bool EnableDealiasing
		{
			inline bool get() { return m_enableDealiasing; }
			inline void set(bool val) { SetEnableDealiasing(val); }
		}

		property List<int>^ HdrScaleList
		{
			inline List<int>^ get()
			{
				List<int>^ allowedValues = gcnew List<int>();
				allowedValues->Add(0);
				allowedValues->Add(1);
				allowedValues->Add(2);
				allowedValues->Add(3);
				allowedValues->Add(4);
				return allowedValues;
			}
		}

		[Description("HDR Scale")]
		[AccessState(ConnectionStates::Connected, ConnectionStates::Connected)]
		[AllowedValueList("HdrScaleList", nullptr)]
		property int HdrScale
		{
			inline int get() { return m_hdrScale; }
			inline void set(int val) { SetHdrScale(val); }
		}
		
		[Description("HDR Filter")]
		[AccessState(ConnectionStates::Connected, ConnectionStates::Connected)]
		property bool HDRFilter
		{
			inline bool get() { return (m_hdr_filter_id >= 0); }
			inline void set(bool val) { SetHDRFilter(val); }
		}

		property List<int>^ QuadsList
		{
			inline List<int>^ get()
			{
				List<int>^ allowedValues = gcnew List<int>();
				allowedValues->Add(4);
				allowedValues->Add(6);
				return allowedValues;
			}
		}

		[Description("Quads")]
		[AccessState(ConnectionStates::Connected, ConnectionStates::Connected)]
		[AllowedValueList("QuadsList", nullptr)]
		property int Quads
		{
			inline int get() { return (int)m_quadCntMax; }
			inline void set(int val) { SetQuads((uint)val); }
		}

		property List<int>^ SubFramesList
		{
			inline List<int>^ get()
			{
				List<int>^ allowedValues = gcnew List<int>();
				allowedValues->Add(1);
				allowedValues->Add(2);
				allowedValues->Add(4);
				return allowedValues;
			}
		}

		[Description("Sub Frames")]
		[AccessState(ConnectionStates::Connected, ConnectionStates::Connected)]
		[Unit("Frames")]
		[AllowedValueList("SubFramesList", nullptr)]
		property int SubFrames
		{
			inline int get() { return m_subFrames; }
			inline void set(int val) { SetSubFrames(val); }
		}
		property int CoeffIllum
		{
			inline int get() { return m_coeffIllum; }
			inline void set(int val) { SetCoeffIllum(val); }
		}
		property uint Tillum_calib
		{
			inline uint get() { return m_tillum_calib; }
			inline void set(uint val) { SetTillum_calib(val); }
		}
		property int CoeffSensor
		{
			inline int get() { return m_coeffSensor; }
			inline void set(int val) { SetCoeffSensor(val); }
		}
		property uint Tsensor_calib
		{
			inline uint get() { return m_tsensor_calib; }
			inline void set(uint val) { SetTsensor_calib(val); }
		}
		property bool Calib_prec_high
		{
			inline bool get() { return m_calib_prec_high; }
			inline void set(bool val) { SetCalib_prec_high(val); }
		}
		property bool Disable_temp_corr
		{
			inline bool get() { return m_disable_temp_corr; }
			inline void set(bool val) { SetDisableTempCorr(val); }
		}
		property bool Disable_offset_corr
		{
			inline bool get() { return m_disable_offset_corr; }
			inline void set(bool val) { SetDisableOffsetCorr(val); }
		}
		property bool Ind_Freq_dat_en
		{
			inline bool get() { return m_ind_freq_data_en; }
			inline void set(bool val) { SetIndFreqDataEn(val); }
		}
		property bool Ind_Freq_dat_sel
		{
			inline bool get() { return m_ind_freq_data_sel; }
			inline void set(bool val) { SetIndFreqDataSel(val); }
		}
		property int Dealiased_ph_mask
		{
			inline int get() { return m_dealiased_ph_mask; }
			inline void set(int val) { SetDealiased_ph_mask(val); }
		}

		/**
		 * Gets the current sensor temperature.
		 */
		[Description("Sensor Temperature")]
		[Unit(Unit::DegreeCelsius)]
		[AccessState(ConnectionStates::Connected)]
		property int SensorTemperature
		{
			inline int get() { return GetSensorTemperature(); }
		}

		[Description("Illumination Temperature")]
		[Unit(Unit::DegreeCelsius)]
		[AccessState(ConnectionStates::Connected)]
		property int IlluminationTemperature
		{
			inline int get() { return GetIlluminationTemperature(); }
		}

		[Description("Camera Profile", "Calibration Profiles")]
		[AccessState(ConnectionStates::Connected, ConnectionStates::Disconnected)]
		[AllowedValueList(Profile::typeid, nullptr)]
		property Profile CameraProfile
		{
			inline Profile get() { return m_CameraProfile; }
			inline void set(Profile p) 
			{ 
				m_CameraProfile = p; 
				if (IsConnected)
				{
					cam->setCameraProfile((int)m_CameraProfile);
				}
			}
		}
		
		bool WritePT(ProjectiveTransformationZhang^ proj, int profileId);
		List<KeyValuePair<int, String^>>^ GetCameraProfiles();

	protected:
		virtual void ConnectImpl() override;
		virtual void DisconnectImpl() override;
		virtual void UpdateImpl() override;
		virtual void LoadAllAvailableChannels() override;
		virtual CameraImage^ CalcChannelImpl(String^ channelName) override;
	private:
		/// <summary>
		/// Gets the unambiguous range for two modulation frequencies.
		/// Range = C / (2 * GCD(f_{mod1}, f_{mod2}))
		/// </summary>
		/// <param name="modulationFrequency"></param>
		/// <returns>The unambiguous range, in meters.</returns>
		static float GetUnambiguousRange(int baseModulationFrequency, int dealiasingModulationFrequency)
		{
			return SpeedOfLight / (2.0f * GCD(baseModulationFrequency, dealiasingModulationFrequency));
		}

		/**
		 * Greatest common divisor
		 */
		static int GCD(int a, int b)
		{
			return b == 0 ? a : GCD(b, a % b);
		}
		
		static Voxel::CameraSystem* sys;
		static List<TIVoxel^>^ connectedVoxelObjects;

		//Voxel::DevicePtr* device;
		Voxel::DepthCamera* cam;

		ByteCameraImage^ currentPhases;
		ByteCameraImage^ currentAmplitudes;
		ByteCameraImage^ currentAmbient;
		ByteCameraImage^ currentFlags;


		ByteCameraImage^ phaseData;
		ByteCameraImage^ amplitudeData;
		ByteCameraImage^ ambientData;
		ByteCameraImage^ flagsData;
		AutoResetEvent^ updateResetEvent;

		List<String^>^ configurationParameters;		

		// everything needed for capturing data
		int m_width;
		int m_height;
		int m_devnum;

		// configuration parameters
		int m_phaseOffsetBase = -4096; // -2048 - 2047
		Profile m_CameraProfile = Profile::LensOnly;
		int m_phaseOffsetDealiasing = -4096; // -2048 - 2047
		uint m_amplitudeThreshold = 0; // 0 - 4095
		uint m_illuminationPowerPercentage = 0; // 0 - 100 (%)
		uint m_integrationDutyCycle = 0; // 0 - 50 (%)
		int m_baseModulationFrequency = 40000000; // 14.4 - 48.0 (MHz)
		int m_dealiasingModulationFrequency = 48000000; // 14.4 - 48.0 (MHz)
		uint m_subFrames = 0; // 1, 2, 4
		uint m_quadCntMax = 4; // 4, 6
		int m_coeffIllum = -1;
		int m_coeff_illum_Dealiasing = -1;
		uint m_tillum_calib = -1;
		int m_coeffSensor = -1;
		int m_coeff_sensor_Dealiasing = -1;
		uint m_tsensor_calib = -1;
		bool m_calib_prec_high = true;
		bool m_disable_temp_corr = true;
		bool m_disable_offset_corr = true;
		bool m_enableDealiasing = true;
		bool m_ind_freq_data_en;
		bool m_ind_freq_data_sel;
		int m_dealiased_ph_mask;
		int m_hdrScale;

		// hdr filter id
		int m_hdr_filter_id = -1;


		// settings lock, as only one device can use the regProgrammer at a time
		Object^ settingsLock = gcnew Object();

		// calculating channels for images
		CameraImage^ CalcAmplitude();
		CameraImage^ CalcAmbient();
		CameraImage^ CalcPhase();
		CameraImage^ CalcDistance();
		
		Voxel::DevicePtr* GetDeviceBySerialNumber(String^ serial);
		Object^ GetParameterByName(String^ name);
		void SetParameterByName(String^ name, Object^ value);
		void AdoptCameraData(byte* amplitudes, byte* phases, byte* ambient, int amplitudesWidth, int phasesWidth, int ambientWidth);
		void AdoptFlagData(byte* flags, int flagWidth);
		static void onNewDepthFrame(Voxel::DepthCamera &dc, const Voxel::Frame &frame, Voxel::DepthCamera::FrameType c);

		/*
		 * Second we have to initialise the registers of the camera.
		 */
		void VoxelInit();
		 /*
		 * The following functions can be used to configure parameters.
		 * Note: These are the low level variants by actually writing to the camera's
		 * registers. Never use them unless you know what you're doing.
		 */
		// write acccess
		//void SetTestModeEnable(int val);
		//void SetBlkHeaderEn(int val);
		//void SetOpCsPolarity(int val);
		//void SetFbReadyEn(int val);
		//void SetRampPat(int val);
		//void SetAmplitudeScale(int val);
		//void SetFrequencyScale(int val);
		//void SetPixCntMax(int val);
		void SetSubFrames(uint val);
		void SetQuads(unsigned int val);
		void SetHdrScale(uint val);
		//void SetEasyConfEn(int val);
		//void SetIllumPolarity(int val);
		//void SetTgEn(int val);
		void SetAmplitudeThreshold(uint val);
		void SetIntegrationDutyCycle(uint val);
		void SetPhaseOffsetBase(int val);
		void SetPhaseOffsetDealiasing(int val);
		//void SetDisableOffsetCorrection(bool val);
		//void SetDisableTemperatureCorrection(bool val);
		//void SetModPS1(int val);
		//void SetModPLLUpdate(int val);
		//void EnableFlipHorizontally(bool val);
		//// read access
		//int GetTestModeEnable();
		//int GetBlkHeaderEn();
		//int GetOpCsPolarity();
		//int GetFbReadyEn();
		//int GetRampPat();
		//int GetAmplitudeScale();
		//int GetFrequencyScale();
		//int GetPixCntMax();
		uint GetSubFrames();
		unsigned int GetQuads();
		uint GetHdrScale();
		//int GetEasyConfEn();
		//int GetIllumPolarity();
		//int GetTgEn();
		uint GetAmplitudeThreshold();
		uint GetIntegrationDutyCycle();
		int GetPhaseOffsetBase();
		int GetPhaseOffsetDealiasing();
		//bool GetDisableOffsetCorrection();
		//bool GetDisableTemperatureCorrection();
		//int GetModPS1();
		//int GetModPLLUpdate();
		int GetSensorTemperature();
		int GetIlluminationTemperature();
		/*
		 * Functions for setting the camera properties.
		 * Note: They internally use the functions above.
		 */
		uint GetIlluminationPowerPercentage();
		void SetIlluminationPowerPercentage(uint val);
		bool GetIndFreqDataEn() { return m_ind_freq_data_en; }
		bool GetIndFreqDataSel() { return m_ind_freq_data_sel; }
		uint GetDealiased_ph_mask() { return m_dealiased_ph_mask; }
		int GetBaseModulationFrequency();
		void SetBaseModulationFrequency(int val);
		int GetDealiasingModulationFrequency();
		void SetDealiasingModulationFrequency(int val);
		bool GetEnableDealiasing();
		void SetEnableDealiasing(bool val);

		void SetCoeffSensor(int val);
		void SetTsensor_calib(uint val);
		void SetCoeffIllum(int val);
		void SetTillum_calib(uint val);
		void SetCalib_prec_high(bool val);
		void SetDisableOffsetCorr(bool val);
		void SetDisableTempCorr(bool val);
		void SetTgEnable(bool val);
		void SetDealiasEn(bool val);
		void SetIndFreqDataEn(bool val);
		void SetIndFreqDataSel(bool val);
		void SetDealiased_ph_mask(int val);
		void SetHDRFilter(bool val);
	};
}
}