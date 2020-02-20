using FFmpeg.AutoGen;
using Metrilus.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MetriCam2.Cameras
{
    public class H264Decoder
    {
        //private static bool initialized = false;
        private unsafe AVFormatContext* ic = null;
        private unsafe AVStream* video_st = null;
        private unsafe SwsContext* img_convert_ctx = null;
        private unsafe AVFrame* yuv_image = null;
        private unsafe AVFrame* rgb_image = null;
        private static int bufferSize = 3840 * 2160 * 4;
        private static byte[] startSequence = { 0x00, 0x00, 0x00, 0x01 };
        private static bool initialized = false;
        private static int streamSuffix = 0;

        //private avio_alloc_context_read_packet readCallback;

        static H264Decoder()
        {
        }

        public unsafe H264Decoder(List<byte[]> nalUnits)
        {
            if (!initialized)
            {
                ffmpeg.av_register_all();
                initialized = true;
            }
            int localStreamSuffix = streamSuffix;
            streamSuffix++;

            int dataSize = 0;
            foreach (byte[] nalUnit in nalUnits)
            {
                dataSize += nalUnit.Length + 4;
            }

            byte* dat = (byte*)ffmpeg.av_malloc((ulong)dataSize);


            fixed (byte* start = startSequence)
            {
                foreach (byte[] nalUnit in nalUnits)
                {
                    fixed (byte* dataPtr = nalUnit)
                    {
                        UnmanagedMemory.CopyMemory(dat, start, (uint)startSequence.Length);
                        dat += startSequence.Length;
                        UnmanagedMemory.CopyMemory(dat, dataPtr, (uint)nalUnit.Length);
                        dat += nalUnit.Length;
                    }
                }

                dat -= dataSize;
            }

            AVFormatContext* icLocal = ffmpeg.avformat_alloc_context();

            ic = icLocal;

            avio_alloc_context_write_packet_func writeCallback;
            writeCallback.Pointer = IntPtr.Zero;
            avio_alloc_context_seek_func seekCallback;
            seekCallback.Pointer = IntPtr.Zero;
            avio_alloc_context_read_packet_func readCallback;
            readCallback.Pointer = IntPtr.Zero;

            icLocal->pb = ffmpeg.avio_alloc_context(dat, bufferSize, 0, null, readCallback, writeCallback, seekCallback);

            if (icLocal->pb == null)
            {
                throw new Exception("Failed to allocate ffmpeg context.");
            }

            // Need to probe buffer for input format unless you already know it
            AVProbeData probe_data;
            probe_data.buf_size = dataSize;
            probe_data.filename = (byte*)Marshal.StringToHGlobalAnsi($"stream_{localStreamSuffix}");
            probe_data.buf = (byte*)UnmanagedMemory.Alloc(probe_data.buf_size);
            UnmanagedMemory.CopyMemory(probe_data.buf, dat, (uint)probe_data.buf_size);

            AVInputFormat* pAVInputFormat = ffmpeg.av_probe_input_format(&probe_data, 1);

            if (pAVInputFormat == null)
            {
                pAVInputFormat = ffmpeg.av_probe_input_format(&probe_data, 0);
            }

            // cleanup
            UnmanagedMemory.DeAlloc((IntPtr)probe_data.buf, probe_data.buf_size);
            probe_data.buf = null;

            pAVInputFormat->flags |= ffmpeg.AVFMT_NOFILE;

            ffmpeg.avformat_open_input(&icLocal, $"stream_{localStreamSuffix}", pAVInputFormat, null);

            for (int i = 0; i < icLocal->nb_streams; i++)
            {
                AVCodecContext* enc = icLocal->streams[i]->codec;

                if (AVMediaType.AVMEDIA_TYPE_VIDEO == enc->codec_type)
                {
                    AVCodec* codec = ffmpeg.avcodec_find_decoder(enc->codec_id);

                    if (codec == null || ffmpeg.avcodec_open2(enc, codec, null) < 0)
                    {
                        //Console.WriteLine("Cannot find codec");
                    }

                    video_st = icLocal->streams[i];
                }
            }

            //Init picture
            yuv_image = ffmpeg.av_frame_alloc();
            yuv_image->format = -1; //We do not know the format of the raw decoded image            
        }

        ~H264Decoder()
        {
        }

        /// <summary>
        /// Provided the compressed package for the next frame and get the decoded image of the current frame.
        /// </summary>
        /// <param name="compressedPacket"></param>
        /// <returns></returns>
        public unsafe Bitmap Update(List<byte[]> nalUnits)
        {
            AVPacket packet;

            int dataSize = 0;
            foreach(byte[] nalUnit in nalUnits)
            {
                dataSize += nalUnit.Length + 4;
            }

            byte* dat = (byte*)ffmpeg.av_malloc((ulong)dataSize);


            fixed (byte* start = startSequence)
            {
                foreach (byte[] nalUnit in nalUnits)
                {
                    fixed (byte* dataPtr = nalUnit)
                    {
                        UnmanagedMemory.CopyMemory(dat, start, (uint)startSequence.Length);
                        dat += startSequence.Length;
                        UnmanagedMemory.CopyMemory(dat, dataPtr, (uint)nalUnit.Length);
                        dat += nalUnit.Length;
                    }
                }
                dat -= dataSize;
            }
            

            ffmpeg.av_packet_from_data(&packet, dat, dataSize);

            if (rgb_image != null)
            {
                GC.KeepAlive(nalUnits);
            }

            int ret = ffmpeg.avcodec_send_packet(video_st->codec, &packet);

            if (ret != 0)
            {
                throw new Exception("Error in avcodec_send_packet. Error code: " + ret.ToString());
            }

            ret = ffmpeg.avcodec_receive_frame(video_st->codec, yuv_image);

            if (ret < 0)
            {
                throw new Exception("Error in avcodec_receive_frame. Error code: " + ret.ToString());
            }

            ffmpeg.av_packet_unref(&packet);

            if (rgb_image == null)
            {
                rgb_image = ffmpeg.av_frame_alloc();
                rgb_image->format = (int)AVPixelFormat.AV_PIX_FMT_BGR24; //We want to transform the raw decoded image to BGR24
                rgb_image->width = yuv_image->width;
                rgb_image->height = yuv_image->height;
                ffmpeg.av_frame_get_buffer(rgb_image, 32);
            }

            AVCodecContext* pCodecCtx = video_st->codec;
            AVPixelFormat pixFormat;
            switch (pCodecCtx->pix_fmt)
            {
                case AVPixelFormat.AV_PIX_FMT_YUVJ420P:
                    pixFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
                    break;
                case AVPixelFormat.AV_PIX_FMT_YUVJ422P:
                    pixFormat = AVPixelFormat.AV_PIX_FMT_YUV422P;
                    break;
                case AVPixelFormat.AV_PIX_FMT_YUVJ444P:
                    pixFormat = AVPixelFormat.AV_PIX_FMT_YUV444P;
                    break;
                case AVPixelFormat.AV_PIX_FMT_YUVJ440P:
                    pixFormat = AVPixelFormat.AV_PIX_FMT_YUV440P;
                    break;
                default:
                    pixFormat = pCodecCtx->pix_fmt;
                    break;
            }

            //Convert from one of the YUV color formats provided by H264 decompression to RGB.
            img_convert_ctx = ffmpeg.sws_getCachedContext(img_convert_ctx, yuv_image->width, yuv_image->height, pixFormat, rgb_image->width, rgb_image->height, (AVPixelFormat)rgb_image->format, 0, null, null, null);
            ffmpeg.sws_scale(img_convert_ctx, yuv_image->data, yuv_image->linesize, 0, yuv_image->height, rgb_image->data, rgb_image->linesize);

            Bitmap bmp = new Bitmap(yuv_image->width, yuv_image->height, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(new Point(0, 0), new Size(bmp.Width, bmp.Height)), ImageLockMode.ReadWrite, bmp.PixelFormat);
            UnmanagedMemory.CopyMemory((IntPtr)bmpData.Scan0, (IntPtr)rgb_image->extended_data[0], bmp.Width * bmp.Height * 3);
            bmp.UnlockBits(bmpData);
            return bmp;
        }
    }

}

