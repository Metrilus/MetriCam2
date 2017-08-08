// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

#pragma once
#include <astra_core\astra_core.hpp>
#include <astra\astra.hpp>
#include <algorithm>
#include <iterator>
#include <set>
#include <memory>

using namespace std;

namespace MetriCam2 {
	namespace Cameras
	{
		public class OrbbecWrapper
		{
		public:
			OrbbecWrapper();
			~OrbbecWrapper();
			void StartInfraredStream();
			void StartDepthStream();
			void StartColorStream();
			void StartPointStream();

			void StopInfraredStream();
			void StopDepthStream();
			void StopColorStream();
			void StopPointStream();

			void Update();

			astra::InfraredFrame16 GetInfraredFrame();
			astra::DepthFrame GetDepthFrame();
			astra::ColorFrame GetColorFrame();
			astra::PointFrame GetPointFrame();

			float FocalLengthX;
			float FocalLengthY;
			int Width;
			int Height;
			int DepthWidth;
			int DepthHeight;
		private:
			unique_ptr<astra::StreamSet> m_sensor;
			unique_ptr<astra::StreamReader> m_reader;

			astra::InfraredFrame16 irFrame = nullptr;
			astra::DepthFrame depthFrame = nullptr;
			astra::ColorFrame colorFrame = nullptr;
			astra::PointFrame pointFrame = nullptr;

			bool hasInfraredEnabled = false;
			bool hasDepthEnabled = false;
			bool hasColorEnabled = false;
			bool hasPointStreamEnabled = false;
		};
	}
}
