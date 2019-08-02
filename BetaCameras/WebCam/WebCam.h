// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

#pragma once

#include "MetriQEdit.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Drawing::Imaging;
using namespace System::Drawing;
using namespace MetriCam2;
using namespace Metrilus::Util;

namespace MetriCam2
{
	namespace Cameras
	{
		public ref class WebCam : Camera
		{
		private:
			ref struct DirectShowPointers
			{
			public:
				DirectShowPointers()
				{
					pGrabber = NULL;
					pSrcFilter = NULL;
					pVSC = NULL;
					pControl = NULL;
					pEvent = NULL;
					pGraph = NULL;
					pCapture = NULL;
					pGrabberF = NULL;
					pEnum = NULL;
					pPin = NULL;
					pNullF = NULL;

					friendlyName = nullptr;
				}
				ISampleGrabber *pGrabber;
				IBaseFilter *pSrcFilter;
				//for adjusting the source
				IAMStreamConfig *pVSC;
				// Get DirectShow interfaces
				IMediaControl *pControl;
				IMediaEventEx *pEvent;
				IGraphBuilder *pGraph;
				ICaptureGraphBuilder2 *pCapture;
				IBaseFilter *pGrabberF;
				IEnumPins *pEnum;
				IPin *pPin;
				IBaseFilter *pNullF;

				property bool IsConnected
				{
					bool get() { return pGrabber != NULL; }
				}

				property String^ FriendlyName //To do: at the friendly name in ScanForCameras
				{
					String^ get() { return friendlyName; }
					void set(String^ friendlyName) { this->friendlyName = friendlyName; }
				}

			private:
				String ^ friendlyName;
			};

			DirectShowPointers^ directShowPointers;
			Object^ lockObject;
			String^ serialNumberToConnect;
			String^ connectedSerialNumber;
			int nPixels;
			unsigned char* sourceData;
			int activeChannel;
			int nColumns;
			int nRows;
			int nChannels;
			int webCamNumber;
			int frameNumber;
			long currentCbBuffer;
			byte* pBuffer;
			int stride;
			//Buffer size
			int bufferSize;
			bool flipV;
			static List<DirectShowPointers^>^ availableDSWebcams;
			static List<String^>^ availableSerials;
			static List<String^>^ serialsMarkedForConnect;

			static bool DirectShowFindCaptureDevice(IBaseFilter ** ppSrcFilter, String^ serialNumber);
			static HRESULT ConnectFilters(IGraphBuilder *pGraph, IPin *pOut, IBaseFilter *pDest);
			static HRESULT ConnectFilters(IGraphBuilder *pGraph, IBaseFilter *pSrc, IBaseFilter *pDest);
			static HRESULT FindUnconnectedPin(IBaseFilter *pFilter, PIN_DIRECTION PinDir, IPin **ppPin);
			static HRESULT MatchPin(IPin *pPin, PIN_DIRECTION direction, BOOL bShouldBeConnected, BOOL *pResult);
			static HRESULT IsPinConnected(IPin *pPin, BOOL *pResult);
			static HRESULT IsPinDirection(IPin *pPin, PIN_DIRECTION dir, BOOL *pResult);
			static bool DirectShowRePrepareConnect(DirectShowPointers^ dsPointers, String^ serialToRePrepare);
			static ISampleGrabber* DirectShowConnect(DirectShowPointers^ dsPointers);
			static void DirectShowDisconnect(DirectShowPointers^ dsPointers, String^ serialNumber);
			static void DirectShowReleasePrepareConnect(DirectShowPointers^ dsPointers);
			static Object^ serialsMarkedForConnectListLock;
			DirectShowPointers^ GetDirectShowPointersForSerialNumber(String^ serialNumber);

		public:
			static WebCam(void)
			{
				availableDSWebcams = gcnew List<DirectShowPointers^>();
				availableSerials = gcnew List<String^>();
				serialsMarkedForConnect = gcnew List<String^>();
				serialsMarkedForConnectListLock = gcnew Object();
				ScanForCameras();
			}

			WebCam::WebCam(void)
			{
				this->currentCbBuffer = 0;
				this->pBuffer = NULL;
				this->directShowPointers = nullptr;
				this->flipV = false;
				this->lockObject = gcnew Object();
				this->activeChannel = 0;
				this->frameNumber = -1;
				this->ActivateChannel(ChannelNames::Color);
				this->serialNumberToConnect = nullptr;
				this->mirrorImage = false;
			}

			WebCam::~WebCam(void)
			{
				if (pBuffer)
				{
					this->currentCbBuffer = 0;
					CoTaskMemFree(this->pBuffer);
				}
			}

#if !NETSTANDARD2_0
			property System::Drawing::Icon^ CameraIcon
			{
				System::Drawing::Icon^ get() override
				{
					System::Reflection::Assembly^ assembly = System::Reflection::Assembly::GetExecutingAssembly();
					System::IO::Stream^ iconStream = assembly->GetManifestResourceStream("WebcamIcon.ico");
					return gcnew System::Drawing::Icon(iconStream);
				}
			}
#endif

			property bool MirrorImage
			{
				bool get() { return mirrorImage; }
				void set(bool value) { mirrorImage = value; }
			}

			static array<String^, 1>^ ScanForCameras()
			{
				log->EnterMethod();

				bool cleanUpDirectShowConnect = false;

				List<String^>^ connectedSerials = CleanListOfAvailableCameras();

				DirectShowPointers^ dsPointers = gcnew DirectShowPointers();
				int number = 0;
				String^ serial;

				// Use the system device enumerator and class enumerator to find
				// a video capture/preview device, such as a desktop USB video camera.
				HRESULT hr = S_OK;
				IBaseFilter * pSrc = NULL;
				IMoniker* pMoniker = NULL;
				ICreateDevEnum *pDevEnum = NULL;
				IEnumMoniker *pClassEnum = NULL;

				// Create the system device enumerator
				if (CoCreateInstance(CLSID_SystemDeviceEnum, NULL, CLSCTX_INPROC, IID_ICreateDevEnum, (void **)&pDevEnum) < 0)
				{
					log->Error("Could not create system device enumerator");
					CleanUpDirectShowConnect(dsPointers);
					return availableSerials->ToArray();
				}
				// after this point, release up pDevEnum

				// Create an enumerator for the video capture devices
				if (pDevEnum->CreateClassEnumerator(CLSID_VideoInputDeviceCategory, &pClassEnum, 0) < 0 || pClassEnum == NULL)
				{
					log->Error("Could not create class enumerator");
					pDevEnum->Release();
					CleanUpDirectShowConnect(dsPointers);
					return availableSerials->ToArray();;
				}
				// after this point, release up pClassEnum

				// Use the first video capture device on the device list.
				// Note that if the Next() call succeeds but there are no monikers,
				// it will return S_FALSE (which is not a failure).  Therefore, we
				// check that the return code is S_OK instead of using SUCCEEDED() macro.
				while (true)
				{
					// Get next class.
					if (pClassEnum->Next(1, &pMoniker, NULL) == S_FALSE)
					{
						// no next class, exit loop
						break;
					}

					// For the connected WebCam
					dsPointers->pSrcFilter = NULL;
					pin_ptr<IBaseFilter> ppSrcFilter = dsPointers->pSrcFilter;
					pin_ptr<IAMStreamConfig> ppVSC = dsPointers->pVSC;
					// Get DirectShow interfaces
					pin_ptr<IMediaControl> ppControl = dsPointers->pControl;
					pin_ptr<IMediaEventEx> ppEvent = dsPointers->pEvent;
					pin_ptr<IGraphBuilder> ppGraph = dsPointers->pGraph;
					pin_ptr<ICaptureGraphBuilder2> ppCapture = dsPointers->pCapture;

					// Create the filter graph
					if (CoCreateInstance(CLSID_FilterGraph, NULL, CLSCTX_INPROC, IID_IGraphBuilder, (void **)&ppGraph) < 0)
					{
						log->Error("Could not create filter graph");
						cleanUpDirectShowConnect = true;
						break;
					}

					dsPointers->pGraph = ppGraph;

					// Create the capture graph builder
					if (CoCreateInstance(CLSID_CaptureGraphBuilder2, NULL, CLSCTX_INPROC, IID_ICaptureGraphBuilder2, (void**)&ppCapture) < 0)
					{
						log->Error("Could not create CLSID_CaptureGraphBuilder2");
						cleanUpDirectShowConnect = true;
						break;
					}

					dsPointers->pCapture = ppCapture;

					// Obtain interfaces for media control and Video Window
					if (((IGraphBuilder*)ppGraph)->QueryInterface(IID_IMediaControl, (LPVOID*)&ppControl) < 0)
					{
						log->Error("Query for media controls did not return results");
						cleanUpDirectShowConnect = true;
						break;
					}

					dsPointers->pControl = ppControl;

					if (((IGraphBuilder*)ppGraph)->QueryInterface(IID_IMediaEventEx, (LPVOID*)&ppEvent) < 0)
					{
						log->Error("Query for media events did not return results");
						cleanUpDirectShowConnect = true;
						break;
					}

					dsPointers->pEvent = ppEvent;

					// Attach the filter graph to the capture graph
					if (((ICaptureGraphBuilder2*)ppCapture)->SetFiltergraph((IGraphBuilder*)(ppGraph)) < 0)
					{
						log->Error("Attaching filter graph failed");
						cleanUpDirectShowConnect = true;
						break;
					}

					// get the device friendly name:
					IPropertyBag *pPropBag = NULL;
					hr = pMoniker->BindToStorage(0, 0, IID_IPropertyBag, (void **)&pPropBag);
					//CHECK_ERROR(TEXT(" Failed to BindToStorage."), hr);

					dsPointers->FriendlyName = ReadFromPropertyBag(pPropBag, L"FriendlyName");

					serial = GetSerialNumber(pPropBag);
					log->DebugFormat("serial = '{0}'", serial);

					// Bind Moniker to a filter object
					hr = -1;
					try
					{
						hr = pMoniker->BindToObject(0, 0, IID_IBaseFilter, (void**)&pSrc);
					}
					catch (Exception^ ex)
					{
						log->ErrorFormat("Could not bind to object (exception: {0})", ex->Message);
					}
					if (hr < 0)
					{
						log->Error("Could not bind to object (hr < 0)");
						// Some DirectShow drivers (e.g. of GigE cameras) are registered and listed here even though no camera is connected to the system.
						// The bind will have failed, but we have to ignore that.
						if (pMoniker)
						{
							pMoniker->Release();
						}
						if (pPropBag)
						{
							pPropBag->Release();
						}
						CleanUpDirectShowConnect(dsPointers);
						continue;
					}

					if (pMoniker)
					{
						pMoniker->Release();
					}
					if (pPropBag)
					{
						pPropBag->Release();
					}

					if (connectedSerials->Contains(serial)) //if the camera is already connected -> do not add it to the available cameras
					{
						log->DebugFormat("Skipping camera (S/N: {0}) because it is already connected", serial);
						CleanUpDirectShowConnect(dsPointers);
						dsPointers = gcnew DirectShowPointers();
						continue;
					}

					// Copy the found filter pointer to the output parameter.
					ppSrcFilter = pSrc;
					//ppSrcFilter->AddRef(); //made problems for the Basler camera

					dsPointers->pSrcFilter = ppSrcFilter;

					// Add Capture filter to our graph.
					hr = ((IGraphBuilder*)ppGraph)->AddFilter((IBaseFilter*)ppSrcFilter, L"Video Capture");

					//Now create a video stream control, which allows to adjust color, framerate and resolution
					if (((ICaptureGraphBuilder2*)ppCapture)->FindInterface(&PIN_CATEGORY_CAPTURE, &MEDIATYPE_Video, ppSrcFilter, IID_IAMStreamConfig, (void**)&ppVSC) < 0)
					{
						log->Error("Could not create a video stream control");
						cleanUpDirectShowConnect = true;
						break;
					}

					dsPointers->pVSC = ppVSC;

					log->InfoFormat("Found camera with S/N '{0}'", serial);
					availableDSWebcams->Add(dsPointers);
					availableSerials->Add(serial);
					dsPointers = gcnew DirectShowPointers();
				}// while(true)

				if (cleanUpDirectShowConnect)
				{
					CleanUpDirectShowConnect(dsPointers);
				}

				pDevEnum->Release();
				pClassEnum->Release();

				return availableSerials->ToArray();
			}

			bool WebCam::SetVideoParams(int width, int height, double fps, const GUID &subType)
			{
				if (this->directShowPointers == nullptr)
				{
					throw gcnew ArgumentException("Camera is not connected or failed to connect. Please connect the camera before calling this method.");
				}

				if (this->directShowPointers->pPin == NULL)
				{
					throw gcnew ArgumentException("Camera is not connected or failed to connect. Output pin must not be NULL.");
				}

				if (fps <= 0)
				{
					throw gcnew ArgumentException("FPS must be positive and greater than 0.");
				}

				HRESULT hr;

				AM_MEDIA_TYPE *pmt = 0;
				if (FAILED(directShowPointers->pVSC->GetFormat(&pmt)))
				{
					return false;
				}

				if (subType != GUID_NULL)
				{
					pmt->subtype = subType;
				}

				if (pmt->formattype != FORMAT_VideoInfo)
				{
					throw gcnew Exception("Wrong format type of DirectShow output pin.");
				}

				VIDEOINFOHEADER* pvi = (VIDEOINFOHEADER*)pmt->pbFormat;

				if (fps != -1.0)
				{
					pvi->AvgTimePerFrame = (LONGLONG)(10000000 / fps);
				}
				if (width != -1)
				{
					pvi->bmiHeader.biWidth = width;
				}
				if (height != -1)
				{
					pvi->bmiHeader.biHeight = height;
				}

				pin_ptr<IMediaControl> ppControl = directShowPointers->pControl;
				OAFilterState pfs;
				((IMediaControl*)ppControl)->GetState(INFINITE, &pfs);
				if (pfs != State_Stopped)
				{
					((IMediaControl*)ppControl)->Stop();
				}

				hr = directShowPointers->pVSC->SetFormat(pmt);
				if (hr < 0)
				{
					throw gcnew Exception("Camera property change failed.");
				}

				hr = ((IMediaControl*)ppControl)->Run();
				if (hr < 0)
				{
					throw gcnew Exception("Camera restart failed.");
				}

				if (pmt != NULL)
				{
					if (pmt->cbFormat != 0)
					{
						CoTaskMemFree((PVOID)pmt->pbFormat);

						// Strictly unnecessary but tidier
						pmt->cbFormat = 0;
						pmt->pbFormat = NULL;
					}
					if (pmt->pUnk != NULL)
					{
						pmt->pUnk->Release();
						pmt->pUnk = NULL;
					}
					CoTaskMemFree((PVOID)pmt);
				}

				return SUCCEEDED(hr);
			}

		protected:
			/// <summary>
			/// Connects the camera.
			/// </summary>
			virtual void ConnectImpl() override
			{
				//ScanForCameras();
				List<String^>^ availableCameras = GetAvailableCameraSerials();
				if (serialNumberToConnect == nullptr)
				{
					if (availableCameras->Count > 0)
					{
						serialNumberToConnect = availableCameras[0];
					}
					else
					{
						ScanForCameras();
						availableCameras = GetAvailableCameraSerials();
						if (availableCameras->Count > 0)
						{
							serialNumberToConnect = availableCameras[0];
						}
						else
						{
							throw gcnew MetriCam2::Exceptions::ConnectionFailedException("WebCam: error_connectionFailed. No camera available.");
						}
					}
				}
				else
				{
					if (!availableCameras->Contains(serialNumberToConnect))
					{
						ScanForCameras();
						availableCameras = GetAvailableCameraSerials();
						if (!availableCameras->Contains(serialNumberToConnect))
						{
							throw gcnew MetriCam2::Exceptions::ConnectionFailedException("WebCam: error_connectionFailed. Selected camera not available.");
						}
					}
				}

				if (directShowPointers != nullptr && directShowPointers->IsConnected)
				{
					throw gcnew MetriCam2::Exceptions::ConnectionFailedException("WebCam: error_connectionFailed - Camera already connected!");
				}

				//Using DirectShow
				directShowPointers = GetDirectShowPointersForSerialNumber(serialNumberToConnect);
				if (directShowPointers == nullptr)
				{
					throw gcnew MetriCam2::Exceptions::ConnectionFailedException("WebCam: error_connectionFailed");
				}

				ISampleGrabber* dummy = DirectShowConnect(directShowPointers);
				connectedSerialNumber = gcnew String(serialNumberToConnect);
				frameNumber = -1;
				flipV = true;
			}

			virtual void DisconnectImpl() override
			{
				if (directShowPointers->IsConnected)
				{
					DirectShowDisconnect(directShowPointers, this->connectedSerialNumber);
					this->directShowPointers = nullptr;
					delete[] sourceData;
					sourceData = NULL;
					this->bufferSize = 0;
				}
			}

			virtual void UpdateImpl() override
			{
				if (!directShowPointers->IsConnected)
				{
					throw gcnew Exception("error_cameraNotConnected");
				}

				HRESULT hr;
				long evCode;
				pin_ptr<IMediaEventEx> ppEvent = directShowPointers->pEvent;
				hr = ((IMediaEventEx*)ppEvent)->WaitForCompletion(INFINITE, &evCode);

				// Find the required buffer size.
				long cbBuffer;
				pin_ptr<ISampleGrabber> ppGrabber = directShowPointers->pGrabber;

				if (((ISampleGrabber*)ppGrabber)->GetCurrentBuffer(&cbBuffer, NULL) < 0)
				{
					return;
				}

				// If buffer size changed, allocate new buffer
				if (currentCbBuffer != cbBuffer)
				{
					this->currentCbBuffer = cbBuffer;
					CoTaskMemFree(this->pBuffer);
					this->pBuffer = (BYTE*)CoTaskMemAlloc(cbBuffer);
					if (!this->pBuffer)
					{
						hr = E_OUTOFMEMORY;
						throw gcnew OutOfMemoryException("error_outOfMemoryBitmapBuffer");
					}
				}

				if (((ISampleGrabber*)ppGrabber)->GetCurrentBuffer(&cbBuffer, (long*)pBuffer) < 0)
				{
					return;
				}

				AM_MEDIA_TYPE mt;
				ZeroMemory(&mt, sizeof(mt));
				mt.majortype = MEDIATYPE_Video;
				mt.subtype = MEDIASUBTYPE_RGB24;

				if (((ISampleGrabber*)ppGrabber)->GetConnectedMediaType(&mt) < 0)
				{
					return;
				}

				// Examine the format block.
				if ((mt.formattype == FORMAT_VideoInfo) &&
					(mt.cbFormat >= sizeof(VIDEOINFOHEADER)) &&
					(mt.pbFormat != NULL))
				{
					VIDEOINFOHEADER *pVih = (VIDEOINFOHEADER*)mt.pbFormat;

					this->nColumns = pVih->bmiHeader.biWidth;
					this->nRows = pVih->bmiHeader.biHeight;
					this->nChannels = pVih->bmiHeader.biBitCount / 8;
					this->stride = 3 * this->nColumns;
					int newBufferSize = this->stride * this->nRows;

					frameNumber++;
					if (bufferSize != newBufferSize)
					{
						this->bufferSize = newBufferSize;
						if (sourceData != NULL)
						{
							delete[] sourceData;
						}
						sourceData = new unsigned char[this->bufferSize];
					}
					memcpy(sourceData, (unsigned char*)pBuffer, Math::Min(cbBuffer, bufferSize) * sizeof(unsigned char));
				}
				else
				{
					// Invalid format.
					hr = VFW_E_INVALIDMEDIATYPE;
				}

				if (mt.cbFormat != 0)
				{
					CoTaskMemFree((PVOID)mt.pbFormat);
					mt.cbFormat = 0;
					mt.pbFormat = NULL;
				}

				if (mt.pUnk != NULL)
				{
					// pUnk should not be used.
					mt.pUnk->Release();
					mt.pUnk = NULL;
				}
			}

			virtual void LoadAllAvailableChannels() override
			{
				ChannelRegistry^ cr = ChannelRegistry::Instance;

				Channels->Clear();
				Channels->Add(cr->RegisterChannel(ChannelNames::Color));//0
			}

			virtual ImageBase^ CalcChannelImpl(String^ channelName) override
			{
				if (channelName != ChannelNames::Color)
				{
					throw gcnew ArgumentException("error_inactiveChannelName", channelName);
				}

				Bitmap^ bmp = gcnew Bitmap(this->nColumns, this->nRows, System::Drawing::Imaging::PixelFormat::Format24bppRgb);
				Bitmap^ bmpNew = gcnew Bitmap(this->nColumns, this->nRows, System::Drawing::Imaging::PixelFormat::Format24bppRgb);
				Graphics^ graphics = Graphics::FromImage(bmpNew);
				graphics->DrawImage(bmp, Point(0, 0));
				delete graphics;
				delete bmp;

				BitmapData^ bData = bmpNew->LockBits(System::Drawing::Rectangle(System::Drawing::Point(0, 0), bmpNew->Size), System::Drawing::Imaging::ImageLockMode::WriteOnly, bmpNew->PixelFormat);

				if (bData->Stride != this->stride)
				{
					throw gcnew Exception("error_strideMatch");
				}

				memcpy((void*)bData->Scan0, this->sourceData, this->stride * this->nRows);

				bmpNew->UnlockBits(bData);
				if (mirrorImage)
				{
					bmpNew->RotateFlip(RotateFlipType::RotateNoneFlipXY);
				}
				else
				{
					bmpNew->RotateFlip(RotateFlipType::RotateNoneFlipY);
				}

				return gcnew ColorImage(bmpNew);
			}

		private:
			bool mirrorImage;

			List<String^>^ GetAvailableCameraSerials()
			{
				// build list of avail. (disc.) cameras
				List<String^>^ availableCameras = gcnew List<String^>();
				for (int i = 0; i < availableDSWebcams->Count; ++i)
				{
					// add all disconnected cameras to availableCameras
					if (availableDSWebcams[i]->IsConnected)
					{
						continue;
					}

					availableCameras->Add(availableSerials[i]);
				}
				return availableCameras;
			}

			property ParamDesc<bool>^ MirrorImageDesc
			{
				inline ParamDesc<bool>^ get()
				{
					ParamDesc<bool>^ res = gcnew ParamDesc<bool>();
					res->Unit = "";
					res->Description = "Check to flip image horizontally.";
					res->ReadableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
					res->WritableWhen = ParamDesc::ConnectionStates::Connected | ParamDesc::ConnectionStates::Disconnected;
					return res;
				}
			}

			// Cleans up DirectShow semi-connected state.
			static void CleanUpDirectShowConnect(DirectShowPointers^ dsPtrs)
			{
				log->EnterMethod();
				DirectShowDisconnect(dsPtrs, nullptr);
				DirectShowReleasePrepareConnect(dsPtrs);
			}
			// Cleans the current list of available cameras.
			// Afterwards, availableDSWebcams and availableSerials contain only connected cameras.
			// Return value contains only connected cameras.
			static List<String^>^ CleanListOfAvailableCameras()
			{
				// Connected cameras are added to the list.
				// Non-connected cameras are released and added to the list later (if they are still available).
				List<DirectShowPointers^>^ newList = gcnew List<DirectShowPointers^>();
				List<String^>^ connectedSerials = gcnew List<String^>();
				List<String^>^ newSerials = gcnew List<String^>();
				for (int i = 0; i < availableDSWebcams->Count; i++)
				{
					if (availableDSWebcams[i]->IsConnected)
					{
						newList->Add(availableDSWebcams[i]);
						newSerials->Add(availableSerials[i]);
						connectedSerials->Add(availableSerials[i]);
					}
					else
					{
						CleanUpDirectShowConnect(availableDSWebcams[i]);
					}
				}
				availableDSWebcams = newList;
				availableSerials = newSerials;

				return connectedSerials;
			}
			static String^ ReadFromPropertyBag(IPropertyBag* pPropBag, LPOLESTR propName)
			{
				HRESULT hr;
				VARIANT varBuf;
				varBuf.vt = VT_BSTR;
				String^ value = nullptr;

				hr = pPropBag->Read(propName, &varBuf, 0);
				if (0 == hr)
				{
					TCHAR charBuf[1000];
					//CHECK_ERROR(TEXT(" Failed to read friendlyname."), hr);

#ifdef UNICODE    
					(void)StringCchCopy(charBuf, NUMELMS(charBuf), varBuf.bstrVal);
					value = gcnew String((Char*)charBuf);
#else
					WideCharToMultiByte(CP_ACP, 0, varBuf.bstrVal, -1, charBuf, sizeof(charBuf), 0, 0);
					value = gcnew String((SByte*)charBuf);
#endif
					VariantClear(&varBuf);
				}

				return value;
			}
			// Finds the serial number.
			// The serial number should be unique (different for each camera) and constant (if attached to another USB port or computer).
			static String^ GetSerialNumber(IPropertyBag* pPropBag)
			{
				String^ devicePath = ReadFromPropertyBag(pPropBag, L"DevicePath");
				if (nullptr != devicePath && devicePath == "PS3Eye Camera")
				{
					devicePath = nullptr;
				}
				if (nullptr != devicePath)
				{
					//CHECK_ERROR(TEXT(" Failed to read friendlyname."), hr);

					array<String^, 1>^ fields = devicePath->Split('#');
					if (fields->Length >= 3)
					{
						array<String^, 1>^ deviceInfo = fields[2]->Split('&');
						if (deviceInfo->Length >= 2)
						{
							return deviceInfo[1];
						}
					}
				}

				String^ deviceID = ReadFromPropertyBag(pPropBag, L"DeviceID");
				if (nullptr != deviceID)
				{
					return deviceID;
				}

				// DevicePath was not available, or did not contain a usable serial number
				//hr = pPropBag->Read(L"TGUID", &varFriendlyName, 0);
				String^ clsid = ReadFromPropertyBag(pPropBag, L"CLSID");
				if (nullptr != clsid)
				{
					return clsid;
				}

				return nullptr;
			}
		};
	}
}