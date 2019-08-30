//规避警告错误
#pragma warning(disable: 4245 4100)
#include <windows.h>
#include <usbioctl.h>
#include <sstream>

//头文件包含有先后顺序
#include "ObUvcAPI.h"
//
#include <Shlwapi.h>        // For QISearch, etc.
#include <mfapi.h>          // For MFStartup, etc.
#include <mfidl.h>          // For MF_DEVSOURCE_*, etc.
#include <mfreadwrite.h>    // MFCreateSourceReaderFromMediaSource
#include <mferror.h>

#pragma comment(lib, "Shlwapi.lib")
#pragma comment(lib, "mf.lib")
#pragma comment(lib, "mfplat.lib")
#pragma comment(lib, "mfreadwrite.lib")
#pragma comment(lib, "mfuuid.lib")

#pragma comment(lib, "setupapi.lib")
#pragma comment(lib, "winusb.lib")

//#include <uuids.h>
#include <vidcap.h>
#include <ksmedia.h>
#include <ksproxy.h>

#include <Cfgmgr32.h>

#pragma comment(lib, "cfgmgr32.lib")

#include <SetupAPI.h>
#include <WinUsb.h>

#include <functional>
#include <thread>
#include <chrono>
#include <algorithm>
#include <regex>
#include <map>
#include <mutex>

#include <strsafe.h>

#include "ObCommon.h"
#include "uuids.h"

#define ORBBEC_VENDOR_ID 0x2bc5
#define ASTRA_PRO_COLOR_PID_START 0x0500
#define ASTRA_PRO_COLOR_PID_END 0x05FF

std::shared_ptr<ObUVCDevice> m_pDev;
int rgbWidth;
int rgbHeight;
bool rgbFlipImage;
unsigned char* rgbImage;
int rgbFps = 0;
std::mutex rgb_mutex;

namespace obuvcWin32{

	struct to_string
	{
		std::ostringstream ss;
		template<class T> to_string & operator << (const T & val) { ss << val; return *this; }
		operator std::string() const { return ss.str(); }
	};
}

//static function
static std::string win_to_utf(const WCHAR * s)
{
	int len = WideCharToMultiByte(CP_UTF8, 0, s, -1, nullptr, 0, NULL, NULL);
	if (len == 0) throw std::runtime_error(obuvcWin32::to_string() << "WideCharToMultiByte(...) returned 0 and GetLastError() is " << GetLastError());
	std::string buffer(len - 1, ' ');
	len = WideCharToMultiByte(CP_UTF8, 0, s, -1, &buffer[0], (int)buffer.size() + 1, NULL, NULL);
	if (len == 0) throw std::runtime_error(obuvcWin32::to_string() << "WideCharToMultiByte(...) returned 0 and GetLastError() is " << GetLastError());
	return buffer;
}

template<class T> class com_ptr
{
	T * p;

	void ref(T * new_p)
	{
		if (p == new_p) return;
		unref();
		p = new_p;
		if (p) p->AddRef();
	}

	void unref()
	{
		if (p)
		{
			p->Release();
			p = nullptr;
		}
	}
public:
	com_ptr() : p() {}
	com_ptr(T * p) : com_ptr() { ref(p); }
	com_ptr(const com_ptr & r) : com_ptr(r.p) {}
	~com_ptr() { unref(); }

	operator T * () const { return p; }
	T & operator * () const { return *p; }
	T * operator -> () const { return p; }

	T ** operator & () { unref(); return &p; }
	com_ptr & operator = (const com_ptr & r) { ref(r.p); return *this; }
};

std::vector<std::string> tokenize(std::string string, char separator)
{
	std::vector<std::string> tokens;
	std::string::size_type i1 = 0;
	while (true)
	{
		auto i2 = string.find(separator, i1);
		if (i2 == std::string::npos)
		{
			tokens.push_back(string.substr(i1));
			return tokens;
		}
		tokens.push_back(string.substr(i1, i2 - i1));
		i1 = i2 + 1;
	}
}

bool parse_usb_path(int & vid, int & pid, int & mi, std::string & unique_id, const std::string & path)
{
	auto name = path;
	std::transform(begin(name), end(name), begin(name), ::tolower);
	auto tokens = tokenize(name, '#');
	if (tokens.size() < 1 || tokens[0] != R"(\\?\usb)") return false; // Not a USB device
	if (tokens.size() < 3)
	{
		// LOG_ERROR("malformed usb device path: " << name);
		return false;
	}

	auto ids = tokenize(tokens[1], '&');
	if (ids.size() < 3)
	{
		return false;
	}
	if (ids[0].size() != 8 || ids[0].substr(0, 4) != "vid_" || !(std::istringstream(ids[0].substr(4, 4)) >> std::hex >> vid))
	{
		//LOG_ERROR("malformed vid string: " << tokens[1]);
		return false;
	}

	if (ids[1].size() != 8 || ids[1].substr(0, 4) != "pid_" || !(std::istringstream(ids[1].substr(4, 4)) >> std::hex >> pid))
	{
		//LOG_ERROR("malformed pid string: " << tokens[1]);
		return false;
	}

	if (ids[2].size() != 5 || ids[2].substr(0, 3) != "mi_" || !(std::istringstream(ids[2].substr(3, 2)) >> mi))
	{
		//LOG_ERROR("malformed mi string: " << tokens[1]);
		return false;
	}

	ids = tokenize(tokens[2], '&');
	if (ids.size() < 2)
	{
		//LOG_ERROR("malformed id string: " << tokens[2]);
		return false;
	}
	unique_id = ids[1];
	return true;
}

