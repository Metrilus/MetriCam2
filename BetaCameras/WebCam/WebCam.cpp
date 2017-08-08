// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

#include "stdafx.h"

#include "WebCam.h"

using namespace MetriCam2::Cameras;

WebCam::DirectShowPointers^ WebCam::GetDirectShowPointersForSerialNumber(String^ serialNumber)
{
    for	(int i = 0;  i < availableDSWebcams->Count; ++i)
    {
        if (!availableDSWebcams[i]->IsConnected)
        {
            if (serialNumber == availableSerials[i])
            {
                return availableDSWebcams[i];
            }
        }
    }
    return nullptr;
}

bool WebCam::DirectShowFindCaptureDevice(IBaseFilter **ppSrcFilter, String^ serialToFind)
{
    HRESULT hr = S_OK;
    IBaseFilter * pSrc = NULL;
    IMoniker* pMoniker = NULL;
    ICreateDevEnum *pDevEnum = NULL;
    IEnumMoniker *pClassEnum = NULL;

    if (!ppSrcFilter)
    {
        return false;
    }

    // Create the system device enumerator
    if (CoCreateInstance(CLSID_SystemDeviceEnum, NULL, CLSCTX_INPROC, IID_ICreateDevEnum, (void **) &pDevEnum) < 0)
    {
        return false;
    }
    // after this point, release pDevEnum

    // Create an enumerator for the video capture devices
    if (pDevEnum->CreateClassEnumerator(CLSID_VideoInputDeviceCategory, &pClassEnum, 0) < 0 || pClassEnum == NULL)
    {
        pDevEnum->Release();
        return false;
    }
    // after this point, release pClassEnum

    // Use the first video capture device on the device list.
    // Note that if the Next() call succeeds but there are no monikers,
    // it will return S_FALSE (which is not a failure).  Therefore, we
    // check that the return code is S_OK instead of using SUCCEEDED() macro.
    while (true)
    {
        if (pClassEnum->Next(1, &pMoniker, NULL) == S_FALSE)
        {
            break;
        }

        // get the device friendly name:
        IPropertyBag *pPropBag = NULL;
        hr = pMoniker->BindToStorage( 0, 0, IID_IPropertyBag, (void **)&pPropBag );
        //CHECK_ERROR( TEXT(" Failed to BindToStorage."), hr);

        String^ friendlyName = ReadFromPropertyBag(pPropBag, L"FriendlyName"); // for DBG only

        String^ serialNumber;
        serialNumber = GetSerialNumber(pPropBag);

        if (pPropBag)
        {
            pPropBag->Release();
        }

        if (serialNumber == serialToFind)
        {
            // Bind Moniker to a filter object
            hr = -1;
            try
            {
                hr = pMoniker->BindToObject(0,0,IID_IBaseFilter, (void**)&pSrc);
            }
            catch(Exception^)
            {
            }
            if (hr < 0)
            {
                pMoniker->Release();
                break;
            }

            // Copy the found filter pointer to the output parameter.
            *ppSrcFilter = pSrc;
            //(*ppSrcFilter)->AddRef(); //made problems for the Basler camera

            if (pMoniker)
            {
                pMoniker->Release();
            }
            if (pDevEnum)
            {
                pDevEnum->Release();
            }
            if (pClassEnum)
            {
                pClassEnum->Release();
            }

            return true;
        }

        if (pMoniker)
        {
            pMoniker->Release();
            pMoniker = NULL;
        }
    }// while

    // release ressources
    pDevEnum->Release();
    pClassEnum->Release();

    return false;
}
// Connect filter to filter
HRESULT WebCam::ConnectFilters(IGraphBuilder *pGraph, IBaseFilter *pSrc, IBaseFilter *pDest)
{
    IPin *pOut = NULL;

    // Find an output pin on the first filter.
    HRESULT hr = FindUnconnectedPin(pSrc, PINDIR_OUTPUT, &pOut);
    if (hr >= 0)
    {
        hr = ConnectFilters(pGraph, pOut, pDest);
        pOut->Release();
    }
    return hr;
}

HRESULT WebCam::ConnectFilters(IGraphBuilder *pGraph, IPin *pOut, IBaseFilter *pDest) 
{
    IPin *pIn = NULL;

    // Find an input pin on the downstream filter.
    HRESULT hr = FindUnconnectedPin(pDest, PINDIR_INPUT, &pIn);
    if (hr >= 0)
    {
        // Try to connect them.
        hr = pGraph->Connect(pOut, pIn);
        pIn->Release();
    }
    return hr;
}

