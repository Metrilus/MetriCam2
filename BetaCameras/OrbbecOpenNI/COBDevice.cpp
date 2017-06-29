#include "COBDevice.h"
#include <iostream>

struct  _device_vid_pid
{
    uint16_t vid;
    uint16_t pid;
};


COBDevice::COBDevice()
{
    m_camTag = 0;

#ifdef WIN32
    m_hUSBDevice = NULL;
#endif

#ifdef LINUX
	handle = NULL;
#endif
}


COBDevice::~COBDevice()
{
}

int COBDevice::InitDevice()
{
    int status;

#ifdef WIN32
	status = xnUSBInit();
#endif

#ifdef LINUX
	status = libusb_init(NULL);
#endif
    
    if (status != 0)
    {
        return -1;
    }

    return 0;
}


int COBDevice::OpenDeviceByPath(const char* astrDevicePaths)
{
	int status = -1;

#ifdef WIN32
	status = xnUSBOpenDeviceByPath(astrDevicePaths, &m_usbHandle);
#endif

#ifdef LINUX

	char uri[16];
	libusb_device **devs;

	int cnt = libusb_get_device_list(NULL, &devs);
	if (cnt < 0)
	{
	    libusb_free_device_list(devs, 1);
	    printf("failed to list device\n");
	    return -1;
	}

	struct libusb_device_descriptor desc;

	int i = 0;
	int found = 0;
	libusb_device *dev;

	while ((dev = devs[i++]) != NULL)
	{

	    int res = libusb_get_device_descriptor(dev, &desc);
	    if (res < 0)
	    {
			printf("failed to get device descriptor");
			libusb_free_device_list(devs, 1);
			return -1;
	    }

	    int vid = desc.idVendor;
	    int pid = desc.idProduct;
	    int bus = libusb_get_bus_number(dev);
	    int addr = libusb_get_device_address(dev);

	    sprintf(uri,"%04x/%04x@%d/%d", vid, pid, bus, addr);
	    //printf("%04x/%04x@%d/%d)\n", vid, pid, bus, addr);

	    if (0 == strncmp(uri, astrDevicePaths, 14))
	    {
			found = 1;
			break;
	    }
	}

	if (found == 0)
	{
		printf("failed to find device %s\r\n", astrDevicePaths);
		return -1;
	}

	status = libusb_open(dev, &handle);
	
#endif

	return status;
}


int COBDevice::OpenDevice(const char* astrDevicePaths)
{
	int status = OpenDeviceByPath(astrDevicePaths);

	if (status != 0)
	{
		std::cout << "Open Device ByPath Error " << astrDevicePaths << std::endl;
		return -1;
	}

    m_camTag = 0;
    return 0;
}


int COBDevice::CloseDevice()
{

#ifdef WIN32
	if (m_usbHandle != NULL)
	{
		xnUSBCloseDevice(m_usbHandle);
	}
#endif

#ifdef LINUX
	if (handle != NULL){
		libusb_close(handle);
	}
#endif

    return 0;
}


int COBDevice::SendCmd(uint16_t cmd, void *cmdbuf, uint16_t cmd_len, void *replybuf, uint16_t reply_len)
{
    int res;
    unsigned int actual_len;
    uint8_t obuf[0x400];
    uint8_t ibuf[0x200];

    cam_hdr *chdr = (cam_hdr*)obuf;
    cam_hdr *rhdr = (cam_hdr*)ibuf;

#ifdef WIN32
    if (m_usbHandle == 0) return -1;
#endif

#ifdef LINUX
	if (handle == 0) return -1;
#endif

    if (cmd_len & 1 || cmd_len > (0x400 - sizeof(*chdr))) {
        return -1;
    }

    chdr->magic[0] = 0x47;
    chdr->magic[1] = 0x4d;
    chdr->cmd = cmd;
    chdr->tag = m_camTag;
    chdr->len = cmd_len / 2;
    //chdr->aa = 0x0010;

    //copy the cmdbuf
    memcpy(obuf + sizeof(*chdr), cmdbuf, cmd_len);

#ifdef WIN32
    res = xnUSBSendControl(m_usbHandle, 
		XN_USB_CONTROL_TYPE_VENDOR, 
		0x00, 
		0x0000, 
		0x0000, 
		(XnUChar*)obuf, 
		cmd_len + sizeof(*chdr), 
		5000);
#endif

#ifdef LINUX
	res = libusb_control_transfer(handle,
		LIBUSB_ENDPOINT_OUT | LIBUSB_REQUEST_TYPE_VENDOR,
		0x00,
		0x0000,
		0x0000,
		(unsigned char *)obuf,
		cmd_len + sizeof(*chdr),
		1000);
#endif

    if (res < 0)
    {
        printf("send_cmd: Output control transfer failed (%d)\n", res);
        return res;
    }

    do
    {

#ifdef WIN32
        xnUSBReceiveControl(m_usbHandle, 
			XN_USB_CONTROL_TYPE_VENDOR, 
			0x00, 
			0x0000, 
			0x0000, 
			(XnUChar *)ibuf, 
			0x200, 
			&actual_len, 
			5000);
#endif

#ifdef LINUX
		actual_len = libusb_control_transfer(handle, 
			LIBUSB_ENDPOINT_IN | LIBUSB_REQUEST_TYPE_VENDOR, 
			0x00, 
			0x0000, 
			0x0000, 
			(unsigned char *)ibuf, 
			0x200, 
			1000);
#endif

        //print_dbg("send_cmd: actual length = %d\n", actual_len);
    } while ((actual_len == 0) || (actual_len == 0x200));

    //print_dbg("Control reply: %d\n", res);
    if (actual_len < (int)sizeof(*rhdr)) {
        printf("send_cmd: Input control transfer failed (%d)\n", res);
        return res;
    }
    actual_len -= sizeof(*rhdr);

    if (rhdr->magic[0] != 0x52 || rhdr->magic[1] != 0x42) {
        printf("send_cmd: Bad magic %02x %02x\n", rhdr->magic[0], rhdr->magic[1]);
        return -1;
    }
    if (rhdr->cmd != chdr->cmd) {
        printf("send_cmd: Bad cmd %02x != %02x\n", rhdr->cmd, chdr->cmd);
        return -1;
    }
    if (rhdr->tag != chdr->tag) {
        printf("send_cmd: Bad tag %04x != %04x\n", rhdr->tag, chdr->tag);
        return -1;
    }
    if (rhdr->len != (actual_len / 2)) {
        printf("send_cmd: Bad len %04x != %04x\n", rhdr->len, (int)(actual_len / 2));
        return -1;
    }

    if (actual_len > reply_len) {
        printf("send_cmd: Data buffer is %d bytes long, but got %d bytes\n", reply_len, actual_len);
        memcpy(replybuf, ibuf + sizeof(*rhdr), reply_len);
    }
    else {
        memcpy(replybuf, ibuf + sizeof(*rhdr), actual_len);
    }

    m_camTag++;

    return actual_len;

}