void check(const char * call, HRESULT hr)
{
	if (FAILED(hr)) throw std::runtime_error(obuvcWin32::to_string() << call << "(...) returned 0x" << std::hex << (uint32_t)hr);
}

void ConvertYUY2ToRGBImage(unsigned char* yuy2_image)
{
	for (int y = 0; y < rgbHeight; y++)
	{
		unsigned char* rgb = rgbImage + y * (rgbWidth * 3);
		unsigned char* yuy2 = yuy2_image + y * (rgbWidth * 2);
		for (int x = 0, j = 0; x <= (rgbWidth - 2) * 3; x += 6, j += 4)
		{
			//first pixel
			int y0 = yuy2[j];
			int u0 = yuy2[j + 1];
			int y1 = yuy2[j + 2];
			int v0 = yuy2[j + 3];

			int c = y0 - 16;
			int d = u0 - 128;
			int e = v0 - 128;

			int b = (298 * c + 516 * d + 128) >> 8; // blue
			int g = (298 * c - 100 * d - 208 * e + 128) >> 8; // green
			int r = (298 * c + 409 * e + 128) >> 8; // red

			//This prevents color distortions in your rgb image
			if (r < 0) r = 0;
			else if (r > 255) r = 255;
			if (g < 0) g = 0;
			else if (g > 255) g = 255;
			if (b < 0) b = 0;
			else if (b > 255) b = 255;

			rgb[x + 0] = (unsigned char)b;
			rgb[x + 1] = (unsigned char)g;
			rgb[x + 2] = (unsigned char)r;

			//Second pixel
			c = y1 - 16;

			b = (298 * c + 516 * d + 128) >> 8; // blue
			g = (298 * c - 100 * d - 208 * e + 128) >> 8; // green
			r = (298 * c + 409 * e + 128) >> 8; // red

			if (r < 0) r = 0;
			else if (r > 255) r = 255;
			if (g < 0) g = 0;
			else if (g > 255) g = 255;
			if (b < 0) b = 0;
			else if (b > 255) b = 255;

			rgb[x + 3] = (unsigned char)b;
			rgb[x + 4] = (unsigned char)g;
			rgb[x + 5] = (unsigned char)r;
		}
	}
}

void ConvertNV12ToRGBImage(unsigned char* nv12_image)
{
	for (int y = 0; y < rgbHeight; y++)
	{
		unsigned char* rgb = rgbImage + y * (rgbWidth * 3);
		unsigned char* nv12y = nv12_image + y * rgbWidth;
		//On UV-line colorizes two lines in the RGB image, since it is based on 2x2 subsampling
		unsigned char* nv12uv = nv12_image + rgbWidth * rgbHeight + (y / 2) * rgbWidth;
		for (int x = 0, j = 0; x <= (rgbWidth - 2) * 3; x += 6, j += 2)
		{
			//u and v are packed interleaved and are valid for a block of 2x2 pixels
			int u = nv12uv[j];
			int v = nv12uv[j + 1];
			int d = u - 128;
			int e = v - 128;

			//First pixel
			int y0 = nv12y[j];
			int c = y0 - 16;

			int b = (298 * c + 516 * d + 128) >> 8; // blue
			int g = (298 * c - 100 * d - 208 * e + 128) >> 8; // green
			int r = (298 * c + 409 * e + 128) >> 8; // red

			//This prevents color distortions in your rgb image
			if (r < 0) r = 0;
			else if (r > 255) r = 255;
			if (g < 0) g = 0;
			else if (g > 255) g = 255;
			if (b < 0) b = 0;
			else if (b > 255) b = 255;

			rgb[x + 0] = (unsigned char)b;
			rgb[x + 1] = (unsigned char)g;
			rgb[x + 2] = (unsigned char)r;

			//Second pixel
			int y1 = nv12y[j + 1];
			c = y1 - 16;

			b = (298 * c + 516 * d + 128) >> 8; // blue
			g = (298 * c - 100 * d - 208 * e + 128) >> 8; // green
			r = (298 * c + 409 * e + 128) >> 8; // red

			if (r < 0) r = 0;
			else if (r > 255) r = 255;
			if (g < 0) g = 0;
			else if (g > 255) g = 255;
			if (b < 0) b = 0;
			else if (b > 255) b = 255;

			rgb[x + 3] = (unsigned char)b;
			rgb[x + 4] = (unsigned char)g;
			rgb[x + 5] = (unsigned char)r;
		}
	}
}

