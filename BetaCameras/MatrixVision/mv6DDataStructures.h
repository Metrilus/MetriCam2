#pragma once

#ifndef MV6D_DATASTRUCTURES_H
#define MV6D_DATASTRUCTURES_H

#pragma pack(push, 4)

/// \brief Errors reported by the mv6D library.
/// \ingroup common
/**
*  These are errors which might occur in a background thread
*  or while working with the library directly.
*/
enum MV6D_ResultCode
{
    /// \brief The function call was executed successfully.
    rcOk = 0,
    /// \brief The library or another module hasn't been initialized properly.
    /// This error occurs if the user tries, for e.g., to close the device manager without
    /// having initialized it before or if a library used internally or a module or device associated with that library has has not been initialized properly.
    rcNotInitialized = -4096,
    /// \brief An unknown error occurred while processing a user called driver function.
    rcUnknownError,
    /// \brief A driver function has been called with an invalid device handle.
    rcInvalidHandle,
    /// \brief A driver function has been called but one or more of the input parameters are invalid.
    /// There are several possible reasons for this error:
    /// - an unassigned pointer has been passed to a function, that requires a valid pointer
    /// - one or more of the passed parameters are of an incorrect type
    /// - one or more parameters contain an invalid value (e.g. a filename that points to a file that can't
    /// be found, a value, that is larger or smaller than the allowed values.
    rcInvalidArgument,
    /// \brief Not implemented.
    /// Some algorithm may not be implemented on the current platform.
    rcNotImplemented,
    /// \brief Out of bound access.
    /// Access out of bound requested.
    rcOutOfBounds,
    /// \brief Out of resources.
    rcOutOfResources,
    /// \brief Timed out.
    rcTimedOut,
    /// \brief Already in use.
    rcInUse,
    /// \brief GPU not supported.
    rcGPUNotSupported,
    /// \brief CPU not supported.
    rcCPUNotSupported,
    /// \brief Laser malfunctioned.
    rcLaserMalfunction,
    /// \brief Invalid mv6D Handle.
    rcInvalidLibraryHandle,
    /// \brief no Device opened.
    rcNoDeviceOpened,
    /// \brief Input Parameter has invalid values order (ie. min > max).
    rcInvalidOrder,
    /// \brief No license was found or all licenses are invalid.
    /// \remarks Deprecated
    rcNoValidLicense,
    /// \brief Not found.
    rcNotFound,
    /// \brief Camera is not supported by this version of mv6D.
    rcCameraNotSupported,
    /// \brief No supported OpenCL device found by the mv6D.
    rcOpenCLNotSupported,
    /// \brief Laser state is critical and should be replaced soon
    rcLaserCritical,
    /// \brief Laser doesn't seem to work any more
    rcLaserDead,
    /// \brief In case of external trigger subsample no trigger pulse was received in time
    rcTriggerTimedOut,
    /// \brief Firmware of camera is outdated, please update firmware by mvDeviceConfigure.
    rcFirmwareOutdated
};

/// \brief Log level.
/// \ingroup common
enum MV6D_LogLevel
{
    /// \brief Information.
    llInfo = 1,
    /// \brief Warning.
    llWarning = 2,
    /// \brief Error.
    llError = 4
};

/// \brief Module handle.
/// \ingroup common
typedef struct {} *MV6D_Handle;

/// \brief Library handle.
/// \ingroup prop
typedef struct {} *MV6D_Property;

/// \brief ABGR color format.
/// \remarks GL_BGRA when using OpenGL.
struct MV6D_ColorABGR
{
    /// \brief Blue.
    unsigned char b;
    /// \brief Green.
    unsigned char g;
    /// \brief Red.
    unsigned char r;
    /// \brief Alpha.
    /// \remarks Alpha value = 0 is transparent, alpha value = 255 is opaque.
    unsigned char a;
};

