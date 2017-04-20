# MetriCam2
A consistent .NET SDK for Depth Cameras

## Prerequisites
### Documentation
The SDK documentation sources are in the sub-folder _doc_. Please use doxygen to build the documentation with the _doxyfile_ in this folder.

### Cameras
To actually use the cameras, you need to connect the camera to your PC or network. For most cameras you also need to install the respective camera drivers which can be obtained from the camera vendor. Some cameras also require the appropriate SDK-DLL to be placed in the execution directory of your application.

### Development 
You need Visual Studio 2012 develop and build the sources. 

## Structure
# BetaCameras
This folder contains the individual camera SDK implementations. These are usually Visual Studio Projects that compile a .NET DLL

# MetriCam2
The sources for the core component _MetriCam2.dll_ for all camera implementations. Refer to CameraTemplate (in _BetaCameras_) if you want to implement a new MetriCam2-camera based on this library.

# Samples
Sample applications that show how to use MetriCam-implementations.

# Test Programs / Tests
Test applications, unit tests and test related code.

# doc
The doxygen sources for the developer documentation.

# libraries
Contains the pre-compiled _Metrilus.Util.dll_ which holds all the relevant data types to handle camera data.
