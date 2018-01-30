// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

#pragma once
#include <astra_core\astra_core.hpp>
#include <astra\streams\Infrared.hpp>
#include <astra\astra.hpp>
#include "OrbbecWrapper.h"
#include <algorithm>
#include <iterator>
#include <set>

using namespace MetriCam2;
using namespace MetriCam2::Exceptions;
using namespace MetriCam2::Enums;
using namespace MetriCam2::Attributes;
using namespace Metrilus::Util;
using namespace System;

namespace MetriCam2 {
	namespace Cameras
	{
		public ref class Astra : Camera
		{
		public:
			/// <summary>
			/// Defines the custom channel names for easier handling.
			/// </summary>
			/// <remarks>Similar to MetriCam2.ChannelNames for standard channel names.</remarks>
			static ref class CustomChannelNames
			{
			public:
				static const String^ Infrared = "Infrared";
			};

			Astra();
			~Astra();

			virtual Metrilus::Util::IProjectiveTransformation^ GetIntrinsics(String^ channelName) override;

			[Description("Depth QVGA", "Depth QVGA mode")]
			[AccessState(
				ConnectionStates::Connected | ConnectionStates::Disconnected,
				ConnectionStates::Connected)]
			property bool DepthQVGA
			{
				bool get(void)
				{ 
					return depthQvga;
				}
				void set(bool value) 
				{
					if(!IsConnected)
					{
						depthQvga = value;
					}
				}
			}

#if !NETSTANDARD2_0
			property System::Drawing::Icon^ CameraIcon
			{
				System::Drawing::Icon^ get() override
				{
					System::Reflection::Assembly^ assembly = System::Reflection::Assembly::GetExecutingAssembly();
					System::IO::Stream^ iconStream = assembly->GetManifestResourceStream("OrbbecIcon.ico");
					return gcnew System::Drawing::Icon(iconStream);
				}
			}
#endif

		protected:
			/// <summary>
			/// Resets list of available channels (<see cref="Channels"/>) to union of all cameras supported by the implementing class.
			/// </summary>
			virtual void LoadAllAvailableChannels() override;

			/// <summary>
			/// Connects the camera.
			/// </summary>
			/// <remarks>This method is implicitely called by <see cref="Camera.Connect"/> inside a camera lock.</remarks>
			/// <seealso cref="Camera.Connect"/>
			virtual void ConnectImpl() override;

			/// <summary>
			/// Disconnects the camera.
			/// </summary>
			/// <remarks>This method is implicitely called by <see cref="Camera.Disconnect"/> inside a camera lock.</remarks>
			/// <seealso cref="Camera.Disconnect"/>
			virtual void DisconnectImpl() override;

			/// <summary>
			/// Updates data buffers of all active channels with data of current frame.
			/// </summary>
			/// <remarks>This method is implicitely called by <see cref="Camera.Update"/> inside a camera lock.</remarks>
			/// <seealso cref="Camera.Update"/>
			virtual void UpdateImpl() override;

			/// <summary>Computes (image) data for a given channel.</summary>
			/// <param name="channelName">Channel name.</param>
			/// <returns>(Image) Data.</returns>
			/// <seealso cref="Camera.CalcChannel"/>
			virtual Metrilus::Util::CameraImage^ CalcChannelImpl(String^ channelName) override;
		private:
			OrbbecWrapper* wrapper;

			UShortCameraImage^ CalcInfrared();
			FloatCameraImage^ CalcZImage();
			ColorCameraImage^ CalcColor();
			Point3fCameraImage^ CalcPoint3fImage();

			bool depthQvga;
		};

	}
}