void ConvertYUY2ToRGBImageAndFlip(unsigned char* yuy2_image)
{
	for (int y = 0; y < rgbHeight; y++)
	{
		unsigned char* rgb = rgbImage + y * (rgbWidth * 3);
		unsigned char* yuy2 = yuy2_image + y * (rgbWidth * 2);
		for (int x = (rgbWidth - 2) * 3, j = 0; x >= 0; x -= 6, j += 4)
		{
			//first pixel
			int y0 = yuy2[j];
			int u0 = yuy2[j + 1];
			int y1 = yuy2[j + 2];
			int v0 = yuy2[j + 3];

			int c = y0 - 16;
			int d = u0 - 128;
			int e = v0 - 128;

			int b = (298 * c + 516 * d + 128) >> 8; // blue
			int g = (298 * c - 100 * d - 208 * e + 128) >> 8; // green
			int r = (298 * c + 409 * e + 128) >> 8; // red

			//This prevents color distortions in your rgb image
			if (r < 0) r = 0;
			else if (r > 255) r = 255;
			if (g < 0) g = 0;
			else if (g > 255) g = 255;
			if (b < 0) b = 0;
			else if (b > 255) b = 255;

			rgb[x + 3] = (unsigned char)b;
			rgb[x + 4] = (unsigned char)g;
			rgb[x + 5] = (unsigned char)r;

			//Second pixel
			c = y1 - 16;

			b = (298 * c + 516 * d + 128) >> 8; // blue
			g = (298 * c - 100 * d - 208 * e + 128) >> 8; // green
			r = (298 * c + 409 * e + 128) >> 8; // red

			if (r < 0) r = 0;
			else if (r > 255) r = 255;
			if (g < 0) g = 0;
			else if (g > 255) g = 255;
			if (b < 0) b = 0;
			else if (b > 255) b = 255;

			rgb[x + 0] = (unsigned char)b;
			rgb[x + 1] = (unsigned char)g;
			rgb[x + 2] = (unsigned char)r;
		}
	}
}

void ConvertNV12ToRGBImageAndFlip(unsigned char* nv12_image)
{
	for (int y = 0; y < rgbHeight; y++)
	{
		unsigned char* rgb = rgbImage + y * (rgbWidth * 3);
		unsigned char* nv12y = nv12_image + y * rgbWidth;
		//On UV-line colorizes two lines in the RGB image, since it is based on 2x2 subsampling
		unsigned char* nv12uv = nv12_image + rgbWidth * rgbHeight + (y / 2) * rgbWidth;
		for (int x = (rgbWidth - 2) * 3, j = 0; x >= 0; x -= 6, j+=2)
		{
			//u and v are packed interleaved and are valid for a block of 2x2 pixels
			int u = nv12uv[j];
			int v = nv12uv[j + 1];
			int d = u - 128;
			int e = v - 128;

			//First pixel
			int y0 = nv12y[j];
			int c = y0 - 16;			

			int b = (298 * c + 516 * d + 128) >> 8; // blue
			int g = (298 * c - 100 * d - 208 * e + 128) >> 8; // green
			int r = (298 * c + 409 * e + 128) >> 8; // red

			//This prevents color distortions in your rgb image
			if (r < 0) r = 0;
			else if (r > 255) r = 255;
			if (g < 0) g = 0;
			else if (g > 255) g = 255;
			if (b < 0) b = 0;
			else if (b > 255) b = 255;				

			rgb[x + 3] = (unsigned char)b;
			rgb[x + 4] = (unsigned char)g;
			rgb[x + 5] = (unsigned char)r;

			//Second pixel
			int y1 = nv12y[j + 1];
			c = y1 - 16;

			b = (298 * c + 516 * d + 128) >> 8; // blue
			g = (298 * c - 100 * d - 208 * e + 128) >> 8; // green
			r = (298 * c + 409 * e + 128) >> 8; // red

			if (r < 0) r = 0;
			else if (r > 255) r = 255;
			if (g < 0) g = 0;
			else if (g > 255) g = 255;
			if (b < 0) b = 0;
			else if (b > 255) b = 255;

			rgb[x + 0] = (unsigned char)b;
			rgb[x + 1] = (unsigned char)g;
			rgb[x + 2] = (unsigned char)r;
		}
	}
}

