# Version 16.1.2

## Hikvision

* [bugfix] Avoid potential access violation if resolution of YUV image changes.
* [performance] Avoid busy waiting in update and switch to event based approach.



# Version 16.1.1

## General

* Update to latest `Metrilus.Util` release `16.1.1`
* Change local Nuget output directory to `Z:\releases\nuget{-unstable}`

## Hikvision

* Build non-public Nuget package including all managed and native dependencies
* Implement `GetIntrinsics`

## ifm

* [performance] Reduce delays in parameter getters and setters
* [bugfix] ifm used diffferent string for non-ambiguity mode `LessThan5`



# Version 16.1.0

## Hikvision

* Make binary strongnamed



# Version 16.0.1

## Hikvision

* Add support for Hikvision cameras

## Orbbec

* [bugfix] Throw exception on timeout and make it configurable.

## ifm

* [bugfix] Don't delete existing MetriCam app on the O3D3XX on connect & reuse it if possible
* [bugfix] Fix MetriCam2 camera GUI for ifm



# Version 16.0.0

## General

* Build public NuGet packages for `ifm`, `Sick.VisionaryT` and `WebCam`
* Build non-public NuGet packages for `BaslerACE`, `BaslerToF` and `OrbbecOpenNI`
* Add pre-release string to version numbers of local builds

## Azure Kinect

* [breaking] Rename type `Kinect4Azure` to `AzureKinect`
* [bugfix] Copy native dependencies when using the NuGet package



# Version 15.1.1

## General

* Build public NuGet packages for MetriCam2 and both Kinect cameras

## Azure Kinect

* Stability improvements



# Version 15.1.0

## General

* Update to latest `Metrilus.Util` release (first official Nuget package)
* Modernize C# project files by switching to new csproj-format
* Compile for multiple target frameworks: `.NET 4.7.2`, `.NET 4.5` and `.NET Standard 2.0` (not applicable to all projects)
* Delete all x86 build configurations
* Remove separate strong-name build config (build signed by default where possible)



# Version 15.0.0

## General

Update `Metrilus.Util` reference including a lot of new functionality:
* [breaking] Rename all image classes (remove string `Camera`)
* [breaking] Rename class `CameraImage` to `ImageBase`
* [breaking] Rename `ProjectiveTransformationZhang` to `ProjectiveTransformationRational` (has three additional, radial distortion parameters)
* Include more basic functionality for all image classes, e.g. flipping, rotation etc.

## Kinect4Azure

* Add support for `Kinect4Azure` based on .NET wrapper included in https://github.com/microsoft/Azure-Kinect-Sensor-SDK

## Orbbec

* Add color channel support for `Embedded S` and `Stereo S`
* [bugfix] Depth stream was not started, if both `ZImage` and `Point3DImage` were activated before `Connect`.
* [bugfix] Depth stream was stopped in `DeactivateChannel`, even though it could be used by another channel.

## Webcam

* Add property `MirrorImage`



# Version 14.2.0

## Orbbec

* Fix a camera freeze when using two cameras and fetching `ZImage` and `Point3DImage`
* Fix memory leaks
* Reduce Update timeout to 500ms
* Use VC++ 2017



# Version 14.1.0

## Sick

* Add SICK Visionary-T Pro
* SICK Visionary-T: Don't apply z-offset to 3-D coordinates

## Orbbec

* Add 60fps support and fix IR gain getter/setter for 2nd gen devices (Embedded S and Stereo S)
* Move OpenNI version into a deployed `.props` file



# Version 14.0.1

## General

* Update Metrilus.Util reference to version 14.0.1. 

## Sick Visionary-T

* Don't stop streaming on disconnect
* [bugfix] Allow reconnect
* Add strong name build


# Version 14.0.0

## General

* [breaking] Update to `Metrilus.Util` v.14.0 (uses managed arrays instead of unmanaged memory in image classes)
* [breaking] Remove `NetStandard` suffix in DLL name for `netstandard2.0` build
* [minor] Update log4net reference to .NET 4.5 version of 2.0.8



# Version 13.0.0

## General

* [breaking] Upgrade TargetFrameworkVersion to 4.5
* Add StrongNamed builds for selected cameras



# Version 12.2.0

## Orbbec Astra

