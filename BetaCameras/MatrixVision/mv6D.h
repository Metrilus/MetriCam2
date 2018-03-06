#ifndef MV6D_INCLUDED
#define MV6D_INCLUDED

#include <stddef.h>
#include "mv6DDataStructures.h"

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

#ifndef DOXYGEN_SHOULD_SKIP_THIS
 #ifdef MV6D_STATIC
  #define MV6D_API
  #define MV6D_CALL
 #else
  #define MV6D_CALL
  #if defined(_MSC_VER)
   // Microsoft
   #ifdef mv6D_EXPORTS
    #define MV6D_API __declspec(dllexport)
   #else // MV6D_DLL
    #define MV6D_API __declspec(dllimport)
   #endif // MV6D_DLL
  #elif defined(_GCC)
   // GCC
   #ifdef mv6D_EXPORTS
    #define MV6D_API __attribute__((visibility("default")))
   #else // MV6D_DLL
    #define MV6D_API
   #endif // MV6D_DLL
  #else
   // Unsupported compiler
   #pragma error Unknown dynamic link import/export semantics.
  #endif // other compiler
 #endif // MV6D_DLL
#endif // DOXYGEN_SHOULD_SKIP_THIS

/// \defgroup common Common structures and functions
/// \brief Common structures and functions to open and close the library. The library provides basic structures that may be used with OpenCV or HALCON without a memory-copy involved.

/// \defgroup depth Depth Measurement
/// \brief Depth measurement related structures and functions. The library uses stereoscopic depth measurement.

/// \defgroup flow Optical Flow
/// \brief Optical flow calculation. Optical flow describes the change in position of a pixel from one image to another image. If both images are within a sequence the optical flow shows the changes of the image over time.

/// \defgroup color Color image
/// \brief Color image generation. The library provides basic structures for accessing the color information from the camera that may be used with OpenCV or HALCON.

/// \defgroup gray Gray image
/// \brief Gray image generation. The library provides basic structures for accessing 8 bit gray images from the camera that may be used with OpenCV or HALCON.

/// \defgroup pick Box Picking
/// \brief The Box Picking module searches a volume for a given box. The box description includes the dimensions of the box. The result includes all found boxes with their pick-points. Each pick-point has a quality attribute and the transformation in world coordinates.

/// \defgroup prop Properties
/// \brief Properties are used to write and read attributes of the mv6D module.

/// \defgroup record Recording
/// \brief Raw images (pi-Format) can be recorded to harddrive during acquisition.

/// \brief whole version as string
/// \ingroup common
#define MV6D_VERSION_BUILD "2.4.1.313"

/// \brief Major version
/// \ingroup common
#define MV6D_VERSION_MAJOR 2

/// \brief Minor version
/// \ingroup common
#define MV6D_VERSION_MINOR 4

/// \brief Patch version
/// \ingroup common
#define MV6D_VERSION_PATCH 1

/// \brief Revision version
/// \ingroup common
#define MV6D_VERSION_REVISION 313

/// \brief Sets the log callback. This is a function that prints information from the library into the console.
/// \ingroup common
/// A log message is generated asynchronous to the current execution. The log message will
/// be called within a different thread.
/// \remarks Logging is independent from the mv6D handle. Thus multiple instances log to the same output.
/// \param h Library handle.
/// \param logCallback Log callback, called asynchronous. Set to NULL to disable logging.
/// \return \b rcOk successful.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_SetLogCallback(MV6D_Handle h, MV6D_LogCallback logCallback);

/// \brief Build version information of the mv6D library. 
/// \ingroup common
/// The version information that was actually used to build the library.
/// \param [out] major Major version.
/// \param [out] minor Minor version.
/// \param [out] patch Patch version.
/// \return Whole <B> Version </B> as string.
MV6D_API const char* MV6D_CALL MV6D_GetBuildVersion(int* major, int* minor, int* patch);

/// \brief Get result code as string.
/// \ingroup common
/// \param code Result code.
/// \return <B> Result code </B> description or NULL.
MV6D_API const char* MV6D_CALL MV6D_ResultCodeToString(MV6D_ResultCode code);

/// \brief Loads a configuration file.
/// \ingroup common
/// \param h6D Library handle.
/// \param path Path to configuration file.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument invalid path.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
/// \return \b rcUnknownError invalid config file.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_LoadConfiguration(MV6D_Handle h6D, const char* path);