void ProcessorCallback(const void *frame, int size, void *pstream)
{
	if (UVC_COLOR_MEDIASUBTYPE == MEDIASUBTYPE_YUY2)
	{
		rgb_mutex.lock();
		if (rgbImage == NULL)
		{
			return;
		}
		if (rgbFlipImage)
		{
			ConvertYUY2ToRGBImageAndFlip((unsigned char*)frame);
		}
		else
		{
			ConvertYUY2ToRGBImage((unsigned char*)frame);
		}
		rgb_mutex.unlock();
	}
	else if (UVC_COLOR_MEDIASUBTYPE == MEDIASUBTYPE_NV12)
	{
		rgb_mutex.lock();
		if (rgbImage == NULL)
		{
			return;
		}
		if (rgbFlipImage)
		{
			ConvertNV12ToRGBImageAndFlip((unsigned char*)frame);
		}
		else
		{
			ConvertNV12ToRGBImage((unsigned char*)frame);
		}
		rgb_mutex.unlock();
	}
	else if (UVC_COLOR_MEDIASUBTYPE == MEDIASUBTYPE_MJPG)
	{
		BYTE* data = NULL;
		MFT_REGISTER_TYPE_INFO inputFilter = { MFMediaType_Video, MFVideoFormat_MJPG };
		MFT_REGISTER_TYPE_INFO outputFilter = { MFMediaType_Video, MFVideoFormat_YUY2 };
		UINT32 unFlags = MFT_ENUM_FLAG_SYNCMFT | MFT_ENUM_FLAG_LOCALMFT | MFT_ENUM_FLAG_SORTANDFILTER;

		IMFActivate** ppActivate;
		UINT32 numDecodersMJPG = 0;

		HRESULT r = MFTEnumEx(MFT_CATEGORY_VIDEO_DECODER, unFlags, &inputFilter, &outputFilter, &ppActivate, &numDecodersMJPG);
		if (numDecodersMJPG < 1)
		{
			return;
		}

		// Activate transform
		IMFTransform *pDecoder = NULL;
		r = ppActivate[0]->ActivateObject(__uuidof(IMFTransform), (void**)&pDecoder);

		IMFMediaType* pInputMediaType;
		r = MFCreateMediaType(&pInputMediaType);
		r = pInputMediaType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
		r = pInputMediaType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_MJPG);
		r = MFSetAttributeSize(pInputMediaType, MF_MT_FRAME_SIZE, rgbWidth, rgbHeight);
		r = MFSetAttributeRatio(pInputMediaType, MF_MT_FRAME_RATE, rgbFps, 1);
		r = MFSetAttributeRatio(pInputMediaType, MF_MT_PIXEL_ASPECT_RATIO, 1, 1);
		r = pInputMediaType->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive);
		r = pDecoder->SetInputType(0, pInputMediaType, 0);

		IMFMediaType* pOutputMediaType;
		r = MFCreateMediaType(&pOutputMediaType);
		r = pOutputMediaType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
		r = pOutputMediaType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_YUY2);

		r = MFSetAttributeSize(pOutputMediaType, MF_MT_FRAME_SIZE, rgbWidth, rgbHeight);
		r = MFSetAttributeRatio(pOutputMediaType, MF_MT_FRAME_RATE, rgbFps, 1);
		r = MFSetAttributeRatio(pOutputMediaType, MF_MT_PIXEL_ASPECT_RATIO, 1, 1);
		r = pOutputMediaType->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive);

		r = pDecoder->SetOutputType(0, pOutputMediaType, 0);

		r = pDecoder->ProcessMessage(MFT_MESSAGE_NOTIFY_BEGIN_STREAMING, 0);

		DWORD cbMaxLength = 0;
		DWORD cbCurrentLength = 0;

		IMFMediaBuffer* mediaInputBuffer;
		r = MFCreateMemoryBuffer(size, &mediaInputBuffer);
		BYTE* frameBuffer = NULL;
		r = mediaInputBuffer->Lock(&frameBuffer, &cbMaxLength, &cbCurrentLength);
		memcpy(frameBuffer, frame, size);
		r = mediaInputBuffer->Unlock();
		r = mediaInputBuffer->SetCurrentLength(size);

		IMFSample *inputSample = NULL;
		r = MFCreateSample(&inputSample);
		r = inputSample->AddBuffer(mediaInputBuffer);

		r = pDecoder->ProcessInput(0, inputSample, 0);

		DWORD status = 0;
		r = pDecoder->GetOutputStatus(&status);

		IMFMediaBuffer* mediaOutputBuffer;
		r = MFCreateMemoryBuffer(rgbWidth * rgbHeight * 2, &mediaOutputBuffer);

		IMFSample *outputSample = NULL;
		r = MFCreateSample(&outputSample);
		r = outputSample->AddBuffer(mediaOutputBuffer);

		DWORD outStatus = 0;
		MFT_OUTPUT_DATA_BUFFER odf;
		odf.dwStreamID = 0;
		odf.pSample = outputSample;
		odf.dwStatus = 0;
		odf.pEvents = NULL;
		r = pDecoder->ProcessOutput(0, 1, &odf, &outStatus);
		r = pDecoder->ProcessMessage(MFT_MESSAGE_NOTIFY_END_OF_STREAM, 0);
		r = pDecoder->ProcessMessage(MFT_MESSAGE_COMMAND_DRAIN, 0);

		r = mediaOutputBuffer->Lock(&data, &cbMaxLength, &cbCurrentLength);

		rgb_mutex.lock();
		if (rgbImage == NULL)
		{
			return;
		}
		if (rgbFlipImage)
		{
			ConvertYUY2ToRGBImageAndFlip((unsigned char*)data);
		}
		else
		{
			ConvertYUY2ToRGBImage((unsigned char*)data);
		}
		rgb_mutex.unlock();
		mediaOutputBuffer->Unlock();

		mediaInputBuffer->Release();
		inputSample->Release();
		mediaOutputBuffer->Release();
		outputSample->Release();
		pDecoder->Release();
		for (UINT32 i = 0; i < numDecodersMJPG; i++)
		{
			ppActivate[i]->Release();
		}
		CoTaskMemFree(ppActivate);
	}

	AutoResetEventSet();
}
	