HRESULT WebCam::FindUnconnectedPin(IBaseFilter *pFilter, PIN_DIRECTION PinDir, IPin **ppPin)
{
    IEnumPins *pEnum = NULL;
    IPin *pPin = NULL;
    BOOL bFound = FALSE;

    HRESULT hr = pFilter->EnumPins(&pEnum);
    if (hr<0)
    {
        goto done;
    }

    while (S_OK == pEnum->Next(1, &pPin, NULL))
    {
        hr = MatchPin(pPin, PinDir, FALSE, &bFound);
        if (FAILED(hr))
        {
            goto done;
        }
        if (bFound)
        {
            *ppPin = pPin;
            (*ppPin)->AddRef();
            break;
        }
        if(pPin)
        {
            pPin->Release();
            pPin = NULL;
        }
    }

    if (!bFound)
    {
        hr = VFW_E_NOT_FOUND;
    }

done:
    if(pPin)
    {
        pPin->Release();
        pPin = NULL;
    }
    if(pEnum)
    {
        pEnum->Release();
        pEnum = NULL;
    }
    return hr;
}
// Match a pin by pin direction and connection state.
HRESULT WebCam::MatchPin(IPin *pPin, PIN_DIRECTION direction, BOOL bShouldBeConnected, BOOL *pResult)
{
    BOOL bMatch = FALSE;
    BOOL bIsConnected = FALSE;

    HRESULT hr = IsPinConnected(pPin, &bIsConnected);
    if (hr>=0)
    {
        if (bIsConnected == bShouldBeConnected)
        {
            hr = IsPinDirection(pPin, direction, &bMatch);
        }
    }

    if (hr>=0)
    {
        *pResult = bMatch;
    }
    return hr;
}
// Query whether a pin is connected to another pin.
//
// Note: This function does not return a pointer to the connected pin.
HRESULT WebCam::IsPinConnected(IPin *pPin, BOOL *pResult)
{
    IPin *pTmp = NULL;
    HRESULT hr = pPin->ConnectedTo(&pTmp);
    if (hr>=0)
    {
        *pResult = TRUE;
    }
    else if (hr == VFW_E_NOT_CONNECTED)
    {
        // The pin is not connected. This is not an error for our purposes.
        *pResult = FALSE;
        hr = S_OK;
    }

    if(pTmp)
    { 
        pTmp->Release(); 
        pTmp = NULL; 
    }
    return hr;
}
// Query whether a pin has a specified direction (input / output)
HRESULT WebCam::IsPinDirection(IPin *pPin, PIN_DIRECTION dir, BOOL *pResult)
{
    PIN_DIRECTION pinDir;
    HRESULT hr = pPin->QueryDirection(&pinDir);
    if (hr>=0)
    {
        *pResult = (pinDir == dir);
    }
    return hr;
}

bool WebCam::DirectShowRePrepareConnect(DirectShowPointers^ dsPointers, String^ serialToRePrepare)
{
    HRESULT hr;
    //For the connected WebCam
    dsPointers->pSrcFilter = NULL;
    pin_ptr<IBaseFilter> ppSrcFilter = dsPointers->pSrcFilter;
    pin_ptr<IAMStreamConfig> ppVSC = dsPointers->pVSC;
    // Get DirectShow interfaces
    pin_ptr<IMediaControl> ppControl = dsPointers->pControl;
    pin_ptr<IMediaEventEx> ppEvent = dsPointers->pEvent;
    pin_ptr<IGraphBuilder> ppGraph = dsPointers->pGraph;
    pin_ptr<ICaptureGraphBuilder2> ppCapture = dsPointers->pCapture;

    // Create the filter graph
    do
    {
        if (CoCreateInstance(CLSID_FilterGraph, NULL, CLSCTX_INPROC, IID_IGraphBuilder, (void **)&ppGraph) < 0)
        {
            break;
        }

        dsPointers->pGraph = ppGraph;

        // Create the capture graph builder
        if (CoCreateInstance(CLSID_CaptureGraphBuilder2 , NULL, CLSCTX_INPROC, IID_ICaptureGraphBuilder2, (void**)&ppCapture) < 0)
        {
            break;
        }

        dsPointers->pCapture = ppCapture;

        // Obtain interfaces for media control and Video Window
        if (((IGraphBuilder*)ppGraph)->QueryInterface(IID_IMediaControl,(LPVOID*)&ppControl) < 0)
        {
            break;
        }

        dsPointers->pControl = ppControl;

        if (((IGraphBuilder*)ppGraph)->QueryInterface(IID_IMediaEventEx, (LPVOID*)&ppEvent) < 0)
        {
            break;
        }

        dsPointers->pEvent = ppEvent;

        // Attach the filter graph to the capture graph
        if (((ICaptureGraphBuilder2*)ppCapture)->SetFiltergraph((IGraphBuilder*)(ppGraph)) < 0)
        {
            break;
        }

        // Use the system device enumerator and class enumerator to find
        // a video capture/preview device, such as a desktop USB video camera.
        if (!DirectShowFindCaptureDevice((IBaseFilter**)(&ppSrcFilter), serialToRePrepare)) 
        {
            break;
        }

        dsPointers->pSrcFilter = ppSrcFilter;
        
        // Add Capture filter to our graph.
        hr = ((IGraphBuilder*)ppGraph)->AddFilter((IBaseFilter*)ppSrcFilter, L"Video Capture");

        //Now create a video stream control, which allows to adjust color, framerate and resolution
        if (((ICaptureGraphBuilder2*)ppCapture)->FindInterface(&PIN_CATEGORY_CAPTURE, &MEDIATYPE_Video, ppSrcFilter, IID_IAMStreamConfig, (void**)&ppVSC) < 0)
        {
            break;
        }

        dsPointers->pVSC = ppVSC;

        return true;
    } while (false);

    // if we got here, DirectShowPrepareConnect didn't succeed.
    CleanUpDirectShowConnect(dsPointers);
    return false; 
}