/// \brief Save a configuration file.
/// \ingroup common
/// \param h6D Library handle.
/// \param path Path to file to save configuration in.
/// \return \b rcOk saving successfully.
/// \return \b rcInvalidLibraryHandle library handle is invalid.
/// \return \b rcInvalidArguement invalid path.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_SaveConfiguration( MV6D_Handle h6D, const char* path );

/// \brief Set depth attribute. Depth attributes are the sizes of reference and refinement masks, the used filter sets and the minimum distance for calculating stereo.
/// \ingroup depth
/// possible values for each MV6D_DepthAttribute can be seen in the enums MV6D_MinimumDistance, MV6D_FilterSet, MV6D_ReferenceMask and MV6D_RefinementMask.
/// \param h Library handle.
/// \param attribute Attribute
/// \param attributeValue Attribute value.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg attribute is not an Mv6D_DepthAttribute. \arg attributeValue is invalid.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_SetDepthPreset(MV6D_Handle h,
	MV6D_DepthAttribute attribute, int attributeValue);

/// \brief Get depth attribute. Depth attributes are the sizes of reference and refinement masks, the used filter sets and the minimum distance for calculating stereo.
/// \ingroup depth
/// possible values for each MV6D_DepthAttribute can be seen in the enums MV6D_MinimumDistance, MV6D_FilterSet, MV6D_ReferenceMask and MV6D_RefinementMask.
/// \param h Library handle.
/// \param attribute Attribute
/// \param [out] pAttributeValue Attribute value.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg attribute is not an Mv6D_DepthAttribute. \arg invalid pAttributeValue pointer.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_GetDepthPreset(MV6D_Handle h,
	MV6D_DepthAttribute attribute, int* pAttributeValue);

/// \brief Update supported compute device list count. The number of supported computing devices, that have been found.
/// \ingroup common
/// \param [out] pComputeDeviceListCount Number of supported compute devices in the system.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid pComputeDeviceListCount pointer.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_ComputeDeviceListUpdate(int* pComputeDeviceListCount);

/// \brief Get information from a compute device list entry. The information includes Device -vendor, -name and -id. 
/// \ingroup common
/// \param index List index of the supported compute device list.
/// \param info Information to ask for.
/// \param pBuffer Pointer to store the result.
/// \param [in, out] pBufferSize Size of the result buffer. Size written to the buffer.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid index. \arg info is not an MV6D_ComputeDeviceInfo enum. \arg invalid pBuffer pointer.
/// \return \b rcOutOfResources \arg pBufferSize too small.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_ComputeDeviceListGetInformation(int index,
	MV6D_ComputeDeviceInfo info,
	char* pBuffer,
	int* pBufferSize);

/// \brief Get a property handle. All supported Propertys can be accessed by using makros defined in the library header.
/// \ingroup prop
/// \param h Library handle.
/// \param pPropertyName Name of the property.
/// \param [out] pProperty Handle to the property.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid MV6D_Property pointer. \arg invalid pPropertyName string
/// \return \b rcNotFound \arg pPropertyName could not be found.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_PropertyGet(MV6D_Handle h,
	const char* pPropertyName,
	MV6D_Property* pProperty);

/// \brief Read value from a property. Needs a valid property handle.
/// \ingroup prop
/// \param h Library handle.
/// \param property Property handle.
/// \param pValue Value of the property.
/// \param [in, out] pValueSize Size of the value buffer. Size written to the buffer.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid property attribute. \arg invalid pValue pointer. \arg invalid pValueSize pointer.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_PropertyRead(MV6D_Handle h,
	MV6D_Property property,
	void* pValue,
	int* pValueSize);

/// \brief Write value to a property if it is within the bounds of the property. Needs a valid property handle.
/// \ingroup prop
/// \param h Library handle.
/// \param property Property handle.
/// \param pValue New value of the property.
/// \param valueSize Size of the value buffer.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid property attribute. \arg invalid pValue pointer.
/// \return \b rcOutOfResources \arg valueSize is too small.
/// \return \b rcOutOfBounds \arg value to set is out of the given properties bounds.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_PropertyWrite(MV6D_Handle h,
	MV6D_Property property,
	const void* pValue,
	int valueSize);