/// \brief Image buffer.
/// \ingroup color
struct MV6D_ColorBuffer
{
    /// \brief Data pointer, continuous memory.
    /// Each element specifies the color value of pixel in ABGR format.
    /// Access data (row, column) by using MV6D_ColorBuffer->pData[row * MV6D_ColorBuffer->iWidth + column]
    const struct MV6D_ColorABGR* pData;
    /// \brief Width.
    int iWidth;
    /// \brief Height.
    int iHeight;
};

/// \brief gray image buffer
/// \ingroup gray
struct MV6D_GrayBuffer
{
    /// \brief Data pointer, continuous memory.
    /// Each value is the grey value in 0...255.
    /// Access data (row, column) by using MV6D_GreyBuffer->data[row * MV6D_GreyBuffer->iWidth + column]
    const unsigned char* pData;
    /// \brief Width.
    int iWidth;
    /// \brief Height.
    int iHeight;
};

/// Flow element constants.
/// \ingroup flow
enum MV6D_Flow
{
    /// \brief Invalid flow value.
    /// \see MV6D_FlowElement::Delta::iHorizontal
    /// \see MV6D_FlowElement::Delta::iVertical
    fInvalidFlow = 32767,
    /// \brief Invalid flow value
    /// \see MV6D_FlowElement::iRaw
    fInvalidFlowRaw = ( fInvalidFlow << 16 | fInvalidFlow )
};

/// \brief Flow element.
/// \ingroup flow
/// Subtract attributes of the current pixel (u, v) to get the source pixel.
union MV6D_FlowElement
{
    /// \brief Delta motion.
    struct Delta
    {
        /// \brief Delta motion [pixels] in horizontal direction.
        short iHorizontal;
        /// \brief Delta motion [pixels] in vertical direction. 
        short iVertical;
    } delta;

    /// \brief Raw flow.
    /// INVALID_FLOW indicates an invalid flow.
    int iRaw;
};

/// \brief Fully describes a flow buffer.
/// \ingroup flow
struct MV6D_FlowBuffer
{
    /// \brief Data pointer, continuous memory.
    const union MV6D_FlowElement* pData;
    /// \brief Width.
    int iWidth;
    /// \brief Height.
    int iHeight;
};

/// \brief Fully describes a depth buffer.
/// \ingroup depth
/// Use the following formula to calculate the X, Y, Z position from the depth measurement.
/// (U, V) are the pixel-index within the depth image. Distance is the depth-value at the
/// given pixel-index. Width is the depth image width and height is the depth image height.
/// \ref MV6D_RequestBuffer::focalLength
/// \f[
///	x = \frac{(U - \frac{width}{2})}{focallength} \times distance
/// \f]
/// \f[
///	y = \frac{(V - \frac{height}{2})}{focallength} \times distance
/// \f]
/// \f[
///	z = distance
/// \f]
struct MV6D_DepthBuffer
{
    /// \brief Data pointer, continuous memory.
    /// \remarks A distance value smaller or equal to 0 is invalid.
    /// Each element specifies the distance in meters.
    /// Access data (row, column) by using MV6D_DepthBuffer::pData[row * MV6D_DepthBuffer->iWidth + column]
    const float* pData;
    /// \brief Width.
    int iWidth;
    /// \brief Height.
    int iHeight;
};

/// \brief Point Cloud
/// \ingroup depth
/// The point cloud consists of three buffer. One buffer for each dimension (x, y, z).
/// The index of the arrays for a certain point are equal.
/// E.g. Point 42:  x-value: bufferX[42], y-value: bufferY[42], z-value: bufferZ[42]
/// Thou we are using an ordered list, any point cloud values with x=y=z=0 are invalid.
/// The size of the arrays is equal.
struct MV6D_PointCloud
{
    /// \brief Size of the buffers
    int iSize;

    /// \brief Data pointer to the x-buffer, continuous memory.
    /// Each element specifies the distance in meters.
    const float* pDataX;

    /// \brief Data pointer to the x-buffer, continuous memory.
    /// Each element specifies the distance in meters.
    const float* pDataY;

