#ifndef __OB_UVCAPI_H__
#define __OB_UVCAPI_H__


#include <OniCTypes.h>
#include <stdint.h>

#include <vector>
#include <map>
#include <memory>
#include <functional>
#include <string>

#include "AutoResetEvent.h"

//To keep things simple, we should take the highest color resolution which matches the aspect ratio of the intrinsics and 
//has maximum FPS (in our case 30)

//With this mode we can get up to 50fps, but depending on the illumination it can drop down to 15fps
//#define UVC_COLOR_WIDTH 1280
//#define UVC_COLOR_HEIGHT 960

//With this mode we can get up to 26fps, but depending on the illumination it can drop down to 15fps
//#define UVC_COLOR_WIDTH 2592
//#define UVC_COLOR_HEIGHT 1944

#define UVC_COLOR_MEDIASUBTYPE MEDIASUBTYPE_NV12
//#define UVC_COLOR_MEDIASUBTYPE MEDIASUBTYPE_MJPG //Also works, but needs more processing time
//#define UVC_COLOR_MEDIASUBTYPE MEDIASUBTYPE_YUY2 //Delivers high framerates only for 640x480 or lower

struct ObUVCDevice;

void ProcessorCallback(const void *frame, int size, void *pstream);

int ObUVCInit(int uvcColorWidth, int uvcColorHeight, bool uvcColorFlipImage);
void ObUVCFillColorImage(unsigned char* colorData);
void ObUVCShutdown();
void ObUVCWaitForNewColorImage();
void ConvertNV12ToRGBImage(unsigned char* nv12_image);
void ConvertYUY2ToRGBImage(unsigned char* yuy2_image);
void ConvertNV12ToRGBImageAndFlip(unsigned char* nv12_image);
void ConvertYUY2ToRGBImageAndFlip(unsigned char* yuy2_image);

int enumerate_all_devices(std::map<std::string, std::shared_ptr<ObUVCDevice>> &devices);
int get_vendor_id(const ObUVCDevice & device);
int get_product_id(const ObUVCDevice & device);

void set_stream(ObUVCDevice & device, int subdevice_index, void * pstream);

typedef std::function<void(const void * frame, int size, void *pstream)> video_channel_callback;
void set_subdevice_mode(ObUVCDevice & device, int subdevice_index, video_channel_callback callback);

void start_streaming(ObUVCDevice & device,int subdevice_index=0);
void stop_streaming(ObUVCDevice & device, int subdevice_index=0);
#endif