/// \brief Get maximum value of a property. Needs a valid property handle. Not every property has this!
/// \ingroup prop
/// \param h Library handle.
/// \param property Property handle.
/// \param [out] hasMaximum Set to 1 if property has a maximum value.
/// \param [out] pMaximum If not NULL the value is written to the buffer.
/// \param [in, out] pMaximumBufferSize Size of the value buffer. Size written to the buffer.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid property attribute. \arg invalid hasMaximum pointer. \arg invalid pMaximum pointer \arg invalid pMaximumBufferSize pointer.
/// \return \b rcOutOfResources \arg pMaximumBufferSize too small.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_PropertyGetMaximum(MV6D_Handle h,
	MV6D_Property property,
	int* hasMaximum,
	void* pMaximum,
	int* pMaximumBufferSize);
	
/// \brief Get minimum value of a property. Needs a valid property handle. Not every property has this!
/// \ingroup prop
/// \param h Library handle.
/// \param property Property handle.
/// \param [out] hasMinimum Set to 1 if property has a minimum value.
/// \param [out] pMinimum If not NULL the value is written to the buffer.
/// \param [in, out] pMinimumBufferSize Size of the value buffer. Size written to the buffer.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid property attribute. \arg invalid hasMinimum pointer. \arg invalid pMinimum pointer \arg invalid pMinimumBufferSize pointer.
/// \return \b rcOutOfResources \arg pMinimumBufferSize too small.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_PropertyGetMinimum(MV6D_Handle h,
	MV6D_Property property,
	int* hasMinimum,
	void* pMinimum,
	int* pMinimumBufferSize);

/// \brief Get step size of a property. Needs a valid property handle. Not every property has this!
/// \ingroup prop
/// \param h Library handle.
/// \param property Property handle.
/// \param [out] hasStepSize Set to 1 if property has a step size.
/// \param [out] pStepSize If not NULL the value is written to the buffer.
/// \param [in, out] pStepSizeBufferSize Size of the value buffer. Size written to the buffer.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid property attribute. \arg invalid hasStepSize pointer. \arg invalid pStepSize pointer \arg invalid pStepSizeBufferSize pointer.
/// \return \b rcOutOfResources \arg pStepSizeBufferSize too small.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_PropertyGetStepSize(MV6D_Handle h,
	MV6D_Property property,
	int* hasStepSize,
	void* pStepSize,
	int* pStepSizeBufferSize);

/// \brief Create an instance of the mv6D module. 
/// This triggers creation of binary database of graphic card kernels in the background if it has not been build yet or isn't up to date.
/// This will be done only the first time, mv6D library is startet.
/// The build process is reported in log callback.
/// If the right database exists and is up to date, it just gets loaded very quickly.
/// \ingroup common
/// \param [out] h Handle to the mv6D library.
/// \param useGPU GPU to use for processing. Use MV6D_ANY_GPU to let the system chose the preferred one.
/// \return \b rcOk successful.
/// \return \b rcGPUNotSupported The GPU device is not supported but CPU is supported
/// \return \b rcCPUNotSupported The CPU device is not supported but GPU is supported
/// \return \b rcOpenGLNotSupported The GPU and CPU device is not supported by the used OpenCL
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle. \arg h is a nullpointer.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_Create(MV6D_Handle* h, int useGPU);

/// \brief Close an instance of the mv6D module.
/// Closing might take a while if build of graphic card kernel database was triggered by MV6D_Create (only at the first time).
/// \ingroup common
/// \param h Library handle.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid h pointer.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_Close(MV6D_Handle h);

/// \brief Update internal device list. These are the camera-devices.
/// \ingroup common
/// \param h Handle to the mv6D library.
/// \param [out] pDeviceCount Number of devices.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid pDeviceCount pointer. 
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_DeviceListUpdate(MV6D_Handle h, int* pDeviceCount);

/// \brief Get serial of the given list index.
/// \ingroup common
/// \param h Handle to the mv6D library.
/// \param [out] pBuffer Buffer pointer.
/// \param bufferSize Size of the buffer pointed to by buffer.
/// \param [out] used Is camera used at the moment.
/// \param index Device index.
/// \remarks The index list can be obtained by UpdateDeviceList().
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid pBuffer pointer. \arg invalid used pointer. 
/// \return \b rcOutOfBounds \arg invalid index.
/// \return \b rcOutOfResources \arg buffer size is too small.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_DeviceListGetSerial(MV6D_Handle h, char* pBuffer, int bufferSize, int* used, int index);