ISampleGrabber* WebCam::DirectShowConnect(DirectShowPointers^ dsPointers)
{
    HRESULT hr;
    //For the connected WebCam
    pin_ptr<IBaseFilter> ppSrcFilter = dsPointers->pSrcFilter;
    pin_ptr<IAMStreamConfig> ppVSC = dsPointers->pVSC;
    // Get DirectShow interfaces
    pin_ptr<IMediaControl> ppControl = dsPointers->pControl;
    pin_ptr<IMediaEventEx> ppEvent = dsPointers->pEvent;
    pin_ptr<IGraphBuilder> ppGraph = dsPointers->pGraph;
    pin_ptr<ICaptureGraphBuilder2> ppCapture = dsPointers->pCapture;

    pin_ptr<IBaseFilter> ppGrabberF = dsPointers->pGrabberF;
    pin_ptr<ISampleGrabber> ppGrabber = dsPointers->pGrabber;
    pin_ptr<IEnumPins> ppEnum = dsPointers->pEnum;
    pin_ptr<IPin> ppPin = dsPointers->pPin;
    pin_ptr<IBaseFilter> ppNullF = dsPointers->pNullF;

    // Create the Sample Grabber filter.
    hr = CoCreateInstance(CLSID_SampleGrabber, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS((IBaseFilter**)(&ppGrabberF)));
    if (hr < 0)
    {
        DirectShowDisconnect(dsPointers, nullptr); 
        return NULL;
    }

    hr = ((IGraphBuilder*)ppGraph)->AddFilter((IBaseFilter*)ppGrabberF, L"Sample Grabber");
    if (hr < 0)
    {
        DirectShowDisconnect(dsPointers, nullptr); 
        return NULL; 
    }

    hr = ((IBaseFilter*)ppGrabberF)->QueryInterface(IID_PPV_ARGS((ISampleGrabber**)(&ppGrabber)));
    if (hr < 0)
    {
        DirectShowDisconnect(dsPointers, nullptr); 
        return NULL;
    }

    AM_MEDIA_TYPE mt;
    ZeroMemory(&mt, sizeof(mt));
    mt.majortype = MEDIATYPE_Video;
    mt.subtype = MEDIASUBTYPE_RGB24;

    hr = ((ISampleGrabber*)ppGrabber)->SetMediaType(&mt);
    if (hr < 0)
    {
        DirectShowDisconnect(dsPointers, nullptr); 
        return NULL;
    }

    hr = ((IBaseFilter*)ppSrcFilter)->EnumPins((IEnumPins**)(&ppEnum));
    if (hr < 0)
    {
        DirectShowDisconnect(dsPointers, nullptr); 
        return NULL;
    }

    while (S_OK == ((IEnumPins*)ppEnum)->Next(1, (IPin**)(&ppPin), NULL))
    {
        hr = ConnectFilters((IGraphBuilder*)ppGraph, (IPin*)ppPin, (IBaseFilter*)ppGrabberF);
        if (ppPin)
        {
            ((IPin*)ppPin)->Release(); 
            dsPointers->pPin=NULL;
        }
        if (hr >= 0)
        {
            break;
        }
    }

    if (hr < 0)
    {
        DirectShowDisconnect(dsPointers, nullptr); 
        return NULL; 
    }

    hr = CoCreateInstance(CLSID_NullRenderer, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS((IBaseFilter**)(&ppNullF)));
    if (hr < 0)
    {
        DirectShowDisconnect(dsPointers, nullptr); 
        return NULL;
    }
    //because a subsequent filter is required -> NullFilter
    hr = ((IGraphBuilder*)ppGraph)->AddFilter((IBaseFilter*)ppNullF, L"Null Filter");
    if (hr < 0)
    {
        DirectShowDisconnect(dsPointers, nullptr); 
        return NULL;
    }

    hr = ConnectFilters((IGraphBuilder*)ppGraph, (IBaseFilter*)ppGrabberF, (IBaseFilter*)ppNullF);

    if (hr < 0)
    {
        DirectShowDisconnect(dsPointers, nullptr); 
        return NULL;
    }

    hr = ((ISampleGrabber*)ppGrabber)->SetOneShot(TRUE);
    if (hr < 0)
    {
        DirectShowDisconnect(dsPointers, nullptr); 
        return NULL;
    }

    hr = ((ISampleGrabber*)ppGrabber)->SetBufferSamples(TRUE);
    if (hr < 0)
    {
        DirectShowDisconnect(dsPointers, nullptr); 
        return NULL;
    }

    hr = ((IMediaControl*)ppControl)->Run();
    if (hr < 0)
    {
        DirectShowDisconnect(dsPointers, nullptr); 
        return NULL;
    }

    dsPointers->pEvent = ppEvent;
    dsPointers->pGrabber = ppGrabber;
    dsPointers->pSrcFilter = ppSrcFilter;
    dsPointers->pVSC = ppVSC;
    dsPointers->pControl = ppControl;
    dsPointers->pGraph = ppGraph;
    dsPointers->pCapture = ppCapture;
    dsPointers->pGrabberF = ppGrabberF;
    dsPointers->pEnum = ppEnum;
    dsPointers->pPin = ppPin;
    dsPointers->pNullF = ppNullF;
    return (ISampleGrabber*)ppGrabber;
}