    /// \brief Data pointer to the x-buffer, continuous memory.
    /// Each element specifies the distance in meters.
    const float* pDataZ;
};



/// \brief A marker alignment measurement request containing the id of a marker that is to be measured in addition to its known world position. Multiple such requests can be used together to construct a automatic world alignment matrix.
/// \ingroup awa
struct MV6D_MarkerMeasurementRequest
{
    /// \brief The unique identifier of the marker
    int ID;

    /// \brief The known world X position (meters)
    double worldPositionX;
    /// \brief The known world Y position (meters)
    double worldPositionY;
    /// \brief The known world Z position (meters)
    double worldPositionZ;
};

/// \brief The result of a marker measurement process
/// \ingroup awa
struct MV6D_MarkerMeasurement
{
    /// \brief The unique identifier of the marker
    int ID;

    /// \brief The known world X position (meters)
    double worldPositionX;
    /// \brief The known world Y position (meters)
    double worldPositionY;
    /// \brief The known world Z position (meters)
    double worldPositionZ;

    /// \brief The measured device X position (meters)
    double devicePositionX;
    /// \brief The measured device Y position (meters)
    double devicePositionY;
    /// \brief The measured device Z position (meters)
    double devicePositionZ;

    /// \brief The measured frame U position (pixels)
    double framePositionU;
    /// \brief The measured frame V position (pixels)
    double framePositionV;
};

/// \brief The result of the marker based auto world alignment process, containing the transformation from device to world coordinates and some quality measure
/// \ingroup awa
struct MV6D_MarkerWorldAlignmentTransformation
{
    /// \brief World rotation matrix.
    double R11;
    /// \brief World rotation matrix.
    double R12;
    /// \brief World rotation matrix.
    double R13;
    /// \brief World rotation matrix.
    double R21;
    /// \brief World rotation matrix.
    double R22;
    /// \brief World rotation matrix.
    double R23;
    /// \brief World rotation matrix.
    double R31;
    /// \brief World rotation matrix.
    double R32;
    /// \brief World rotation matrix.
    double R33;

    /// \brief World translation matrix.
    double T1;
    /// \brief World translation matrix.
    double T2;
    /// \brief World translation matrix.
    double T3;

    /// \brief The remaining error in world space (meters)
    ///  a large number indicates incorrect user input values
    double spatialError;
};

/// \brief Data from the marker auto alignment module
/// \ingroup awa
struct MV6D_MarkerWorldAlignmentInfo
{
    /// \brief The number of new marker measurements
    /// \remarks May be less than the number of requested measurements if not all were found.
    int numMeasurements;

    /// \brief A list of new marker elements
    /// \remarks Contains exactly numMeasurements elements, and will be NULL if no measurements were found
    MV6D_MarkerMeasurement* markerMeasurements;

    /// \brief The transformation from device to world coordinates
    /// \remarks Will be null if no transformation could be calculated
    MV6D_MarkerWorldAlignmentTransformation* transformation;
};

/// \brief Request buffer.
/// \ingroup common
/// The request buffer holds the depth measurement data, the flow data and color information.
/// \remarks The RequestBuffer must be unlocked after finished processing, see UnlockRequest.
struct MV6D_RequestBuffer
{
    /// \brief Image buffer.
    /// \remarks Pixel mapped (color, flow, depth).
    struct MV6D_ColorBuffer colorMapped;

    /// \brief Depth buffer.
    /// \remarks Pixel mapped (color, flow, depth).
    struct MV6D_DepthBuffer depthMapped;

    /// \brief Flow buffer.
    /// \remarks Pixel mapped (color, flow, depth).
    struct MV6D_FlowBuffer flowMapped;

    /// \brief Raw depth buffer.
    /// \remarks Not pixel mapped.
    /// The raw depth buffer is not mapped with the color nor
    /// the flow buffer. Therefore it holds more depth information
    /// as the mapped depth buffer.
    struct MV6D_DepthBuffer depthRaw;