int ObUVCInit(int uvcColorWidth, int uvcColorHeight, bool uvcColorFlipImage)
{
	rgbWidth = uvcColorWidth;
	rgbHeight = uvcColorHeight;
	rgbFlipImage = uvcColorFlipImage;
	rgbImage = new unsigned char[rgbWidth * rgbHeight * 3];

	CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
	//std::this_thread::sleep_for(std::chrono::milliseconds(100));
	HRESULT r = MFStartup(MF_VERSION, MFSTARTUP_NOSOCKET);
	if (FAILED(r)) return -1;

	std::map<std::string, std::shared_ptr<ObUVCDevice>> uvcDevices;
	uvcDevices.clear();

	enumerate_all_devices(uvcDevices);

	std::map<std::string, std::shared_ptr<ObUVCDevice>>::iterator it;

	bool orbbecFound = false;
	for (it = uvcDevices.begin(); it != uvcDevices.end(); it++)
	{
		if (get_vendor_id(*(it->second)) == ORBBEC_VENDOR_ID
			&& get_product_id(*(it->second)) >= ASTRA_PRO_COLOR_PID_START
			&& get_product_id(*(it->second)) <= ASTRA_PRO_COLOR_PID_END)
		{
			m_pDev = it->second;
			orbbecFound = true;
			break;
		}
	}

	if (!orbbecFound)
	{
		return -2;
	}

	set_stream(*m_pDev, 0, NULL);

	//设置回调函数
	set_subdevice_mode(*m_pDev, 0, ProcessorCallback);

	//启动视频流
	start_streaming(*m_pDev);

	return rgbFps;
}

void ObUVCWaitForNewColorImage()
{
	AutoResetEventWaitOne();
}
	
void ObUVCFillColorImage(unsigned char* colorData)
{
	rgb_mutex.lock();
	memcpy(colorData, rgbImage, rgbWidth * rgbHeight * 3);
	rgb_mutex.unlock();
}

void ObUVCShutdown()
{
	stop_streaming(*m_pDev);

	MFShutdown();
	CoUninitialize();

	rgb_mutex.lock();
	delete[] rgbImage;
	rgbImage = NULL;
	rgb_mutex.unlock();
}

class reader_callback :public IMFSourceReaderCallback
{
	std::weak_ptr<ObUVCDevice> owner; // The device holds a reference to us, so use weak_ptr to prevent a cycle
	int subdevice_index;
	ULONG ref_count;
	volatile bool streaming = false;
public:
	reader_callback(std::weak_ptr<ObUVCDevice> owner, int subdevice_index) : owner(owner), subdevice_index(subdevice_index), ref_count() {}

	bool is_streaming() const { return streaming; }
	void on_start() { streaming = true; }

#pragma warning( push )
#pragma warning( disable: 4838 )
	// Implement IUnknown
	HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void ** ppvObject) override
	{
		static const QITAB table[] = { QITABENT(reader_callback, IUnknown), QITABENT(reader_callback, IMFSourceReaderCallback), { 0 } };
		return QISearch(this, table, riid, ppvObject);
	}
#pragma warning( pop )

	ULONG STDMETHODCALLTYPE AddRef() override { return InterlockedIncrement(&ref_count); }
	ULONG STDMETHODCALLTYPE Release() override
	{
		ULONG count = InterlockedDecrement(&ref_count);
		if (count == 0) delete this;
		return count;
	}

	// Implement IMFSourceReaderCallback
	HRESULT STDMETHODCALLTYPE OnReadSample(HRESULT hrStatus, DWORD dwStreamIndex, DWORD dwStreamFlags, LONGLONG llTimestamp, IMFSample * sample) override;
	HRESULT STDMETHODCALLTYPE OnFlush(DWORD dwStreamIndex) override
	{
		dwStreamIndex = dwStreamIndex;
		streaming = false; return S_OK;
	}
	HRESULT STDMETHODCALLTYPE OnEvent(DWORD dwStreamIndex, IMFMediaEvent *pEvent) override
	{
		dwStreamIndex = dwStreamIndex;
		pEvent = pEvent;
		return S_OK;
	}
};

struct ObSubdevice
{
	com_ptr<reader_callback> reader_callback;
	com_ptr<IMFActivate> mf_activate;
	com_ptr<IMFMediaSource> mf_media_source;

	//zoom, pan, aperture adjustment, or shutter speed. （pu）
	com_ptr<IAMCameraControl> am_camera_control;
	//brightness, contrast, hue, saturation, gamma, and sharpness （pu）
	com_ptr<IAMVideoProcAmp> am_video_proc_amp;

	//extension unit param set
	std::map<int, com_ptr<IKsControl>> ks_controls;

