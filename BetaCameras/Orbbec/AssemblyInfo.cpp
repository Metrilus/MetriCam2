// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

#include "stdafx.h"
#include "../../SolutionAssemblyInfo.h"

using namespace System;
using namespace System::Reflection;
using namespace System::Runtime::CompilerServices;
using namespace System::Runtime::InteropServices;
using namespace System::Security::Permissions;

//
// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly:AssemblyTitleAttribute("MetriCam 2: Orbbec wrapper")];
[assembly:AssemblyDescriptionAttribute("MetriCam 2 wrapper for Orbbec cameras")];
[assembly:MetriCam2::Attributes::ContainsCameraImplementations]

[assembly:ComVisible(false)];

[assembly:CLSCompliantAttribute(true)];