void WebCam::DirectShowDisconnect(DirectShowPointers^ dsPointers, String^ serialNumber)
{
    // Stop previewing data
    if (dsPointers->pControl)
    {
        dsPointers->pControl->StopWhenReady();
    }
    // Release DirectShow interfaces
    if (dsPointers->pPin)
    {
        dsPointers->pPin->Release();
    }
    if (dsPointers->pEnum)
    {
        dsPointers->pEnum->Release();
    }
    if (dsPointers->pNullF)
    {
        dsPointers->pNullF->Release();
    }
    if (dsPointers->pGrabber)
    {
        dsPointers->pGrabber->Release();
    }
    if (dsPointers->pGrabberF)
    {
        dsPointers->pGrabberF->Release(); 
    }
    dsPointers->pPin = NULL;
    dsPointers->pEnum = NULL; 
    dsPointers->pNullF = NULL; 
    dsPointers->pGrabber = NULL;
    dsPointers->pGrabberF = NULL;
    //if (serialNumber != nullptr) // TODO: remove this check?
    {
        DirectShowReleasePrepareConnect(dsPointers);
        DirectShowRePrepareConnect(dsPointers, serialNumber);
    }
}

void WebCam::DirectShowReleasePrepareConnect(DirectShowPointers^ dsPointers)
{
    if (dsPointers->pGraph)
    {
        dsPointers->pGraph->Release();
    }
    if (dsPointers->pControl)
    {
        dsPointers->pControl->Release();
    }
    if (dsPointers->pEvent)
    {
        dsPointers->pEvent->Release();
    }
    if (dsPointers->pSrcFilter)
    {
        while (dsPointers->pSrcFilter->Release() > 1); //Mandatory for Basler camera to decrement the reference count.
    }
    if (dsPointers->pVSC)
    {
        dsPointers->pVSC->Release();
    }

    dsPointers->pGraph = NULL;
    dsPointers->pControl = NULL;
    dsPointers->pEvent = NULL;
    dsPointers->pSrcFilter = NULL;
    dsPointers->pVSC = NULL;
}
