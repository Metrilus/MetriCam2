// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

#include "stdafx.h"
#include "OrbbecWrapper.h"
#include <cmath>

MetriCam2::Cameras::OrbbecWrapper::OrbbecWrapper()
{
	astra::initialize();

	m_sensor = unique_ptr<astra::StreamSet>(new astra::StreamSet(astra::StreamSet("device/default")));
	if (m_sensor->is_valid())
	{
		m_reader = unique_ptr<astra::StreamReader>(new astra::StreamReader(m_sensor->create_reader()));
	}
	Width = DepthWidth = 640;
	Height = DepthHeight = 480;
}

MetriCam2::Cameras::OrbbecWrapper::~OrbbecWrapper()
{
	astra::terminate();
}

void MetriCam2::Cameras::OrbbecWrapper::StartPointStream()
{
	astra::PointStream pointStream = m_reader->stream<astra::PointStream>();
	pointStream.start();

	hasPointStreamEnabled = true;
}

void MetriCam2::Cameras::OrbbecWrapper::StartInfraredStream()
{
	astra::InfraredStream irStream = m_reader->stream<astra::InfraredStream>();
	astra::ImageStreamMode irMode;
	
	irMode.set_width(Width);
	irMode.set_height(Height);
	irMode.set_pixel_format(astra_pixel_formats::ASTRA_PIXEL_FORMAT_GRAY16);
	irMode.set_fps(30);

	irStream.set_mode(irMode);
	irStream.enable_mirroring(false);
	irStream.start();

	hasInfraredEnabled = true;
}

void MetriCam2::Cameras::OrbbecWrapper::StartDepthStream()
{
	astra::DepthStream depthStream = m_reader->stream<astra::DepthStream>();
	astra::ImageStreamMode depthMode;

	depthMode.set_width(DepthWidth);
	depthMode.set_height(DepthHeight);
	depthMode.set_pixel_format(astra_pixel_formats::ASTRA_PIXEL_FORMAT_DEPTH_MM);
	depthMode.set_fps(30);

	depthStream.set_mode(depthMode);
	depthStream.enable_mirroring(false);
	depthStream.start();
	float hfov = depthStream.hFov();
	float vfov = depthStream.vFov();
	FocalLengthX = Width / (float)(2 * tan(hfov / 2.0));
	FocalLengthY = Height / (float)(2 * tan(vfov / 2.0));
	hasDepthEnabled = true;
}

void MetriCam2::Cameras::OrbbecWrapper::StartColorStream()
{
	astra::ColorStream colorStream = m_reader->stream<astra::ColorStream>();
	astra::ImageStreamMode colorMode;

	colorMode.set_width(Width);
	colorMode.set_height(Height);
	colorMode.set_pixel_format(astra_pixel_formats::ASTRA_PIXEL_FORMAT_RGB888);
	colorMode.set_fps(30);

	colorStream.set_mode(colorMode);
	colorStream.enable_mirroring(false);
	colorStream.start();

	hasColorEnabled = true;
}

void MetriCam2::Cameras::OrbbecWrapper::StopInfraredStream()
{
	m_reader->stream<astra::InfraredStream>().stop();
	hasDepthEnabled = false;
}

void MetriCam2::Cameras::OrbbecWrapper::StopDepthStream()
{
	m_reader->stream<astra::DepthStream>().stop();
	hasDepthEnabled = false;
}

void MetriCam2::Cameras::OrbbecWrapper::StopPointStream()
{
	m_reader->stream<astra::PointStream>().stop();
	hasPointStreamEnabled = false;
}

void MetriCam2::Cameras::OrbbecWrapper::StopColorStream()
{
	m_reader->stream<astra::ColorStream>().stop();
	hasColorEnabled = false;
}

void MetriCam2::Cameras::OrbbecWrapper::Update()
{
	astra::Frame frame = m_reader->get_latest_frame();

	// not optimal to put following code in here, but the code did not work for some reason when moved into corresponding Get-Methods.

	if (hasInfraredEnabled)
	{
		irFrame = frame.get<astra::InfraredFrame16>();
	}
	if (hasDepthEnabled)
	{
		depthFrame = frame.get<astra::DepthFrame>();
	}
	if (hasColorEnabled)
	{
		colorFrame = frame.get<astra::ColorFrame>();
	}
	if (hasPointStreamEnabled)
	{
		pointFrame = frame.get<astra::PointFrame>();
	}
}

astra::InfraredFrame16 MetriCam2::Cameras::OrbbecWrapper::GetInfraredFrame()
{
	return irFrame;
}

astra::DepthFrame MetriCam2::Cameras::OrbbecWrapper::GetDepthFrame()
{
	return depthFrame;
}

astra::PointFrame MetriCam2::Cameras::OrbbecWrapper::GetPointFrame()
{
	return pointFrame;
}

astra::ColorFrame MetriCam2::Cameras::OrbbecWrapper::GetColorFrame()
{
	return colorFrame;
}