    /// \brief Point cloud
    /// \remarks Point cloud in meters
    /// The point cloud is mapped with the color image.
    struct MV6D_PointCloud pointCloud;

    /// \brief Information about marker measurements and the resulting world alignment.
    /// \remarks Will be NULL if no measurement or alignment request was made.
    struct MV6D_MarkerWorldAlignmentInfo* markerAlignmentInfo;

    /// \brief Valid calibration.
    /// The system is always re-calibrating itself thus the calibration.
    /// may change over time but stays valid.
    int hasValidCalibration;

    /// \brief Absolute timestamp [s].
    /// The timestamp may use an internal clock. Thus it is not synchronized.
    /// to the system clock.
    double timestamp;

    /// \brief Focal length [pel].
    /// Needed to calculate the world coordinates (X, Y, Z) from the depth measurement.
    double focalLength;

    /// New with version 2.1.0
    //@{

    /// \brief Raw gray image from master camera head (first infrared camera for stereo calculation).
    /// \remarks This image has no compensation of distortions.
    struct MV6D_GrayBuffer rawMaster;

    /// \brief Raw gray image from slave1 camera head (second infrared camera for stereo calculation).
    /// \remarks This image has no compensation of distortions.
    struct MV6D_GrayBuffer rawSlave1;

    /// \brief Raw gray image from slave 2 camera head (color camera or third infrared camera for stereo calculation).
    /// In case of color camera this is the bayer image.
    /// \remark This image has no compensation of distortions.
    struct MV6D_GrayBuffer rawSlave2;

    /// \brief Debayered color image from slave 2 camera.
    /// This might contain no data, if we don't have a color camera here.
    /// \remarks This image has no compensation of distortions.
    struct MV6D_ColorBuffer rawColor;

    //@}
};

/// \brief Camera world transformation.
/// \ingroup pick
/// Camera rotation and translation in world coordinates.
/// \remarks Rotation order is "X, Y', Z''".
/// \f[
///	R = R_z \times R_y \times R_x
/// \f]
struct MV6D_CameraWorldTransformation
{
    /// Position of the camera [m].
    double positionX;
    /// Position of the camera [m].
    double positionY;
    /// Position of the camera [m].
    double positionZ;

    /// Rotation of the camera [arc degree].
    double rotationX_deg;
    /// Rotation of the camera [arc degree].
    double rotationY_deg;
    /// Rotation of the camera [arc degree].
    double rotationZ_deg;
};

/// \brief Volume of interest.
/// \ingroup pick
/// The volume of interest defines an axes aligned cuboid by its center position (positionX, positionY, positionZ)
/// and its length, breadth and height (sizeX, sizeY, sizeZ).
struct MV6D_VolumeOfInterest
{
    /// \brief Center position X [m].
    double positionX;
    /// \brief Center position Y [m].
    double positionY;
    /// \brief Center position Z [m].
    double positionZ;

    /// \brief Size X [m].
    double sizeX;
    /// \brief Size Y [m].
    double sizeY;
    /// \brief Size Z [m].
    double sizeZ;
};

/// \brief Box description.
/// \ingroup pick
struct MV6D_PickBoxDescription
{
    /// \brief Dimension in A [m].
    double dimensionA;
    /// \brief Dimension in B [m].
    double dimensionB;
    /// \brief Dimension in C [m].
    double dimensionC;

    /// \brief Use detailed texture analysis
    int textureAnalysis;
};

/// \brief Pick point result.
/// \ingroup pick
struct MV6D_PickBoxResult
{
    /// \brief Found dimension of stage.
    double sizeX;
    /// \brief Found dimension of stage.
    double sizeY;