	com_ptr<IMFSourceReader> mf_source_reader;
	video_channel_callback callback = nullptr;
	//保存流的this指针
	void *m_pstream = nullptr;
	//data_channel_callback  channel_data_callback = nullptr;
	int vid, pid;
	//add by zh [stream start flag]
	volatile bool isStartStream = false;


	com_ptr<IMFMediaSource> get_media_source()
	{
		if (!mf_media_source)
		{
			check("IMFActivate::ActivateObject", mf_activate->ActivateObject(__uuidof(IMFMediaSource), (void **)&mf_media_source));
			if (mf_media_source)
			{
				check("IMFMediaSource::QueryInterface", mf_media_source->QueryInterface(__uuidof(IAMCameraControl), (void **)&am_camera_control));
				if (SUCCEEDED(mf_media_source->QueryInterface(__uuidof(IAMVideoProcAmp), (void **)&am_video_proc_amp))) OB_LOG_INFO("obtained IAMVideoProcAmp\n");
			}
			else throw std::runtime_error(obuvcWin32::to_string() << "Invalid media source");
		}
		return mf_media_source;
	}

	static bool wait_for_async_operation(WINUSB_INTERFACE_HANDLE interfaceHandle, OVERLAPPED &hOvl, ULONG &lengthTransferred, USHORT timeout)
	{
		if (GetOverlappedResult(interfaceHandle, &hOvl, &lengthTransferred, FALSE))
			return true;

		auto lastResult = GetLastError();
		if (lastResult == ERROR_IO_PENDING || lastResult == ERROR_IO_INCOMPLETE)
		{
			WaitForSingleObject(hOvl.hEvent, timeout);
			auto res = GetOverlappedResult(interfaceHandle, &hOvl, &lengthTransferred, FALSE);
			if (res != 1)
			{
				return false;
			}
		}
		else
		{
			lengthTransferred = 0;
			WinUsb_ResetPipe(interfaceHandle, 0x84);
			return false;
		}

		return true;
	}
};

struct ObUVCDevice
{
	OniDeviceInfo deviceInfo;
	const int vid, pid;
	const std::string unique_id;

	std::vector<ObSubdevice> subdevices;

	HANDLE usb_file_handle = INVALID_HANDLE_VALUE;
	WINUSB_INTERFACE_HANDLE usb_interface_handle = INVALID_HANDLE_VALUE;

	std::vector<int> claimed_interfaces;

	int aux_vid, aux_pid;
	std::string aux_unique_id;
	std::thread data_channel_thread;
	volatile bool data_stop;

	ObUVCDevice(int vid, int pid, std::string unique_id) : vid(vid), pid(pid), unique_id(move(unique_id)), aux_pid(0), aux_vid(0), data_stop(false)
	{
		//

	}

	~ObUVCDevice() {
		size_t size = subdevices.size();
		for (int i = 0; i < size; i++)
		{
			stop_streaming(i);
		}

		//stop_data_acquisition();
		//close_win_usb();
	}

	void start_streaming(int subdevice_index)
	{
		//for (auto & sub : subdevices)
		auto & sub = subdevices[subdevice_index];
		{
			//
			if (sub.mf_source_reader)
			{
				sub.reader_callback->on_start();
				check("IMFSourceReader::ReadSample", sub.mf_source_reader->ReadSample(MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0, NULL, NULL, NULL, NULL));
				sub.isStartStream = true;
			}
		}
	}


	void stop_streaming(int subdevice_index)
	{
		//for (auto & sub : subdevices)
		auto & sub = subdevices[subdevice_index];
		if (sub.isStartStream)
		{
			if (sub.mf_source_reader)
			{
				sub.mf_source_reader->Flush(MF_SOURCE_READER_FIRST_VIDEO_STREAM);
			}
		}

		while (true)
		{
			bool is_streaming = false;
			//for (auto & sub : subdevices)
			auto & sub2 = subdevices[subdevice_index];
			{
				is_streaming |= sub2.reader_callback->is_streaming();
			}

			if (is_streaming)
			{
				std::this_thread::sleep_for(std::chrono::milliseconds(10));
			}
			else
			{
				break;
			}
		}

		// Free up our source readers, our KS control nodes, and our media sources, but retain our original IMFActivate objects for later reuse
		//for (auto & sub : subdevices)

		{
			sub.mf_source_reader = nullptr;
			sub.am_camera_control = nullptr;
			sub.am_video_proc_amp = nullptr;
			sub.ks_controls.clear();
			if (sub.mf_media_source)
			{
				sub.mf_media_source = nullptr;
				check("IMFActivate::ShutdownObject", sub.mf_activate->ShutdownObject());
			}
			sub.callback = {};
		}

		sub.isStartStream = false;
	}

	com_ptr<IMFMediaSource> get_media_source(int subdevice_index)
	{
		return subdevices[subdevice_index].get_media_source();
	}


	void close_win_usb()
	{
		if (usb_interface_handle != INVALID_HANDLE_VALUE)
		{
			WinUsb_Free(usb_interface_handle);
			usb_interface_handle = INVALID_HANDLE_VALUE;
		}

		if (usb_file_handle != INVALID_HANDLE_VALUE)
		{
			CloseHandle(usb_file_handle);
			usb_file_handle = INVALID_HANDLE_VALUE;
		}
	}

