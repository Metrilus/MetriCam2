using System;
using System.Collections.Generic;
using System.Timers;
using System.IO;
using System.Drawing;
using Rtsp;
using Rtsp.Messages;

namespace MetriCam2.Cameras
{
    public class RTSPClient
    {
        private Timer _keepaliveTimer = null;
        private RtspListener _client;
        private string _session = null;
        private string _url = null;
        private int _videoDataChannel = 0;
        private H264Decoder _decoder = null;
        private H264Payload _h264Payload = new H264Payload();
        private Bitmap _currentBitmap = null;

        public System.Threading.AutoResetEvent NewBitmapAvailable { get; set; } = new System.Threading.AutoResetEvent(false);

        public RTSPClient(string ipAddress, uint port, string username, string password)
        {
            _url = $"rtsp://{username}:{password}@{ipAddress}:{port}/Streaming/channels/1/";

            RtspUtils.RegisterUri();

            RtspTcpTransport socket = new RtspTcpTransport(ipAddress, (int)port);
            _client = new RtspListener(socket);

            _client.MessageReceived += MessageReceived;
            _client.DataReceived += DataReceived;
        }

        public void Connect()
        {
            _client.Start();

            // Send first setup messge: OPTIONS
            RtspRequest optionsMessage = new RtspRequestOptions();
            optionsMessage.RtspUri = new Uri(_url);
            _client.SendMessage(optionsMessage);
        }

        public void Disconnect()
        {
            _client.Stop();
            _keepaliveTimer = null;
            _videoDataChannel = 0;
            _decoder = null;
            _h264Payload = new H264Payload();
        }

        public Bitmap GetCurrentBitmap()
        {
            return _currentBitmap;
        }

        private void MessageReceived(object sender, RtspChunkEventArgs e)
        {
            RtspResponse message = e.Message as RtspResponse;

            // If we get a reply to OPTIONS then start the Keepalive Timer and send DESCRIBE
            if (message.OriginalRequest != null && message.OriginalRequest is RtspRequestOptions)
            {
                // Start a Timer to send an Keepalive RTSP command every 20 seconds
                _keepaliveTimer = new Timer();
                _keepaliveTimer.Elapsed += SendKeepalive;
                _keepaliveTimer.Interval = 20 * 1000;
                _keepaliveTimer.Enabled = true;

                // Send DESCRIBE
                RtspRequest describe_message = new RtspRequestDescribe();
                describe_message.RtspUri = new Uri(_url);
                _client.SendMessage(describe_message);
            }


            if (message.OriginalRequest != null && message.OriginalRequest is RtspRequestDescribe)
            {
                Rtsp.Sdp.SdpFile sdp_data;
                using (StreamReader sdp_stream = new StreamReader(new MemoryStream(message.Data)))
                {
                    sdp_data = Rtsp.Sdp.SdpFile.Read(sdp_stream);
                }

                Uri video_uri = null;
                foreach (Rtsp.Sdp.Attribut attrib in sdp_data.Medias[0].Attributs)
                {
                    if (attrib.Key.Equals("control"))
                    {
                        video_uri = new Uri(attrib.Value);
                    }
                }

                RtspTransport transport = new RtspTransport()
                {
                    LowerTransport = RtspTransport.LowerTransportType.TCP,
                    Interleaved = new PortCouple(0, 1),
                };

                // Generate SETUP messages
                RtspRequestSetup setup_message = new RtspRequestSetup();
                setup_message.RtspUri = video_uri;
                setup_message.AddTransport(transport);

                _client.SendMessage(setup_message);
            }


            if (message.OriginalRequest != null && message.OriginalRequest is RtspRequestSetup)
            {
                if (message.Timeout > 0 && message.Timeout > _keepaliveTimer.Interval / 1000)
                {
                    _keepaliveTimer.Interval = message.Timeout * 1000 / 2;
                }

                // Send PLAY
                RtspRequest play_message = new RtspRequestPlay();
                play_message.RtspUri = new Uri(_url);
                play_message.Session = message.Session;
                _client.SendMessage(play_message);
            }
        }

        private void DataReceived(object sender, RtspChunkEventArgs e)
        {
            RtspData data_received = e.Message as RtspData;

            if (data_received.Channel == _videoDataChannel)
            {
                // Received some Video or Audio Data on the correct channel.

                // RTP Packet Header
                // 0 - Version, P, X, CC, M, PT and Sequence Number
                //32 - Timestamp
                //64 - SSRC
                //96 - CSRCs (optional)
                //nn - Extension ID and Length
                //nn - Extension header

                int rtp_version = (e.Message.Data[0] >> 6);
                int rtp_padding = (e.Message.Data[0] >> 5) & 0x01;
                int rtp_extension = (e.Message.Data[0] >> 4) & 0x01;
                int rtp_csrc_count = (e.Message.Data[0] >> 0) & 0x0F;
                int rtp_marker = (e.Message.Data[1] >> 7) & 0x01;
                int rtp_payload_type = (e.Message.Data[1] >> 0) & 0x7F;
                uint rtp_sequence_number = ((uint)e.Message.Data[2] << 8) + (uint)(e.Message.Data[3]);
                uint rtp_timestamp = ((uint)e.Message.Data[4] << 24) + (uint)(e.Message.Data[5] << 16) + (uint)(e.Message.Data[6] << 8) + (uint)(e.Message.Data[7]);
                uint rtp_ssrc = ((uint)e.Message.Data[8] << 24) + (uint)(e.Message.Data[9] << 16) + (uint)(e.Message.Data[10] << 8) + (uint)(e.Message.Data[11]);

                int rtp_payload_start = 4 // V,P,M,SEQ
                                    + 4 // time stamp
                                    + 4 // ssrc
                                    + (4 * rtp_csrc_count); // zero or more csrcs

                uint rtp_extension_id = 0;
                uint rtp_extension_size = 0;
                if (rtp_extension == 1)
                {
                    rtp_extension_id = ((uint)e.Message.Data[rtp_payload_start + 0] << 8) + (uint)(e.Message.Data[rtp_payload_start + 1] << 0);
                    rtp_extension_size = ((uint)e.Message.Data[rtp_payload_start + 2] << 8) + (uint)(e.Message.Data[rtp_payload_start + 3] << 0) * 4; // units of extension_size is 4-bytes
                    rtp_payload_start += 4 + (int)rtp_extension_size;  // extension header and extension payload
                }


                if (data_received.Channel == _videoDataChannel && rtp_payload_type == 96)
                {
                    byte[] rtp_payload = new byte[e.Message.Data.Length - rtp_payload_start];
                    Array.Copy(e.Message.Data, rtp_payload_start, rtp_payload, 0, rtp_payload.Length);

                    List<byte[]> nal_units = _h264Payload.Process_H264_RTP_Packet(rtp_payload, rtp_marker);

                    if (nal_units != null)
                    {
                        // we have passed in enough RTP packets to make a Frame of video
                        try
                        {
                            if (_decoder == null)
                            {
                                _decoder = new H264Decoder(nal_units);
                            }

                            _currentBitmap = _decoder.Update(nal_units);

                            NewBitmapAvailable.Set();
                        }
                        catch (Exception error)
                        {
                            Console.WriteLine(error.Message);
                        }
                    }
                }
            }
        }

        private void SendKeepalive(object sender, ElapsedEventArgs e)
        {
            RtspRequest getparam_message = new RtspRequestGetParameter();
            getparam_message.RtspUri = new Uri(_url);
            getparam_message.Session = _session;
            _client.SendMessage(getparam_message);
        }
    }
}