    /// \brief World rotation matrix.
    double R11;
    /// \brief World rotation matrix.
    double R12;
    /// \brief World rotation matrix.
    double R13;
    /// \brief World rotation matrix.
    double R21;
    /// \brief World rotation matrix.
    double R22;
    /// \brief World rotation matrix.
    double R23;
    /// \brief World rotation matrix.
    double R31;
    /// \brief World rotation matrix.
    double R32;
    /// \brief World rotation matrix.
    double R33;

    /// \brief World translation matrix.
    double T1;
    /// \brief World translation matrix.
    double T2;
    /// \brief World translation matrix.
    double T3;

    /// \brief Quality of found stage [0.0 to 1.0].
    /// \remarks Best value is 1.
    /// At first all stages are extracted from the depth
    /// information. The quality is correlated to
    /// the depth measurement of the box.
    double boxFitQuality;

    /// \brief Robustness of the texture support [0.0 to 1.0].
    /// \remarks Best value is 1.
    /// If there is too much texture on the stage it may not
    /// be substracted from its surface. A low quality
    /// value means that the texture leads to ambiguity.
    double contourSupportQuality;

    /// \brief Quality of the pick point [0.0 to 1.0].
    /// \remarks Best value is 1.
    /// The pick point is calculated using the depth measurement
    /// and a texture analysis. The quality of the pick-point
    /// is related to its actually support from depth measurement.
    double pickPointQuality;
};

/// \brief List of Pick Boxes.
/// \ingroup pick
/// \remarks PickBoxResultList must be unlocked after usage.
struct MV6D_PickBoxResultList
{
    /// \brief Found boxes.
    struct MV6D_PickBoxResult* entries;
    /// \brief Number of boxes in the list.
    int entryCount;
};

/// \brief Depth attribute.
/// \ingroup depth
enum MV6D_DepthAttribute
{
    /// \brief Minimum distance.
    /// \ref MV6D_MinimumDistance
    daMinimumDistance = 0,
    /// \brief Filter set
    /// \ref MV6D_FilterSet
    daFilterSet,
    /// \brief Reference mask
    /// \ref MV6D_ReferenceMask
    daReferenceMask,
    /// \brief Refinement mask
    /// \ref MV6D_RefinementMask
    daRefinementMask,
    /// \brief Used stereo algorithm
    /// \ref MV6D_Stereo_Algorithm
    daStereoAlgorithm
};

/// \brief Minimum distance.
/// Measurement starts at that distance. The performance increases with a higher
/// value.
/// \ingroup depth
/// \remarks Attribute type is int.
/// \ref daMinimumDistance
enum MV6D_MinimumDistance
{
    /// \brief Custom
    mindCustom = -1,
    /// \brief Minimum distance of 800mm
    minDist800mm,
    /// \brief Minimum distance of 1200mm
    minDist1200mm,
    /// \brief Minimum distance of 1600mm
    minDist1600mm
};

/// \brief Stereo reference mask.
/// Mask size used for initial stereo matching. A higher mask size reduces noise
/// but leads to foreground fattening. The performance decreases by setting
/// a higher mask size.
/// \ingroup depth
/// \remarks Attribute type is int.
/// \ref daReferenceMask
enum MV6D_ReferenceMask
{
    /// \brief Custom mask
    referenceCustom = -1,
    /// \brief 13x13 mask
    reference13x13 = 0,
    /// \brief 15x15 mask
    reference15x15,
    /// \brief 17x17 mask
    reference17x17,
    /// \brief 19x19 mask
    reference19x19,
    /// \brief 21x21 mask
    reference21x21
};

/// \brief Stereo refinement mask.
/// Mask size used for final stereo matching. A higher mask size reduces noise
/// but leads to foreground fattening. The performance decreases by setting
/// a higher mask size.
/// \ingroup depth
/// \remarks Attribute type is int.
/// \ref daRefinementMask
enum MV6D_RefinementMask
{
    /// \brief Custom
    rmCustom = -1,
    /// \brief 11x11
    rm11x11 = 0,
    /// \brief 17x17
    rm17x17,
    /// \brief 21x21
    rm21x21,
    /// \brief 31x31
    rm31x31,
    /// \brief 41x41
    rm41x41
};

