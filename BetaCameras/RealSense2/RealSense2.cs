// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
//using RealSense2API;


namespace MetriCam2.Cameras
{
    public class RealSense2 : Camera, IDisposable
    {
        private RealSense2API.RS2Context _context;
        private RealSense2API.RS2Pipeline _pipeline;
        private RealSense2API.RS2Config _config;
        private RealSense2API.RS2Frame _currentColorFrame = new RealSense2API.RS2Frame();
        private RealSense2API.RS2Frame _currentDepthFrame = new RealSense2API.RS2Frame();
        private RealSense2API.RS2Frame _currentLeftFrame = new RealSense2API.RS2Frame();
        private RealSense2API.RS2Frame _currentRightFrame = new RealSense2API.RS2Frame();
        private float _depthScale = 0.0f;
        private bool _disposed = false;

        public Point2i ColorResolution { get; set; } = new Point2i(640, 480);

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

        public uint ColorFPS { get; set; } = 30;

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

        public Point2i DepthResolution { get; set; } = new Point2i(640, 480);

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

        public uint DepthFPS { get; set; } = 30;

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            RealSense2API.DeleteConfig(_config);
            RealSense2API.DeletePipeline(_pipeline);
            RealSense2API.DeleteContext(_context);
            _disposed = true;
        }

        #region Constructor
        public RealSense2()
        {
            _context = RealSense2API.CreateContext();
            _pipeline = RealSense2API.CreatePipeline(_context);
            _config = RealSense2API.CreateConfig();

            RealSense2API.DisableAllStreams(_config);
        }

        ~RealSense2()
        {
            Dispose(false);
        }
        #endregion

        #region MetriCam2 Camera Interface Methods

        protected override void LoadAllAvailableChannels()
        {
            ChannelRegistry cr = ChannelRegistry.Instance;
            Channels.Clear();

            Channels.Add(cr.RegisterChannel(ChannelNames.ZImage));
            Channels.Add(cr.RegisterChannel(ChannelNames.Color));

            Channels.Add(cr.RegisterCustomChannel(ChannelNames.Left, typeof(FloatCameraImage)));
            Channels.Add(cr.RegisterCustomChannel(ChannelNames.Right, typeof(FloatCameraImage)));
        }


        protected override void ConnectImpl()
        {
            if (!string.IsNullOrWhiteSpace(SerialNumber))
            {
                RealSense2API.EnableDevice(_config, SerialNumber);
            }

            RealSense2API.PipelineStart(_pipeline, _config);

            ActivateChannel(ChannelNames.Color);
            ActivateChannel(ChannelNames.ZImage);

            RealSense2API.RS2Device dev = RealSense2API.GetActiveDevice(_pipeline);

            if (!RealSense2API.AdvancedModeEnabled(dev))
            {
                RealSense2API.EnabledAdvancedMode(dev, true);
            }

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
            bool getLeft = IsChannelActive(ChannelNames.Left);
            bool getRight = IsChannelActive(ChannelNames.Right);
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

                int frameCount = RealSense2API.FrameEmbeddedCount(data);
                log.Debug(string.Format("RealSense2: Got {0} Frames", frameCount));


                // extract all frames
                for (int j = 0; j < frameCount; j++)
                {
                    RealSense2API.RS2Frame frame = RealSense2API.FrameExtract(data, j);
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
            if (channelName == ChannelNames.ZImage)
            {
                return CalcZImage();
            }
            if (channelName == ChannelNames.Left)
            {
                return CalcIRImage(_currentLeftFrame);
            }
            if (channelName == ChannelNames.Right)
            {
                return CalcIRImage(_currentRightFrame);
            }

            log.Error("Unexpected ChannelName in CalcChannel().");
            return null;
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
                if (IsChannelActive(ChannelNames.Left) || IsChannelActive(ChannelNames.Right))
                {
                    string msg = string.Format("RealSense2: can't have {0}/{1} and {2} active at the same time", ChannelNames.Left, ChannelNames.Right, ChannelNames.ZImage);
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
            else if (channelName == ChannelNames.Left)
            {
                if(IsChannelActive(ChannelNames.ZImage))
                {
                    string msg = string.Format("RealSense2: can't have {0} and {1} active at the same time", ChannelNames.Left, ChannelNames.ZImage);
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
            else if (channelName == ChannelNames.Right)
            {
                if (IsChannelActive(ChannelNames.ZImage))
                {
                    string msg = string.Format("RealSense2: can't have {0} and {1} active at the same time", ChannelNames.Right, ChannelNames.ZImage);
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
                throw new InvalidOperationException(msg);
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
            else if (channelName == ChannelNames.Left)
            {
                stream = RealSense2API.Stream.INFRARED;
            }
            else if (channelName == ChannelNames.Right)
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
                string msg = string.Format("RealSense2: intrinsics distrotion model {0} does not match Metrilus.Util", intrinsics.model.ToString());
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
            RealSense2API.RS2Frame frame;

            switch (channelName)
            {
                case ChannelNames.Color:
                    frame = _currentColorFrame;
                    break;

                case ChannelNames.ZImage:
                    frame = _currentDepthFrame;
                    break;

                case ChannelNames.Left:
                    frame = _currentLeftFrame;
                    break;

                case ChannelNames.Right:
                    frame = _currentRightFrame;
                    break;

                default:
                    string msg = string.Format("RealSense2: stream profile for channel {0} not available", channelName);
                    log.Error(msg);
                    throw new Exception(msg);
            }

            if (!frame.IsValid())
            {
                string msg = string.Format("RealSense2: Can't get channel profile for {0} without having at least one frame available.", channelName);
                log.Error(msg);
                throw new InvalidOperationException(msg);
            }

            return RealSense2API.GetStreamProfile(frame);
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

            FloatCameraImage depthData = new FloatCameraImage(width, height);
            short* source = (short*)RealSense2API.GetFrameData(_currentDepthFrame);

            for (int y = 0; y < height; y++)
            {
                short* sourceLine = source + y * width;
                for (int x = 0; x < width; x++)
                {
                    depthData[y, x] = (float)(_depthScale * (*sourceLine++));
                }
            }

            return depthData;
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

            FloatCameraImage IRData = new FloatCameraImage(width, height);
            byte* source = (byte*)RealSense2API.GetFrameData(frame);

            for (int y = 0; y < height; y++)
            {
                byte* sourceLine = source + y * width;
                for (int x = 0; x < width; x++)
                {
                    IRData[y, x] = (float)(*sourceLine++);
                }
            }

            return IRData;
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

        public void LoadConfigPreset(AdvancedMode.Preset preset)
        {
            LoadCustomConfig(AdvancedMode.GetPreset(preset));
        }
        
        public void LoadCustomConfig(string json)
        {
            RealSense2API.RS2Device dev = RealSense2API.GetActiveDevice(_pipeline);
            RealSense2API.LoadAdvancedConfig(json, dev);
            RealSense2API.DeleteDevice(dev);
            _depthScale = RealSense2API.GetDepthScale(_pipeline);
        }
    }
}
