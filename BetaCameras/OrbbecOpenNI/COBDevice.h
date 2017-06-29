#ifndef __COBDEVICE_H__
#define __COBDEVICE_H__

#define WIN32

#ifdef WIN32
#include "stdint.h"
#include "XnUSB.h"
#endif

#ifdef LINUX
#include "libusb.h"
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#endif


class COBDevice
{
public:
    COBDevice();
    ~COBDevice();
    int InitDevice();
    int EnumDevice();
    int OpenDevice(const char* path);
	int OpenDeviceByPath(const char* astrDevicePaths);
    int CloseDevice();
    int SendCmd(uint16_t cmd, void *cmdbuf, uint16_t cmd_len, void *replybuf, uint16_t reply_len);

private:

    typedef struct _cam_hdr{
        uint8_t     magic[2];
        uint16_t    len;
        uint16_t    cmd;
        uint16_t    tag;
    } cam_hdr;


    uint16_t                        m_vid;
    uint16_t                        m_pid;

	uint16_t m_camTag;

#ifdef WIN32
    XN_USB_DEV_HANDLE               m_usbHandle;
    const XnUSBConnectionString*    m_devicePath;
    XN_USB_DEV_HANDLE               m_hUSBDevice;
#else
	libusb_device_handle *handle;
#endif

};

#endif