/// \brief Filter set.
/// \ingroup depth
/// \remarks Attribute type is int.
/// \ref daFilterSet
/// The filter set will prepare the measurement data using various methods.
/// There is no over-all best filter setting.
enum MV6D_FilterSet
{
    /// \brief Custom configuration.
    /// \remarks Not to be used in production environment.
    fsCustom = -1,
    /// \brief None.
    /// No filter will be applied.
    fsNone = 0,
    /// \brief Persons
    /// A filter-set optimized for whole person matching.
    /// \remarks If there are objects in close and far distance, you might want to switch to WideRangePersons.
    fsPerson,
    /// \brief Boxes
    /// A filter-set optimized for box detection.
    /// \remarks If there are objects in close and far distance, you might want to switch to WideRangePersons.
    fsBoxes,
    /// \brief Person (wide range),
    /// A filter-set optimized for whole person matching within a wide range.
    /// \remarks If objects distances are within a certain range, you might want to switch to Persons.
    fsPersonWideRange,
    /// \brief Boxes (wide range),
    /// A filter-set optimized for box detection within a wide range.
    /// \remarks If objects distances are within a certain range, you might want to switch to Boxes.
    fsBoxesWideRange,
    /// \brief Allround
    /// A general filter-set optimized for most scenes
    fsAllround,
    /// \brief fsAllroundInterpolation
    /// A general filter-set optimized for most scenes with interpolation
    fsAllroundInterpolation
};

/// \brief Stereo algorithm settings.
/// \ingroup depth
/// \remarks Attribute type is int. Valid range is 0-2.
/// There exist different algorithms to calculate the depthmap using various techniques. 
/// There is no over-all best algorithm.
enum MV6D_Stereo_Algorithm
{
    /// \brief Use the Block Matching (BM) disparity algorithm.
    stereoAlgoBM = 0,
    /// \brief Use the Rapid Semi Global Matching (RSGM) disparity algorithm.
    /// \remarks NBt yet GPU optimized.
    stereoAlgoRSGM,
    /// \brief Use the Semi Global Block Mathcing  (SGBM) disparity algorithm.
    /// \remarks NBt yet GPU optimized.
    stereoAlgoSGBM
};

/// \brief Log callback.
/// \ingroup common
/// \param logLevel Log level.
/// \param pFile Name of the file.
/// \param pFunction Function name.
/// \param line Line number.
/// \param pMessage Log message.
typedef void( *MV6D_LogCallback )( MV6D_LogLevel logLevel,
                                   const char* pFile,
                                   const char* pFunction,
                                   int line,
                                   const char* pTimestamp,
                                   const char* pMessage );

/// \brief Compute device information.
enum MV6D_ComputeDeviceInfo
{
    /// \brief Vendor string.
    giVendor = 0,
    /// \brief Name of the device.
    giName = 1,
    /// \brief Identifier.
    /// \remarks Hardware changes to the system may change the identifier of a device.
    giID = 2
};

/// \brief Common constants
/// \ingroup common
enum MV6D_CommonConstants
{
    /// \brief Use any GPU when creating the mv6D module.
    /// Used as default value to MV6D_Create.
    MV6D_ANY_GPU = -1,
    /// \brief Use any device when creating the mv6D module.
    MV6D_ANY_DEVICE = -2,
    /// \brief Use CPU as computing device.
    MV6D_ANY_CPU = -3,
    /// Used as default value to MV6D_Create.
    MV6D_ANY_AMD_GPU = -4,
    /// Used as default value to MV6D_Create.
    MV6D_ANY_INTEL_GPU = -5,
};

/// \brief Recording mode
enum MV6D_RecordMode
{
    /// \brief Indicates that exactly N frames shall be recorded
    rmNFrames = 1
};

