// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
//using RealSense2API;


namespace MetriCam2.Cameras
{
    public class RealSense2 : Camera
    {
        private RealSense2API.RS2Context _context;
        private RealSense2API.RS2Pipeline _pipeline;
        private RealSense2API.RS2Config _config;
        private RealSense2API.RS2Frame _currentColorFrame = new RealSense2API.RS2Frame();
        private RealSense2API.RS2Frame _currentDepthFrame = new RealSense2API.RS2Frame();
        private float _depthScale = 0.0f;

        Point2i _colorResolution = new Point2i(640, 480);
        public Point2i ColorResolution
        {
            get => _colorResolution;
            set => _colorResolution = value;
        }

        ParamDesc<Point2i> ColorResolutionDesc
        {
            get
            {
                ParamDesc<Point2i> res = new ParamDesc<Point2i>();
                res.Unit = "Pixel";
                res.Description = "Resolution of the color sensor.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }

        private uint _colorFPS = 30;
        public uint ColorFPS
        {
            get => _colorFPS;
            set => _colorFPS = value;
        }

        ParamDesc<Point2i> ColorFPSDesc
        {
            get
            {
                ParamDesc<Point2i> res = new ParamDesc<Point2i>();
                res.Unit = "Frames per Second";
                res.Description = "FPS of the color sensor.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }

        Point2i _depthResolution = new Point2i(640, 480);
        public Point2i DepthResolution
        {
            get => _depthResolution;
            set => _depthResolution = value;
        }

        ParamDesc<Point2i> DepthResolutionDesc
        {
            get
            {
                ParamDesc<Point2i> res = new ParamDesc<Point2i>();
                res.Unit = "Pixel";
                res.Description = "Resolution of the depth sensor.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }

        private uint _depthFPS = 30;
        public uint DepthFPS
        {
            get => _depthFPS;
            set => _depthFPS = value;
        }

        ParamDesc<Point2i> DepthFPSDesc
        {
            get
            {
                ParamDesc<Point2i> res = new ParamDesc<Point2i>();
                res.Unit = "Frames per Second";
                res.Description = "FPS of the depth sensor.";
                res.ReadableWhen = ParamDesc.ConnectionStates.Connected | ParamDesc.ConnectionStates.Disconnected;
                res.WritableWhen = ParamDesc.ConnectionStates.Disconnected;
                return res;
            }
        }

        #region Constructor
        public RealSense2() : base()
        {
            _context = RealSense2API.CreateContext();
            _pipeline = RealSense2API.CreatePipeline(_context);
            _config = RealSense2API.CreateConfig();

            RealSense2API.DisableAllStreams(_config);
        }

        ~RealSense2()
        {
            RealSense2API.DeleteConfig(_config);
            RealSense2API.DeletePipeline(_pipeline);
            RealSense2API.DeleteContext(_context);
        }
        #endregion

        #region MetriCam2 Camera Interface

        #region MetriCam2 Camera Interface Methods

        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;
            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.ZImage));
            Channels.Add(cr.RegisterChannel(ChannelNames.Color));
        }


        protected override void ConnectImpl()
        {
            if(SerialNumber != null && SerialNumber.Length != 0)
            {
                RealSense2API.EnableDevice(_config, SerialNumber);
            }

            RealSense2API.PipelineStart(_pipeline, _config);

            ActivateChannel(ChannelNames.Color);
            ActivateChannel(ChannelNames.ZImage);

            _depthScale = RealSense2API.GetDepthScale(_pipeline);
        }

        protected override void DisconnectImpl()
        {
            RealSense2API.PipelineStop(_pipeline);
        }

        protected override void UpdateImpl()
        {
            RealSense2API.ReleaseFrame(_currentColorFrame);
            RealSense2API.ReleaseFrame(_currentDepthFrame);

            bool getColor = IsChannelActive(ChannelNames.Color);
            bool getDepth = IsChannelActive(ChannelNames.ZImage);
            bool haveColor = false;
            bool haveDepth = false;

            while (true)
            {
                RealSense2API.RS2Frame data = RealSense2API.PipelineWaitForFrames(_pipeline, 500);

                int frameCount = RealSense2API.FrameSetEmbeddedCount(data);
                log.Debug(string.Format("RealSense2: Got {0} Frames", frameCount));


                // extract all frames
                for (int j = 0; j < frameCount; j++)
                {
                    RealSense2API.RS2Frame frame = RealSense2API.FrameSetExtract(data, j);
                    RealSense2API.FrameAddRef(frame);

                    // what kind of frame did we get?
                    RealSense2API.RS2StreamProfile profile = RealSense2API.GetStreamProfile(frame);
                    RealSense2API.GetStreamProfileData(profile, out RealSense2API.Stream stream, out RealSense2API.Format format, out int index, out int uid, out int framerate);

                    log.Debug(string.Format("RealSense2: Analyzing frame {0}", j + 1));
                    log.Debug(string.Format("RealSense2: stream {0}", stream.ToString()));
                    log.Debug(string.Format("RealSense2: format {0}", format.ToString()));


                    switch (stream)
                    {
                        case RealSense2API.Stream.COLOR:
                            if (getColor)
                            {
                                RealSense2API.ReleaseFrame(_currentColorFrame);
                                _currentColorFrame = frame;
                                haveColor = true;
                            }
                            break;
                        case RealSense2API.Stream.DEPTH:
                            if (getDepth)
                            {
                                RealSense2API.ReleaseFrame(_currentDepthFrame);
                                _currentDepthFrame = frame;
                                haveDepth = true;
                            }
                            break;
                    }
                }

                RealSense2API.ReleaseFrame(data);

                if (((getColor && haveColor) || !getColor) && ((getDepth && haveDepth) || !getDepth))
                    break;
            }
        }

        protected override CameraImage CalcChannelImpl(string channelName)
        {
            if (channelName == ChannelNames.Color)
            {
                return CalcColor();
            }
            else if (channelName == ChannelNames.ZImage)
            {
                return CalcZImage();
            }

            return new Metrilus.Util.FloatCameraImage();
        }

        protected override void ActivateChannelImpl(String channelName)
        {
            RealSense2API.Stream stream = RealSense2API.Stream.ANY;
            RealSense2API.Format format = RealSense2API.Format.ANY;
            int res_x = 640;
            int res_y = 480;
            int fps = 30;

            if (channelName == ChannelNames.Color)
            {
                stream = RealSense2API.Stream.COLOR;
                format = RealSense2API.Format.RGB8;

                res_x = ColorResolution.X;
                res_y = ColorResolution.Y;
                fps = (int)ColorFPS;
            }
            else if (channelName == ChannelNames.ZImage)
            {
                stream = RealSense2API.Stream.DEPTH;
                format = RealSense2API.Format.Z16;

                res_x = DepthResolution.X;
                res_y = DepthResolution.Y;
                fps = (int)DepthFPS;
            }

            RealSense2API.PipelineStop(_pipeline);
            RealSense2API.ConfigEnableStream(_config, stream, 0, res_x, res_y, format, fps);
            RealSense2API.PipelineStart(_pipeline, _config);
        }

        protected override void DeactivateChannelImpl(String channelName)
        {
            RealSense2API.Stream stream = RealSense2API.Stream.ANY;

            if (channelName == ChannelNames.Color)
            {
                stream = RealSense2API.Stream.COLOR;
            }
            else if (channelName == ChannelNames.ZImage)
            {
                stream = RealSense2API.Stream.DEPTH;
            }

            RealSense2API.PipelineStop(_pipeline);
            RealSense2API.ConfigDisableStream(_config, stream);
            RealSense2API.PipelineStart(_pipeline, _config);
        }

        unsafe private FloatCameraImage CalcZImage()
        {
            if (!_currentDepthFrame.IsValid())
            {
                log.Error("Depth frame is not valid...\n");
                return null;
            }

            int height = DepthResolution.Y;
            int width = DepthResolution.X;

            FloatCameraImage depthDataMeters = new FloatCameraImage(width, height);
            short* source = (short*)RealSense2API.GetFrameData(_currentDepthFrame);

            for (int y = 0; y < height; y++)
            {
                short* sourceLine = source + y * width;
                for (int x = 0; x < width; x++)
                {
                    depthDataMeters[y, x] = (float)(_depthScale * (short)*sourceLine++);
                }
            }

            return depthDataMeters;
        }

        unsafe private ColorCameraImage CalcColor()
        {
            if (!_currentColorFrame.IsValid())
            {
                log.Error("Color frame is not valid...\n");
                return null;
            }

            int height = ColorResolution.Y;
            int width = ColorResolution.X;

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            Rectangle imageRect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = bitmap.LockBits(imageRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            byte* source = (byte*) RealSense2API.GetFrameData(_currentColorFrame);
            byte* target = (byte*) (void*)bmpData.Scan0;
            for (int y = 0; y < height; y++)
            {
                byte* sourceLine = source + y * width * 3;
                for (int x = 0; x < width; x++)
                {
                    target[2] = *sourceLine++;
                    target[1] = *sourceLine++;
                    target[0] = *sourceLine++;
                    target += 3;
                }
            }

            bitmap.UnlockBits(bmpData);
            ColorCameraImage image = new ColorCameraImage(bitmap);

            return image;
        }

        #endregion
        #endregion

        public void LoadConfigPreset(AdvancedMode.Preset preset)
        {
            LoadCustomConfig(AdvancedMode.GetPreset(preset));
        }
        
        public void LoadCustomConfig(string json)
        {
            RealSense2API.RS2Device dev = RealSense2API.GetActiveDevice(_pipeline);

            if (!RealSense2API.AdvancedModeEnabled(dev))
            {
                RealSense2API.EnabledAdvancedMode(dev, true);
            }

            RealSense2API.LoadAdvancedConfig(json, dev);
        }
    }
}