	bool usb_synchronous_read(uint8_t endpoint, void * buffer, int bufferLength, int * actual_length, DWORD TimeOut)
	{
		if (usb_interface_handle == INVALID_HANDLE_VALUE) throw std::runtime_error("winusb has not been initialized");

		auto result = false;

		BOOL bRetVal = true;

		ULONG lengthTransferred;

		bRetVal = WinUsb_ReadPipe(usb_interface_handle, endpoint, (PUCHAR)buffer, bufferLength, &lengthTransferred, NULL);

		if (bRetVal)
			result = true;
		else
		{
			//auto lastResult = GetLastError();
			WinUsb_ResetPipe(usb_interface_handle, endpoint);
			result = false;
		}

		*actual_length = lengthTransferred;
		return result;
	}

	bool usb_synchronous_write(uint8_t endpoint, void * buffer, int bufferLength, DWORD TimeOut)
	{
		if (usb_interface_handle == INVALID_HANDLE_VALUE) throw std::runtime_error("winusb has not been initialized");

		auto result = false;

		ULONG lengthWritten;
		auto bRetVal = WinUsb_WritePipe(usb_interface_handle, endpoint, (PUCHAR)buffer, bufferLength, &lengthWritten, NULL);
		if (bRetVal)
			result = true;
		else
		{
			//auto lastError = GetLastError();
			WinUsb_ResetPipe(usb_interface_handle, endpoint);
			//OB_LOG_ERROR("WinUsb_ReadPipe failure... lastError: " << lastError);
			result = false;
		}

		return result;
	}
};


HRESULT reader_callback::OnReadSample(HRESULT hrStatus, DWORD dwStreamIndex, DWORD dwStreamFlags, LONGLONG llTimestamp, IMFSample * sample)
{
	if (auto owner_ptr = owner.lock())
	{
		if (sample)
		{
			com_ptr<IMFMediaBuffer> buffer = NULL;
			if (SUCCEEDED(sample->GetBufferByIndex(0, &buffer)))
			{
				BYTE * byte_buffer; DWORD max_length, current_length;
				if (SUCCEEDED(buffer->Lock(&byte_buffer, &max_length, &current_length)))
				{
					auto continuation = [buffer, this]()
					{
						buffer->Unlock();
					};
					//(owner_ptr->subdevices[subdevice_index].m_pstream)->stream_getFrame();
						owner_ptr->subdevices[subdevice_index].callback(byte_buffer, current_length, owner_ptr->subdevices[subdevice_index].m_pstream);
					//(owner_ptr->subdevices[subdevice_index].m_pstream)->stream_raiseNewFrame(nullptr);
				}
			}
		}

		if (auto owner_ptr_new = owner.lock())
		{
			auto hr = owner_ptr_new->subdevices[subdevice_index].mf_source_reader->ReadSample(MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0, NULL, NULL, NULL, NULL);
			switch (hr)
			{
			case S_OK: break;
			case MF_E_INVALIDREQUEST: OB_LOG_ERROR("ReadSample returned MF_E_INVALIDREQUEST \n"); break;
			case MF_E_INVALIDSTREAMNUMBER: OB_LOG_ERROR("ReadSample returned MF_E_INVALIDSTREAMNUMBER \n"); break;
			case MF_E_NOTACCEPTING: OB_LOG_ERROR("ReadSample returned MF_E_NOTACCEPTING \n"); break;
			case E_INVALIDARG: OB_LOG_ERROR("ReadSample returned E_INVALIDARG \n"); break;
			case MF_E_VIDEO_RECORDING_DEVICE_INVALIDATED: OB_LOG_ERROR("ReadSample returned MF_E_VIDEO_RECORDING_DEVICE_INVALIDATED \n"); break;
			default:
				//OB_LOG_ERROR("ReadSample returned HRESULT " << std::hex << (uint32_t)hr);
				break;
			}

			if (hr != S_OK) streaming = false;
		}
	}
	return S_OK;
}


/*API*/

int get_vendor_id(const ObUVCDevice & device) { return device.vid; }

int get_product_id(const ObUVCDevice & device) { return device.pid; }