/// \brief Acquisition mode
enum MV6D_AcquisitionMode
{
    /// \brief Continuous stream of frames at given framerate
    amContinuous = 0,
    /// \brief Step wise acquisition of frames, one by one
    amStep = 1,
    /// \brief Trigger frame by software
    amTriggerSoftware = 2,
    /// \brief Trigger frame by hardware
    amTriggerHardware = 3
};

/// \brief Flags for all types of support data to export
enum MV6D_SupportDataType : int
{
    /// \brief export nothing at all
    sdtNone = 0,
    /// \brief data of the PC system
    sdtSystemData = 1 << 0,
    /// \brief export configuration data on connected camera
    sdtCameraConfigurationData = 1 << 1,
    /// \brief export Calibration data on camera
    sdtCameraCalibrationData = 1 << 2,
    /// \brief export camera properties (as XML)
    sdtCameraProperties = 1 << 3,
    /// \brief export little sequence form camera
    sdtCameraSequence = 1 << 4,
    /// \brief export all data for support
    sdtAll = -1
};

#pragma pack(pop)

/// \brief Frame-rate [Hz]
/// \ingroup prop
/// Frame-rate is given in frames per second [Hz].
/// \remarks Value-type is double.
#define MV6D_PROPERTY_FRAMERATE ("camera/framerate")

/// \brief Acquisition status
/// \ingroup prop
/// \remarks Value-type is int.
#define MV6D_PROPERTY_ACQUISITION_STATUS ("acquisition/status")

/// \brief Acquisition mode
/// \ingroup prop
/// \remarks Value-type is int.
#define MV6D_PROPERTY_ACQUISITION_MODE ("acquisition/mode")

/// \brief Subsampling for external trigger, i.e., only each nth external trigger signal really triggers acquisition of image (and 3D) data.
/// \brief only raising flank is used at external trigger, exposure time is used as set by parameter.
/// \image html trigger_subsample.png
/// \ingroup prop
/// \remarks Value type is int.
#define MV6D_PROPERTY_ACQUISITION_TRIGGER_SUBSAMPLE ("acquisition/triggerSubsample")

/// \brief Trigger Software
/// \ingroup prop
/// \remarks Value-type is int.
#define MV6D_PROPERTY_ACQUISITION_TRIGGERSOFTWARE ("acquisition/triggerSoftware")

/// \brief White balance automatic mode.
/// \ingroup prop
/// \remarks Value-type is int.
#define MV6D_PROPERTY_WHITEBALANCE_AUTO ("camera/whitebalance/auto")

/// \brief White balance, Area of Interest, X in pixels
/// \ingroup prop
/// Pixels from top-left corner.
/// \remarks Value-type is int.
#define MV6D_PROPERTY_WHITEBALANCE_AOI_X ("camera/whitebalance/aoi/x")

/// \brief White balance, Area of Interest, Y in pixels
/// \ingroup prop
/// Pixels from top-left corner.
/// \remarks Value-type is int.
#define MV6D_PROPERTY_WHITEBALANCE_AOI_Y ("camera/whitebalance/aoi/y")

/// \brief White balance, Area of Interest, width in pixels
/// \ingroup prop
/// \remarks Value-type is int.
#define MV6D_PROPERTY_WHITEBALANCE_AOI_WIDTH ("camera/whitebalance/aoi/width")

/// \brief White balance, Area of Interest, height in pixels
/// \ingroup prop
/// \remarks Value-type is int.
#define MV6D_PROPERTY_WHITEBALANCE_AOI_HEIGHT ("camera/whitebalance/aoi/height")

/// \brief White balance, Manual, Red
/// \ingroup prop
/// Factor applied to red pixels.
/// \remarks Value-type is double.
#define MV6D_PROPERTY_WHITEBALANCE_MANUAL_RED ("camera/whitebalance/manual/factor_red")

/// \brief White balance, Manual, Blue
/// \ingroup prop
/// Factor applied to blue pixels.
/// \remarks Value-type is double.
#define MV6D_PROPERTY_WHITEBALANCE_MANUAL_BLUE ("camera/whitebalance/manual/factor_blue")

