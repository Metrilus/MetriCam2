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
		property int Width
		{
			inline int get() { return m_width; }
		}
		property int Height
		{
			inline int get() { return m_height; }
		}
		property System::Drawing::Icon^ CameraIcon
		{
			System::Drawing::Icon^ get() override
			{
				System::ComponentModel::ComponentResourceManager^ resources = gcnew System::ComponentModel::ComponentResourceManager();
				return cli::safe_cast<System::Drawing::Icon^>(resources->GetObject(L"$this.TexasInstrumentsIcon"));
			}
		}
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
		 * These functions are not save, to use it correctly see the ParamDescs 
		 * to know when it should be called and when not.
		 */
		property int PhaseOffsetBase
		{
			inline int get() { return m_phaseOffsetBase; }
			inline void set(int val) { SetPhaseOffsetBase(val); }
		}
		property int PhaseOffsetDealiasing
		{
			inline int get() { return m_phaseOffsetDealiasing; }
			inline void set(int val) { SetPhaseOffsetDealiasing(val); }
		}
		property uint AmplitudeThreshold
		{
			inline uint get() { return m_amplitudeThreshold; }
			inline void set(uint val) { SetAmplitudeThreshold(val); }
		}
		property uint IlluminationPowerPercentage
		{
			inline uint get() { return m_illuminationPowerPercentage; }
			inline void set(uint val) { SetIlluminationPowerPercentage(val); }
		}
		property uint IntegrationDutyCycle
		{
			inline uint get() { return m_integrationDutyCycle; }
			inline void set(uint val) { SetIntegrationDutyCycle(val); }
		}
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
		property int BaseModulationFrequency
		{
			inline int get() { return m_baseModulationFrequency; }
			inline void set(int val) { SetBaseModulationFrequency(val); }
		}
		property int DealiasingModulationFrequency
		{
			inline int get() { return m_dealiasingModulationFrequency; }
			inline void set(int val) { SetDealiasingModulationFrequency(val); }
		}
		property bool EnableDealiasing
		{
			inline bool get() { return m_enableDealiasing; }
			inline void set(bool val) { SetEnableDealiasing(val); }
		}
		property int HdrScale
		{
			inline int get() { return m_hdrScale; }
			inline void set(int val) { SetHdrScale(val); }
		}

		property bool HDRFilter
		{
			inline bool get() { return (m_hdr_filter_id >= 0); }
			inline void set(bool val) { SetHDRFilter(val); }
		}

		property int Quads
		{
			inline int get() { return (int)m_quadCntMax; }
			inline void set(int val) { SetQuads((uint)val); }
		}
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
		property int SensorTemperature
		{
			inline int get() { return GetSensorTemperature(); }
		}
		property int IlluminationTemperature
		{
			inline int get() { return GetIlluminationTemperature(); }
		}

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


		


		/*
		 * Misc.
		 */
		/*bool UpdateSerialNumber();
		int GetIdFromSerialNumber(String^ serial);*/

		/*
		 * Parameter descriptions
		 */
		property ParamDesc<int>^ WidthDesc
		{
			inline ParamDesc<int> ^get()
			{
				ParamDesc<int> ^res = gcnew ParamDesc<int>();
				res->Description = "Width of images";
				res->Unit = "pixels";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
				return res;
			}
		}

		property ParamDesc<int>^ HeightDesc
		{
			inline ParamDesc<int> ^get()
			{
				ParamDesc<int> ^res = gcnew ParamDesc<int>();
				res->Description = "Height of images";
				res->Unit = "pixels";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
				return res;
			}
		}

		property ParamDesc<int>^ SensorTemperatureDesc
		{
			inline ParamDesc<int> ^get()
			{
				ParamDesc<int> ^res = gcnew ParamDesc<int>();
				res->Description = "Sensor Temperature";
				res->Unit = "C";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
				return res;
			}
		}

		property ParamDesc<int>^ IlluminationTemperatureDesc
		{
			inline ParamDesc<int> ^get()
			{
				ParamDesc<int> ^res = gcnew ParamDesc<int>();
				res->Description = "Illumination Temperature";
				res->Unit = "C";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
				return res;
			}
		}

		property ParamDesc<int>^ PhaseOffsetBaseDesc
		{
			inline ParamDesc<int> ^get()
			{
				ParamDesc<int> ^res = ParamDesc::BuildRangeParamDesc(-2048, 2047);
				res->Description = "Phase offset for base freq.";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
				res->WritableWhen = ParamDesc::ConnectionStates::Connected;
				return res;
			}
		}

		property ParamDesc<int>^ PhaseOffsetDealiasingDesc
		{
			inline ParamDesc<int> ^get()
			{
				ParamDesc<int> ^res = ParamDesc::BuildRangeParamDesc(-2048, 2047);
				res->Description = "Phase offset for dealising freq.";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
				res->WritableWhen = ParamDesc::ConnectionStates::Connected;
				return res;
			}
		}

		property ParamDesc<uint>^ AmplitudeThresholdDesc
		{
			inline ParamDesc<uint> ^get()
			{
				ParamDesc<uint> ^res = ParamDesc::BuildRangeParamDesc(0u, 4095u);
				res->Description = "Amplitude Threshold";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
				res->WritableWhen = ParamDesc::ConnectionStates::Connected;
				return res;
			}
		}

		property ParamDesc<uint>^ IlluminationPowerPercentageDesc
		{
			inline ParamDesc<uint> ^get()
			{
				ParamDesc<uint> ^res = ParamDesc::BuildRangeParamDesc((uint)0, (uint)100);
				res->Unit = "%";
				res->Description = "Illumination Power";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
				res->WritableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
				return res;
			}
		}

		property ParamDesc<uint>^ IntegrationDutyCycleDesc
		{
			inline ParamDesc<uint> ^get()
			{
				ParamDesc<uint> ^res = ParamDesc::BuildRangeParamDesc(0u, 20u); // 20 translates to 31% in voxel viewer, which is the upper limit for laser safety.
				res->Unit = "percent";
				res->Description = "Integration Duty Cycle";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
				res->WritableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
				return res;
			}
		}

		// ModulationFrequency Values: 14.4, 16.0, 18.0, 20.5, 24.0, 28.8, 36.0, 48.0
		property ParamDesc<int>^ BaseModulationFrequencyDesc
		{
			inline ParamDesc<int> ^get()
			{
				List<int> ^allowedValues = gcnew List<int>();// FIXME
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
				ParamDesc<int> ^res = ParamDesc::BuildListParamDesc(allowedValues, "");
				res->Unit = "Hz";
				res->Description = "Base Modulation Frequency";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
				res->WritableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
				return res;
			}
		}

		// ModulationFrequency Values: 14.4, 16.0, 18.0, 20.5, 24.0, 28.8, 36.0, 48.0
		property ParamDesc<int>^ DealiasingModulationFrequencyDesc
		{
			inline ParamDesc<int> ^get()
			{
				List<int> ^allowedValues = gcnew List<int>();// FIXME
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
				ParamDesc<int> ^res = ParamDesc::BuildListParamDesc(allowedValues, "");
				res->Unit = "Hz";
				res->Description = "Dealiasing Modulation Frequency";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
				res->WritableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
				return res;
			}
		}

		property ParamDesc<int>^ HdrScaleDesc
		{
			inline ParamDesc<int> ^get()
			{
				List<int> ^allowedValues = gcnew List<int>();
				allowedValues->Add(0);
				allowedValues->Add(1);
				allowedValues->Add(2);
				allowedValues->Add(3);
				allowedValues->Add(4);
				ParamDesc<int> ^res = ParamDesc::BuildListParamDesc(allowedValues, "");
				res->Unit = "";
				res->Description = "HDR Scale";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
				res->WritableWhen = ParamDesc::ConnectionStates::Connected;
				return res;
			}
		}

		property ParamDesc<bool>^ HDRFilterDesc {
			inline ParamDesc<bool> ^get()
			{
				ParamDesc<bool> ^res = gcnew ParamDesc<bool>();
				res->Unit = "";
				res->Description = "HDR Filter";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
				res->WritableWhen = ParamDesc::ConnectionStates::Connected;
				return res;
			}
		}

		property ParamDesc<int>^ QuadsDesc
		{
			inline ParamDesc<int> ^get()
			{
				List<int> ^allowedValues = gcnew List<int>();
				allowedValues->Add(4);
				allowedValues->Add(6);
				ParamDesc<int> ^res = ParamDesc::BuildListParamDesc(allowedValues, "");
				res->Unit = "";
				res->Description = "Quads";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
				res->WritableWhen = ParamDesc::ConnectionStates::Connected;
				return res;
			}
		}

		property ParamDesc<int>^ SubFramesDesc
		{
			inline ParamDesc<int> ^get()
			{
				List<int> ^allowedValues = gcnew List<int>();
				allowedValues->Add(1);
				allowedValues->Add(2);
				allowedValues->Add(4);
				ParamDesc<int> ^res = ParamDesc::BuildListParamDesc(allowedValues, "");
				res->Unit = "frames";
				res->Description = "Sub Frames";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
				res->WritableWhen = ParamDesc::ConnectionStates::Connected;
				return res;
		   }
		}

		property ParamDesc<int>^ CameraProfileDesc
		{
			inline ParamDesc<int> ^get()
			{
				List<int> ^allowedValues = gcnew List<int>();
				allowedValues->Add((int)Profile::LensOnly);
				allowedValues->Add((int)Profile::ShortRange);
				allowedValues->Add((int)Profile::LongRange);
				allowedValues->Add((int)Profile::HighAmbient);
				allowedValues->Add((int)Profile::NoCalibration);
				ParamDesc<int> ^res = ParamDesc::BuildListParamDesc(allowedValues, "");
				res->Unit = "";
				res->Description = "Calibration Profiles";
				res->ReadableWhen = ParamDesc::ConnectionStates::Connected;
				res->WritableWhen = ParamDesc::ConnectionStates::Disconnected;
				return res;
			}
		}
	};
}
}