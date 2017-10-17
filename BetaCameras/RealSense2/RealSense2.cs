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
        private RealSense2API.RS2Frame _currentLeftFrame = new RealSense2API.RS2Frame();
        private RealSense2API.RS2Frame _currentRightFrame = new RealSense2API.RS2Frame();
        private float _depthScale = 0.0f;


        public class CustomChannelNames
        {
            public const string Left = "LeftInfrared";
            public const string Right = "RightInfrared";
        }

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

            Channels.Add(cr.RegisterCustomChannel(CustomChannelNames.Left, typeof(FloatCameraImage)));
            Channels.Add(cr.RegisterCustomChannel(CustomChannelNames.Right, typeof(FloatCameraImage)));
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
            RealSense2API.ReleaseFrame(_currentLeftFrame);
            RealSense2API.ReleaseFrame(_currentRightFrame);

            bool getColor = IsChannelActive(ChannelNames.Color);
            bool getDepth = IsChannelActive(ChannelNames.ZImage);
            bool getLeft = IsChannelActive(CustomChannelNames.Left);
            bool getRight = IsChannelActive(CustomChannelNames.Right);
            bool haveColor = false;
            bool haveDepth = false;
            bool haveLeft = false;
            bool haveRight = false;

            while (true)
            {
                RealSense2API.RS2Frame data = RealSense2API.PipelineWaitForFrames(_pipeline, 500);

                if(!data.IsValid())
                {
                    RealSense2API.ReleaseFrame(data);
                    continue;
                }

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
                        case RealSense2API.Stream.INFRARED:
                            if(index == 1)
                            {
                                if (getLeft)
                                {
                                    RealSense2API.ReleaseFrame(_currentLeftFrame);
                                    _currentLeftFrame = frame;
                                    haveLeft = true;
                                }
                            }
                            else if(index == 2)
                            {
                                if (getRight)
                                {
                                    RealSense2API.ReleaseFrame(_currentRightFrame);
                                    _currentRightFrame = frame;
                                    haveRight = true;
                                }
                            }
                            break;
                    }
                }

                RealSense2API.ReleaseFrame(data);

                if (((getColor && haveColor) || !getColor)
                && ((getDepth && haveDepth) || !getDepth)
                && ((getLeft && haveLeft) || !getLeft)
                && ((getRight && haveRight) || !getRight))
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
            else if (channelName == CustomChannelNames.Left)
            {
                return CalcIRImage(_currentLeftFrame);
            }
            else if (channelName == CustomChannelNames.Right)
            {
                return CalcIRImage(_currentRightFrame);
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
            int index = -1;

            if (channelName == ChannelNames.Color)
            {
                stream = RealSense2API.Stream.COLOR;
                format = RealSense2API.Format.RGB8;

                res_x = ColorResolution.X;
                res_y = ColorResolution.Y;
                fps = (int)ColorFPS;
                index = -1;
            }
            else if (channelName == ChannelNames.ZImage)
            {
                if (IsChannelActive(CustomChannelNames.Left) || IsChannelActive(CustomChannelNames.Right))
                {
                    string msg = string.Format("RealSense2: can't have Left/Right and ZImage active at the same time");
                    log.Error(msg);
                    throw new Exception(msg);
                }

                stream = RealSense2API.Stream.DEPTH;
                format = RealSense2API.Format.Z16;

                res_x = DepthResolution.X;
                res_y = DepthResolution.Y;
                fps = (int)DepthFPS;
                index = -1;
            }
            else if (channelName == CustomChannelNames.Left)
            {
                if(IsChannelActive(ChannelNames.ZImage))
                {
                    string msg = string.Format("RealSense2: can't have ZImage and Left active at the same time");
                    log.Error(msg);
                    throw new Exception(msg);
                }

                stream = RealSense2API.Stream.INFRARED;
                format = RealSense2API.Format.Y8;

                res_x = DepthResolution.X;
                res_y = DepthResolution.Y;
                fps = (int)DepthFPS;
                index = 1;
            }
            else if (channelName == CustomChannelNames.Right)
            {
                if (IsChannelActive(ChannelNames.ZImage))
                {
                    string msg = string.Format("RealSense2: can't have ZImage and right active at the same time");
                    log.Error(msg);
                    throw new Exception(msg);
                }

                stream = RealSense2API.Stream.INFRARED;
                format = RealSense2API.Format.Y8;

                res_x = DepthResolution.X;
                res_y = DepthResolution.Y;
                fps = (int)DepthFPS;
                index = 2;
            }
            else
            {
                string msg = string.Format("RealSense2: Channel not supported {0}", channelName);
                log.Error(msg);
                throw new Exception(msg);
            }

            RealSense2API.PipelineStop(_pipeline);
            RealSense2API.ConfigEnableStream(_config, stream, index, res_x, res_y, format, fps);
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
            else if (channelName == CustomChannelNames.Left)
            {
                stream = RealSense2API.Stream.INFRARED;
            }
            else if (channelName == CustomChannelNames.Right)
            {
                stream = RealSense2API.Stream.INFRARED;
            }

            RealSense2API.PipelineStop(_pipeline);
            RealSense2API.ConfigDisableStream(_config, stream);
            RealSense2API.PipelineStart(_pipeline, _config);
        }

        unsafe public override IProjectiveTransformation GetIntrinsics(string channelName)
        {
            RealSense2API.RS2StreamProfile profile = GetProfileForChannelName(channelName);
            RealSense2API.Intrinsics intrinsics = RealSense2API.GetIntrinsics(profile);

            if(intrinsics.model != RealSense2API.DistortionModel.BROWN_CONRADY)
            {
                string msg = string.Format("RealSense2: intrinsics distrotion model {0} does not match metrilus framework", intrinsics.model.ToString());
                log.Error(msg);
                throw new Exception(msg);
            }

            return new ProjectiveTransformationZhang(
                intrinsics.width,
                intrinsics.height,
                intrinsics.fx,
                intrinsics.fy,
                intrinsics.ppx,
                intrinsics.ppy,
                intrinsics.coeffs[0],
                intrinsics.coeffs[1],
                intrinsics.coeffs[2],
                intrinsics.coeffs[3],
                intrinsics.coeffs[4]);
        }

        private RealSense2API.RS2StreamProfile GetProfileForChannelName(string channelName)
        {
            RealSense2API.RS2StreamProfile profile = new RealSense2API.RS2StreamProfile();

            switch (channelName)
            {
                case ChannelNames.Color:
                    if (!_currentColorFrame.IsValid())
                    {
                        ActivateChannel(ChannelNames.Color);
                        UpdateImpl();
                    }
                    profile = RealSense2API.GetStreamProfile(_currentColorFrame);
                    break;

                case ChannelNames.ZImage:
                    if (!_currentDepthFrame.IsValid())
                    {
                        ActivateChannel(ChannelNames.ZImage);
                        UpdateImpl();
                    }
                    profile = RealSense2API.GetStreamProfile(_currentDepthFrame);
                    break;

                case CustomChannelNames.Left:
                    if (!_currentLeftFrame.IsValid())
                    {
                        ActivateChannel(ChannelNames.Left);
                        UpdateImpl();
                    }
                    profile = RealSense2API.GetStreamProfile(_currentLeftFrame);
                    break;

                case CustomChannelNames.Right:
                    if (!_currentRightFrame.IsValid())
                    {
                        ActivateChannel(ChannelNames.Right);
                        UpdateImpl();
                    }
                    profile = RealSense2API.GetStreamProfile(_currentRightFrame);
                    break;

                default:
                    string msg = string.Format("RealSense2: intrinsics for channel {0} not available", channelName);
                    log.Error(msg);
                    throw new Exception(msg);
            }

            return profile;
        }

        unsafe public override RigidBodyTransformation GetExtrinsics(string channelFromName, string channelToName)
        {
            RealSense2API.RS2StreamProfile from = GetProfileForChannelName(channelFromName);
            RealSense2API.RS2StreamProfile to = GetProfileForChannelName(channelToName);

            RealSense2API.Extrinsics extrinsics = RealSense2API.GetExtrinsics(from, to);


            Point3f col1 = new Point3f(extrinsics.rotation[0], extrinsics.rotation[1], extrinsics.rotation[2]);
            Point3f col2 = new Point3f(extrinsics.rotation[3], extrinsics.rotation[4], extrinsics.rotation[5]);
            Point3f col3 = new Point3f(extrinsics.rotation[6], extrinsics.rotation[7], extrinsics.rotation[8]);
            RotationMatrix rot = new RotationMatrix(col1, col2, col3);

            Point3f trans = new Point3f(extrinsics.translation[0], extrinsics.translation[1], extrinsics.translation[2]);

            return new RigidBodyTransformation(rot, trans);
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

        unsafe private FloatCameraImage CalcIRImage(RealSense2API.RS2Frame frame)
        {
            if (!frame.IsValid())
            {
                log.Error("IR frame is not valid...\n");
                return null;
            }

            int height = DepthResolution.Y;
            int width = DepthResolution.X;

            FloatCameraImage depthDataMeters = new FloatCameraImage(width, height);
            byte* source = (byte*)RealSense2API.GetFrameData(frame);

            for (int y = 0; y < height; y++)
            {
                byte* sourceLine = source + y * width;
                for (int x = 0; x < width; x++)
                {
                    depthDataMeters[y, x] = (float)((byte)*sourceLine++);
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