/// \brief Automatic exposure and gain control
/// \ingroup prop
/// \remarks Value-type is int.
#define MV6D_PROPERTY_CAMERA_CONTROL_AUTO ("camera/control/auto")

/// \brief Exposure time in seconds.
/// \ingroup prop
/// The exposure time is valid for depth measurement as well as for the color camera.
/// \remarks Value-type is double.
#define MV6D_PROPERTY_CAMERA_CONTROL_EXPOSURE ("camera/control/manual/exposure")

/// \brief Analog gain factor.
/// \ingroup prop
/// \remarks Value-type is double.
/// Analog gain factor applied to the depth measurement.
#define MV6D_PROPERTY_CAMERA_CONTROL_ANALOG_GAIN ("camera/control/manual/gain")

/// \brief Analog color-gain factor.
/// \ingroup prop
/// \remarks Value-type is double.
/// Analog gain factor applied to the color measurement.
#define MV6D_PROPERTY_CAMERA_CONTROL_ANALOG_GAIN_COLOR ("camera/control/manual/gain_color")

/// \brief Temperature of the camera fpga
/// \ingroup prop
/// \remarks Value-type is double.
#define MV6D_PROPERTY_CAMERA_FPGA_TEMPERATURE ("camera/fpga/temperature")

/// \brief Current of the laser in A
/// \ingroup prop
/// \remarks Value-type is double.
#define MV6D_PROPERTY_LASER_CURRENT ("camera_control/laser/current")

/// \brief Used stereo algorithm.
/// \ingroup depth
/// \remarks Value-type is int. Valid range is 0-2.
#define MV6D_PROPERTY_STEREO_ALGORITHM ("stereo/reference/stereo_algorithm")

/// \brief Enable calculation of the point cloud
/// \ingroup depth
/// \remarks Value-type is int.
#define MV6D_PROPERTY_STEREO_POINTCLOUD ("stereo/enablePointCloud")

/// \brief Set minimal distance of stereo calculation in meter
/// \ingroup depth
/// \remarks Value type is double.
/// \remarks Supported value lies between 0.8 and 2.5m
#define MV6D_PROPERTY_STEREO_MIN_DISTANCE ("stereo/minDistance")

/// \brief Width of the camera image region of interest
/// \ingroup depth
/// \remarks Value-type is int.
#define MV6D_PROPERTY_ROI_WIDTH ("acquisition/RoI/width")

/// \brief Height of the camera image region of interest
/// \ingroup depth
/// \remarks Value-type is int.
#define MV6D_PROPERTY_ROI_HEIGHT ("acquisition/RoI/height")

/// \brief Horizontal offset for the camera image region of interest
/// \ingroup depth
/// \remarks Value-type is int. READ ONLY!
#define MV6D_PROPERTY_ROI_OFFSET_U ("acquisition/RoI/offsetU")

/// \brief Vertical offset for the camera image region of interest
/// \ingroup depth
/// \remarks Value-type is int. READ ONLY!
#define MV6D_PROPERTY_ROI_OFFSET_V ("acquisition/RoI/offsetV")

/// \brief Horizontal software scaling for the warped image region of interest
/// \ingroup depth
/// \remarks Value-type is double.
#define MV6D_PROPERTY_ROI_SCALE_U ("preprocessing/warp/frameScale/scaleU")

/// \brief Vertical software scaling for the warped image region of interest
/// \ingroup depth
/// \remarks Value-type is double.
#define MV6D_PROPERTY_ROI_SCALE_V ("preprocessing/warp/frameScale/scaleV")

/// \brief Enables the marker detection and auto alignment module.
/// \ingroup depth
/// \remarks Value-type is double.
#define MV6D_ENABLE_MARKER_ALIGNMENT ("AutoAlignment/enable")

#endif // MV6D_DATASTRUCTURES_H