/// \brief Open a given device. Can be used to open a physical device by serial or open a virtual device (a recorded sequence) by filepath.
/// This might take some time during first run of the mv6D library, if the database of graphic card kernels needs to be created.
/// In this case the build process is reported to log callback.
/// \ingroup common
/// \param [out] h Handle to the mv6D library.
/// \param serial Serial number of the device to connect to. A filepath must start with a leading ":" e.g. ":C:\sequences\input_%1%_%2%.pi". %1% is replaced by the sequence counter and %2% is replaced by the given camera image (master, slave1, slave2).
/// \return \b rcOk successful.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
/// \return \b rcInvalidArgument \arg invalid serial pointer.
/// \return \b rcNotFound \arg unable to find specified device.
/// \return \b rcInUse \arg device already in use.
/// \return \b rcLaserMalfunction \arg unable to activate laser.
/// \return \b rcLaserCritical \arg Laser state is critical, please replace it soon
/// \return \b rcLaserDead \arg Laser needs to be replaced as it doesn't seem to be working properly
/// \return \b rcFirmwareOutdated \arg firmware of camera is outdated and should be updated.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_DeviceOpen(MV6D_Handle h, const char* serial);

/// \brief Close a given device.
/// \ingroup common
/// \param [out] h Handle to the mv6D library.
/// \return \b rcOk successful.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_DeviceClose(MV6D_Handle h);

/// \brief Start acquisition, needs an opened device.
/// \ingroup common
/// \param [out] h Handle to the mv6D library.
/// \return \b rcOk successful.
/// \return \b rcNoDeviceOpened \arg no device opened.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_DeviceStart(MV6D_Handle h);

/// \brief Pause acquisition, needs an opened device.
/// \ingroup common
/// \param [out] h Handle to the mv6D library.
/// \return \b rcOk successful.
/// \return \b rcNoDeviceOpened \arg no device opened.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_DevicePause(MV6D_Handle h);

/// \brief Wait for new data from the device. Request buffer must be unlocked before using again, else there will be a memory leak!
/// \ingroup common
/// Please note that mv6D will continuously check for the camera matrix to change. If the camera
/// becomes uncalibrated it will be automatically corrected, thus pCameraParameter may change over the time.
/// \param h Library handle.
/// \param [out] pRequestBuffer Request buffer. Make sure you unlock the buffer before calling the function again!
/// \param [out] dropped Number of dropped frames since last call. May be set to NULL.
/// \param timeout Timeout in milliseconds.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid pRequestBuffer pointer.
/// \return \b rcTimedOut \arg didn't finish operation in time
/// \return \b rcTriggerTimedOut \arg didn't get any trigger pulse in time. This is only used, if external trigger subsample is active (see MV6D_PROPERTY_ACQUISITION_TRIGGER_SUBSAMPLE).
/// \return \b rcOutOfBounds \arg timeout value less than zero.
/// \return \b rcNoDeviceOpened \arg no device openend.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_DeviceResultWaitFor(MV6D_Handle h,
	struct MV6D_RequestBuffer** pRequestBuffer,
	int* dropped,
	int timeout);

/// \brief Unlock request buffer.
/// \ingroup common
/// \param h Library handle.
/// \param pRequestBuffer Resource to unlock.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid pRequestBuffer pointer or object.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_UnlockRequest(MV6D_Handle h, struct MV6D_RequestBuffer* pRequestBuffer);

/// \brief Find boxes within a given volume. The given volume has to be set in 3D world coordinates.
/// \ingroup pick
/// \param h Library handle.
/// \param pRequestBuffer Request buffer.
/// \param pCameraWorldTransformation Camera to world transformation.
/// \param pVolumeOfInterest Volume of interest.
/// The volume of interest in world coordinates.
/// \param pBoxDescription Box description.
/// \param [out] pPickBoxResultList List of PickBox entries.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid pRequestBuffer pointer. \arg invalid pCameraWorldTransformation pointer. \arg invalid pVolumeOfInterest pointer. \arg invalid pBoxDescription pointer. \arg invalid pPickBoxResultList pointer. \arg pPickBoxResultList not unlocked.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_PickBoxFind(MV6D_Handle h,
	struct MV6D_RequestBuffer* pRequestBuffer,
	struct MV6D_CameraWorldTransformation* pCameraWorldTransformation,
	struct MV6D_VolumeOfInterest* pVolumeOfInterest,
	struct MV6D_PickBoxDescription* pBoxDescription,
	struct MV6D_PickBoxResultList** pPickBoxResultList);