* [breaking] Remove proximity sensor support (does not seem to be supported by new OpenNI version)
* Add support for new Orbbec prototypes "Astra Stereo S" and "Astra Embedded S" (Channel "Color" not yet supported)
* Updated to latest OpenNI2 v2.3.1.48
* [bugfix] `ActivateChannel` can now be called before `Connect`
* [bugfix] Set emitter status again in `ActivateChannel`, since activating channels can change the internal state of the Orbbec emitter.
* Improve robustness and performance of `GetIntrinsics` and `GetExtrinsics` by caching the transformations.
* Log only a warning instead of an error if the IR flooder status cannot be set
* Add `DeviceType` property
* Fill `Model` property
* Limit `IRGain` to a maximum of 63

## Pico Zense

* Add support for Pico Zense DCAM 710 (could also work for DCAM 100, but not tested)



# Version 12.1.0

## General

* Frame timestamps are now consistently written in UTC ticks by the Camera base class. Any values set by the camera implementations will be overridden.



# Version 12.0.0

## Orbbec Astra

* [breaking] Drop support of blue-PCB Orbbec Astra cameras (manufacturing year 2016)
* Compatibility with current (2018 green PCB) Astra cameras
* Read `SerialNumber` during connect
* Add `Vendor` property
* Increase robustness
* Faster connect
* Remove limit of max. 20 connected devices

## SICK Visionary-T

* Fix `IntegrationTime` property: can now be any int value, specifying [us].

## General (developers)

* Throwing exceptions improved



# Version 11.8.0

## SICK Visionary-T

* Fix NRE and compile errors
* Update dependency libraries

## Minor Changes

* Update dependency libraries



# Version 11.7.0

## General

* Reference local Newtonsoft.Json binary to keep versions across Metrilus projects in sync



# Version 11.6.0

## SICK Visionary-T

* Fix out of sync frames when using multiple cameras



# Version 11.5.0

## SICK Visionary-T

* [feature] Add retry mode for corrupted frames
* [tweak] Better error handling and output



# Version 11.4.0

## ifm O3D3xx

* [bugfix] Disable timeout exception if camera is triggered

## SICK Visionary-T

* [feature] Read S/N during connect



# Version 11.3.0

## General

* Support loading camera assemblies from working directory.

## ifm O3D3xx

* Support TriggerMode.
* Improved networking.
* Faster connection.

## RealSense2

* Support for more filters.
* Some refactoring.
* DepthResolution now reflects actual resolution of filtered data.
* Additional IRResolution property.
* Use new RealSense SDK version.

## SICK Visionary-T

* Support for coexistence mode.
* Fix integration time.
* Removed wrong implementation of GetIntrinsics.

## Kinect2

* Improved thread-safety / mutual exclusion mechanisms.



# Version 11.2.0

## ifm O3D3xx

* Improved time-out handling.

## RealSense2

* Implement cache for extrinsics and intrinsics



# Version 11.1.0

## New Cameras

* Add Basler acA1300
* Add Matrix Vision mvBlueSirius camera

## Features

* [breaking] RealSense2: Use official .NET wrapper, improve robustness  
  Removes dependency on native realsense2.dll, adds managed reference Intel.RealSense.dll (auto-copied)
* Rework layout of camera configuration dialog
* BaslerToF: Add OutlierTolerance, TemporalFilterStrength, DeviceChannel
* BaslerToF: Improve multicam init
* [breaking] BaslerToF: Rename spatial & temporal filter properties
* ifm O3D3xx: Add 100k mode
* ifm O3D3xx: Improvements to performance and robustness

## Minor Changes

* Deploy TI Voxel and dependencies
* Deploy .pdb files to release (the build config) folders
* Fix icons of C++/CLI projects for Release/x64
* Correct license.txt (log4net is now a reference)
* Update Metrilus.Util to 13.1



# Version 11.0.0

## New Cameras

* Realsense D4XX
* Basler ToF
* SICK TiM561
* Xtion2

## Features

* Added icons for most camera implementations
* Partial .NET Standard 2.0 support
* Improved OrbbecOpenNI implementation
* SVS: logging verbosity can be adjusted

## Bugfixes

* Error reporting for Kinect2

## Other

* [breaking] Removed support for deprecated Orbbec SDK
* Updated referenced libraries