void set_subdevice_mode(ObUVCDevice & device, int subdevice_index, video_channel_callback callback)
{
	auto & sub = device.subdevices[subdevice_index];

	if (!sub.mf_source_reader)
	{
		com_ptr<IMFAttributes> pAttributes;
		check("MFCreateAttributes", MFCreateAttributes(&pAttributes, 1));
		check("IMFAttributes::SetUnknown", pAttributes->SetUnknown(MF_SOURCE_READER_ASYNC_CALLBACK, static_cast<IUnknown *>(sub.reader_callback)));
		check("MFCreateSourceReaderFromMediaSource", MFCreateSourceReaderFromMediaSource(sub.get_media_source(), pAttributes, &sub.mf_source_reader));
	}

	bool desiredModeFound = false;

	for (DWORD j = 0;; j++)
	{
		com_ptr<IMFMediaType> media_type;
		HRESULT hr = sub.mf_source_reader->GetNativeMediaType((DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM, j, &media_type);
		if (hr == MF_E_NO_MORE_TYPES) break;
		check("IMFSourceReader::GetNativeMediaType", hr);

		UINT32 uvc_width, uvc_height, uvc_fps_num, uvc_fps_denom; GUID subtype;
		check("MFGetAttributeSize", MFGetAttributeSize(media_type, MF_MT_FRAME_SIZE, &uvc_width, &uvc_height));

		check("IMFMediaType::GetGUID", media_type->GetGUID(MF_MT_SUBTYPE, &subtype));

		check("MFGetAttributeRatio", MFGetAttributeRatio(media_type, MF_MT_FRAME_RATE, &uvc_fps_num, &uvc_fps_denom));
		int uvc_fps = uvc_fps_num / uvc_fps_denom;

		char buffer[512];

		if (subtype == MEDIASUBTYPE_MJPG)
		{
			sprintf_s(buffer, 512, "MJPG %dx%d@%dfps\n", uvc_width, uvc_height, uvc_fps);
		}
		else if (subtype == MEDIASUBTYPE_YUY2)
		{
			sprintf_s(buffer, 512, "YUY2 %dx%d@%dfps\n", uvc_width, uvc_height, uvc_fps);
		}
		else if (subtype == MEDIASUBTYPE_NV12)
		{
			sprintf_s(buffer, 512, "NV12 %dx%d@%dfps\n", uvc_width, uvc_height, uvc_fps);
		}
		else
		{
			sprintf_s(buffer, 512, "Unknown %dx%d@%dfps\n", uvc_width, uvc_height, uvc_fps);
		}

		OutputDebugStringA(buffer);

		if (subtype != UVC_COLOR_MEDIASUBTYPE)
		{
			continue;
		}
		if (uvc_width != (UINT32)rgbWidth || uvc_height != (UINT32)rgbHeight) continue;
		if (uvc_fps_denom == 0) continue;
		rgbFps = uvc_fps;

		check("IMFSourceReader::SetCurrentMediaType", sub.mf_source_reader->SetCurrentMediaType((DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM, NULL, media_type));

		sub.callback = callback;
		desiredModeFound = true;
	}
	if (!desiredModeFound)
	{
		throw std::runtime_error(obuvcWin32::to_string() << "no matching media type for  pixel format ");
	}
}

//////////////////////////////////////////////////////////////////////////////////////////////////////

void start_streaming(ObUVCDevice & device, int subdevice_index)
{
	device.start_streaming(subdevice_index);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////
void stop_streaming(ObUVCDevice & device, int subdevice_index)
{
	device.stop_streaming(subdevice_index);
}

void set_stream(ObUVCDevice & device, int subdevice_index, void *pstream)
{
	device.subdevices[subdevice_index].m_pstream = pstream;
}

int enumerate_all_devices(std::map<std::string, std::shared_ptr<ObUVCDevice>> &devices)
{
	IMFAttributes * pAttributes = NULL;
	check("MFCreateAttributes", MFCreateAttributes(&pAttributes, 1));
	check("IMFAttributes::SetGUID", pAttributes->SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID));

	IMFActivate ** ppDevices;
	UINT32 numDevices;
	check("MFEnumDeviceSources", MFEnumDeviceSources(pAttributes, &ppDevices, &numDevices));

	for (UINT32 i = 0; i < numDevices; ++i)
	{
		com_ptr<IMFActivate> pDevice;
		*&pDevice = ppDevices[i];

		WCHAR * wchar_name = NULL; UINT32 length;
		pDevice->GetAllocatedString(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK, &wchar_name, &length);
		std::string name = win_to_utf(wchar_name);
		CoTaskMemFree(wchar_name);

		int vid, pid, mi; std::string unique_id;
		if (!parse_usb_path(vid, pid, mi, unique_id, name)) continue;

		std::shared_ptr<ObUVCDevice> dev;
		for (auto & d : devices)
		{
			if (d.second->vid == vid && d.second->pid == pid &&d.second->unique_id == unique_id)
			{
				dev = d.second;
			}
		}
		if (!dev)
		{
			dev = std::make_shared<ObUVCDevice>(vid, pid, unique_id);
			devices.insert(std::pair<std::string, std::shared_ptr<ObUVCDevice>>(name, dev));
		}

		size_t subdevice_index = mi / 2;
		if (subdevice_index >= dev->subdevices.size()) dev->subdevices.resize(subdevice_index + 1);
		//todo:考虑不同设备包括不同码流的情况

		dev->subdevices[subdevice_index].reader_callback = new reader_callback(dev, static_cast<int>(subdevice_index));
		dev->subdevices[subdevice_index].mf_activate = pDevice;
		dev->subdevices[subdevice_index].vid = vid;
		dev->subdevices[subdevice_index].pid = pid;
	}


	CoTaskMemFree(ppDevices);
	return 0;
}