/// \brief Unlock pick box result buffer.
/// \ingroup pick
/// \param h Library handle.
/// \param pPickBoxResultList Resource to unlock.
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid pPickBoxResultList pointer or object.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_UnlockPickBox(MV6D_Handle h, struct MV6D_PickBoxResultList* pPickBoxResultList);

/// \brief Start recording of a specific amount of frames.
/// \ingroup record
/// \param h Library handle.
/// \param pAbsDir Directory where recorded frames will be saved.
/// \param mode Recording mode.
/// \param frames Number of frames to record (set to 0 if mode is set to rmUntilStopped).
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument \arg invalid pAbsDir pointer. \arg pAbsDir empty. \arg mode is not an MV6D_RecordMode. \arg frames value less than zero.
/// \return \b rcNoDeviceOpened \arg no device opened.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_RecordStart(MV6D_Handle h, const char* const pAbsDir, MV6D_RecordMode mode, int frames);

/// \brief Adds a marker measurement along with its known world position to the auto world alignment database.
/// \remarks The measured targets will be made available in the requestBuffer which can be obtained by MV6D_DeviceResultWaitFor.
/// \ingroup awa
/// \param h Library handle.
/// \param numRequests The size of the requestList array (maximum 8)
/// \param requestList The list of marker measurement requests
/// \return \b rcOk successful.
/// \return \b rcInvalidArgument if more than 8 marker measurement requests are given.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_AddMarkerMeasurements(MV6D_Handle h, int numRequests, struct MV6D_MarkerMeasurementRequest* requestList);

/// \brief Triggers the computation of the world alignment matrix using all previously added marker measurements.
/// \remarks The resulting transformation will be made available through the requestBuffer which is obtained by MV6D_DeviceResultWaitFor.
/// \ingroup awa
/// \param h Library handle.
/// \return \b rcOk successful.
/// \return \b rcInvalidLibraryHandle \arg invalid mv6D handle.
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_ComputeMarkerWorldTransformation(MV6D_Handle h);

/// \brief Runs export of support data to a directory
/// \remarks The type of export data is defined as combination of MV6D_SupportDataType flags
/// \param h The used library handle
/// \param directoryPath The path to the directory to export all support data to
/// \param exportDataTypes All types of support data that should be exported in combination of MV6D_SupportDataType flags.
///                        After call of this function all flags that were successfully exported is reset to 0, so in best case you get 0 after calling this function
/// \return Result code of operation. In error case it's always the first error in the list of operations. For details see the log callback.
///         \b rcOk                   All exported successsfully
///         \b rcInvalidLibraryHandle Handle isn't valid or in invalid state
///         \b rcNoDeviceOpened       No connection to device could be found
///         \b rcUnknownError         All unspecific errors, see log file for details
///         \b rcInvalidArgument      Some of the arguments (probably directory path) were not useful for support data export
///         \b rcTimedOut             Operation couldn't be finished in time
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_ExportSupportData(MV6D_Handle h, const char* const directoryPath, int* exportDataTypes );

/// \brief checks whether laser still seems ok
/// The laser has limited life time, so this method checks, whether laser seems all ok or
/// laser life time will end in near future.
/// This function might take some time on camera, so image acquisition must stop for a short time,
/// so don't call this function if you are currently requesting image data from camera.
/// \param h The used library handle
/// \return Result code of operation
///         rcOk                   No failure of laser detected, no need for a replacement soon
///         rcInvalidLibraryHandle Library handle is invalid, no check could be started.
///         rcTimedOut             Didn't get reply on our state request on laser, no data to decide laser state
///         rcLaserCritical        Laser state is critical, please replace it soon
///         rcLaserDead            Laser seems to work no more properly, please replace it
MV6D_API MV6D_ResultCode MV6D_CALL MV6D_CheckLaserValidity( MV6D_Handle h );

#ifdef __cplusplus
}
#endif // __cplusplus

#endif
