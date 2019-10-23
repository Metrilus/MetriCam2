# MetriCam2

MetriCam 2 is a consistent .NET SDK for depth cameras.
It makes it simple to exchange 3D cameras during development without changing the code of your application.


# Contributing

MetriCam2 accepts contributions. Please open a pull request on GitHub.


# Usage

MetriCam2 and several camera implementations are available [on nuget.org](https://www.nuget.org/profiles/Metrilus). Just add a package reference to the camera you want to use to your project:

```PowerShell
PM> Install-Package MetriCam2.Cameras.AzureKinect
```
or
```
dotnet add package MetriCam2.Cameras.AzureKinect
```

Note that some cameras need a driver and/or plain DLLs from the manufacturer to work.
Also, we don't build or publish nuget packages for all cameras (yet), so you might have to build MetriCam2 yourself.


# Documentation

The sub-folder _doc_ contains some documentation and a `Doxyfile` which can be used to build the documentation with `doxygen`.


# Development 

You need Visual Studio 2017 to build MetriCam2.
You also need the dependencies of the cameras you want to build (sorry, we don't have the permission to redistribute them) and you have to place them in the correct location, usually `Z:\external-libraries\`. You can mount a folder as drive `Z` using
```bat
subst Z: .
```
and remove that mapping using
```bat
subst Z: /d
```


## Folder Structure

### BetaCameras

This folder contains the individual camera wrappers.

### doc

Some documentation and a `Doxyfile` which can be used to build the documentation with `doxygen`.

### MetriCam2

The sources for the core component _MetriCam2.dll_ for all camera implementations. Refer to `BetaCameras\CameraTemplate` if you want to implement a new MetriCam2 camera wrapper.

### MetriCam2.Controls

Some WinForms controls for interacting with the cameras.

### Samples

Sample applications that show how to use MetriCam2.

### Scripts

Scripts used for building and deployment.

### Test Programs, Tests

Test applications, unit tests and test related code.
