using MetriCam2;
using MetriCam2.Cameras;
using MetriCam2.Exceptions;
using Metrilus.Logging;
using Metrilus.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetriCam2.Tests.UnitTestParameterMechanism
{
    class Program
    {
        private static Camera cam;
        private static MetriLog log = new MetriLog("UnitTestParameterMechanism");

        static void Main(string[] args)
        {
#if DEBUG
            log.SetLogLevel(log4net.Core.Level.Debug);
#endif

            cam = new CameraTemplate();

            // Test all different parameter types
            // Scalar types
            TestBoolParam();
            TestByteParam();
            TestShortParam();
            TestIntParam();
            TestLongParam();
            TestFloatParam();
            TestDoubleParam();

            // List types
            TestEnumListParam();
            TestFloatListParam();

            // Range types
            TestByteRangeParam();
            TestShortRangeParam();
            TestIntRangeParam();
            TestLongRangeParam();
            TestFloatRangeParam();
            TestDoubleRangeParam();

            // Special Types
            // e.g. Point3f (WhiteBalance)
            TestPoint3fParam();
        }

        #region Test Methods for Scalar Parameters
        private static void TestBoolParam()
        {
            string paramName = "BoolParam";
            log.Info("Testing a ParamDesc<bool>");

            SetParameterToValid(paramName, true);
            SetParameterToValid(paramName, false);

            SetParameterToInvalid(paramName, -1);
            SetParameterToInvalid(paramName, 0);
            SetParameterToInvalid(paramName, 1);
            SetParameterToInvalid(paramName, 2);

            SetParameterToInvalid(paramName, -1f);
            SetParameterToInvalid(paramName, 0f);
            SetParameterToInvalid(paramName, 1f);
            SetParameterToInvalid(paramName, 2f);

            SetParameterToValid(paramName, "true");
            SetParameterToValid(paramName, "True");
            SetParameterToValid(paramName, "TRUE");

            SetParameterToValid(paramName, "false");
            SetParameterToValid(paramName, "False");
            SetParameterToValid(paramName, "FALSE");

            SetParameterToInvalid(paramName, "on");
            SetParameterToInvalid(paramName, "On");
            SetParameterToInvalid(paramName, "ON");

            SetParameterToInvalid(paramName, "off");
            SetParameterToInvalid(paramName, "Off");
            SetParameterToInvalid(paramName, "OFF");

            SetParameterToInvalid(paramName, "-1");
            SetParameterToInvalid(paramName, "0");
            SetParameterToInvalid(paramName, "1");
            SetParameterToInvalid(paramName, "2");
        }

        private static void TestByteParam()
        {
            string paramName = "ByteParam";
            log.Info("Testing a ParamDesc<byte>");
            byte min = byte.MinValue;
            byte max = byte.MaxValue;
            long oobLow = min - 1;
            long oobHigh = max + 1;

            // Test integer types
            SetParameterToValid(paramName, min);
            SetParameterToValid(paramName, max);
            SetParameterToValid(paramName, (short)min);
            SetParameterToValid(paramName, (short)max);
            SetParameterToValid(paramName, (int)min);
            SetParameterToValid(paramName, (int)max);
            SetParameterToValid(paramName, (long)min);
            SetParameterToValid(paramName, (long)max);
            // Test non-integer types
            SetParameterToValid(paramName, (float)min);
            SetParameterToValid(paramName, (float)max);
            SetParameterToValid(paramName, (double)min);
            SetParameterToValid(paramName, (double)max);
            SetParameterToInvalid(paramName, (float)((float)min + 0.5f));
            SetParameterToInvalid(paramName, (float)((float)max - 0.5f));
            SetParameterToInvalid(paramName, (double)min + 0.5);
            SetParameterToInvalid(paramName, (double)max - 0.5);

            // Test out-of-bounds
            SetParameterToInvalid(paramName, oobLow);
            SetParameterToInvalid(paramName, oobHigh);

            // Strings may be ok, but not if they contain a decimal separator
            SetParameterToValid(paramName, min.ToString());
            SetParameterToInvalid(paramName, min.ToString("D") + ".");
            SetParameterToInvalid(paramName, min.ToString("F1"));
            SetParameterToInvalid(paramName, min.ToString("F2"));
            SetParameterToValid(paramName, max.ToString());
            SetParameterToInvalid(paramName, max.ToString("D") + ".");
            SetParameterToInvalid(paramName, max.ToString("F1"));
            SetParameterToInvalid(paramName, max.ToString("F2"));
            // out-of-bounds
            SetParameterToInvalid(paramName, oobLow.ToString());
            SetParameterToInvalid(paramName, oobLow.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F1"));
            SetParameterToInvalid(paramName, oobLow.ToString("F2"));
            SetParameterToInvalid(paramName, oobHigh.ToString());
            SetParameterToInvalid(paramName, oobHigh.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F1"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F2"));
            // invalid values
            TestNonNumericValues(paramName);
        }

        private static void TestShortParam()
        {
            string paramName = "ShortParam";
            log.Info("Testing a ParamDesc<short>");
            short min = short.MinValue;
            short max = short.MaxValue;
            long oobLow = min - 1;
            long oobHigh = max + 1;

            // Test integer types
            SetParameterToValid(paramName, byte.MinValue);
            SetParameterToValid(paramName, byte.MaxValue);
            SetParameterToValid(paramName, min);
            SetParameterToValid(paramName, max);
            SetParameterToValid(paramName, (int)min);
            SetParameterToValid(paramName, (int)max);
            SetParameterToValid(paramName, (long)min);
            SetParameterToValid(paramName, (long)max);
            // Test non-integer types
            SetParameterToValid(paramName, (float)min);
            SetParameterToValid(paramName, (float)max);
            SetParameterToValid(paramName, (double)min);
            SetParameterToValid(paramName, (double)max);
            SetParameterToInvalid(paramName, (float)((float)min + 0.5f));
            SetParameterToInvalid(paramName, (float)((float)max - 0.5f));
            SetParameterToInvalid(paramName, (double)min + 0.5);
            SetParameterToInvalid(paramName, (double)max - 0.5);

            // Test out-of-bounds
            SetParameterToInvalid(paramName, oobLow);
            SetParameterToInvalid(paramName, oobHigh);

            // Strings may be ok, but not if they contain a decimal separator
            SetParameterToValid(paramName, min.ToString());
            SetParameterToInvalid(paramName, min.ToString("D") + ".");
            SetParameterToInvalid(paramName, min.ToString("F1"));
            SetParameterToInvalid(paramName, min.ToString("F2"));
            SetParameterToValid(paramName, max.ToString());
            SetParameterToInvalid(paramName, max.ToString("D") + ".");
            SetParameterToInvalid(paramName, max.ToString("F1"));
            SetParameterToInvalid(paramName, max.ToString("F2"));
            // out-of-bounds
            SetParameterToInvalid(paramName, oobLow.ToString());
            SetParameterToInvalid(paramName, oobLow.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F1"));
            SetParameterToInvalid(paramName, oobLow.ToString("F2"));
            SetParameterToInvalid(paramName, oobHigh.ToString());
            SetParameterToInvalid(paramName, oobHigh.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F1"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F2"));
            // invalid values
            TestNonNumericValues(paramName);
        }

        private static void TestIntParam()
        {
            string paramName = "IntParam";
            log.Info("Testing a ParamDesc<int>");
            int min = int.MinValue;
            int max = int.MaxValue;
            long oobLow = (long)min - 1L;
            long oobHigh = (long)max + 1L;

            // Test integer types
            SetParameterToValid(paramName, byte.MinValue);
            SetParameterToValid(paramName, byte.MaxValue);
            SetParameterToValid(paramName, short.MinValue);
            SetParameterToValid(paramName, short.MaxValue);
            SetParameterToValid(paramName, min);
            SetParameterToValid(paramName, max);
            SetParameterToValid(paramName, (long)min);
            SetParameterToValid(paramName, (long)max);
            // Test non-integer types
            SetParameterToValid(paramName, (float)short.MinValue);
            SetParameterToValid(paramName, (float)short.MaxValue);
            SetParameterToValid(paramName, (double)short.MinValue);
            SetParameterToValid(paramName, (double)short.MaxValue);
            //SetParameterToValid(paramName, (float)(min / 4)); // disabled due to float rounding issues
            //SetParameterToValid(paramName, (float)(max / 4)); // disabled due to float rounding issues
            //SetParameterToValid(paramName, (double)min);
            //SetParameterToValid(paramName, (double)max);
            //SetParameterToInvalid(paramName, (float)((float)min + 0.5f));
            //SetParameterToInvalid(paramName, (float)((float)max - 0.5f));
            //SetParameterToInvalid(paramName, (double)min + 0.5);
            //SetParameterToInvalid(paramName, (double)max - 0.5);

            // Test out-of-bounds
            SetParameterToInvalid(paramName, oobLow);
            SetParameterToInvalid(paramName, oobHigh);

            // Strings may be ok, but not if they contain a decimal separator
            SetParameterToValid(paramName, min.ToString());
            SetParameterToInvalid(paramName, min.ToString("D") + ".");
            SetParameterToInvalid(paramName, min.ToString("F1"));
            SetParameterToInvalid(paramName, min.ToString("F2"));
            SetParameterToValid(paramName, max.ToString());
            SetParameterToInvalid(paramName, max.ToString("D") + ".");
            SetParameterToInvalid(paramName, max.ToString("F1"));
            SetParameterToInvalid(paramName, max.ToString("F2"));
            // out-of-bounds
            SetParameterToInvalid(paramName, oobLow.ToString());
            SetParameterToInvalid(paramName, oobLow.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F1"));
            SetParameterToInvalid(paramName, oobLow.ToString("F2"));
            SetParameterToInvalid(paramName, oobHigh.ToString());
            SetParameterToInvalid(paramName, oobHigh.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F1"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F2"));
            // invalid values
            TestNonNumericValues(paramName);
        }

        private static void TestLongParam()
        {
            string paramName = "LongParam";
            log.Info("Testing a ParamDesc<long>");
            long min = long.MinValue;
            long max = long.MaxValue;

            // Test integer types
            SetParameterToValid(paramName, byte.MinValue);
            SetParameterToValid(paramName, byte.MaxValue);
            SetParameterToValid(paramName, short.MinValue);
            SetParameterToValid(paramName, short.MaxValue);
            SetParameterToValid(paramName, int.MinValue);
            SetParameterToValid(paramName, int.MaxValue);
            SetParameterToValid(paramName, min);
            SetParameterToValid(paramName, max);
            // Test non-integer types
            SetParameterToValid(paramName, (float)short.MinValue);
            SetParameterToValid(paramName, (float)short.MaxValue);
            SetParameterToValid(paramName, (double)short.MinValue);
            SetParameterToValid(paramName, (double)short.MaxValue);
            //SetParameterToValid(paramName, (float)min); // disabled due to float rounding issues
            //SetParameterToValid(paramName, (float)max); // disabled due to float rounding issues
            //SetParameterToValid(paramName, (double)min); // disabled due to double rounding issues
            //SetParameterToValid(paramName, (double)max); // disabled due to double rounding issues
            //SetParameterToInvalid(paramName, (float)((float)min + 0.5f));
            //SetParameterToInvalid(paramName, (float)((float)max - 0.5f));
            //SetParameterToInvalid(paramName, (double)min + 0.5);
            //SetParameterToInvalid(paramName, (double)max - 0.5);

            // Test out-of-bounds
            // impossible without BigInteger

            // Strings may be ok, but not if they contain a decimal separator
            SetParameterToValid(paramName, min.ToString());
            SetParameterToInvalid(paramName, min.ToString("D") + ".");
            SetParameterToInvalid(paramName, min.ToString("F1"));
            SetParameterToInvalid(paramName, min.ToString("F2"));
            SetParameterToValid(paramName, max.ToString());
            SetParameterToInvalid(paramName, max.ToString("D") + ".");
            SetParameterToInvalid(paramName, max.ToString("F1"));
            SetParameterToInvalid(paramName, max.ToString("F2"));
            // invalid values
            TestNonNumericValues(paramName);
        }

        private static void TestFloatParam()
        {
            string paramName = "FloatParam";
            log.Info("Testing a ParamDesc<float>");
            float min = float.MinValue;
            float max = float.MaxValue;
            double oobLow = ((double)min) * 2.0;
            double oobHigh = ((double)max) * 2.0;

            // Test integer types
            SetParameterToValid(paramName, byte.MinValue);
            SetParameterToValid(paramName, byte.MaxValue);
            SetParameterToValid(paramName, short.MinValue);
            SetParameterToValid(paramName, short.MaxValue);
            SetParameterToValid(paramName, int.MinValue);
            SetParameterToValid(paramName, int.MaxValue);
            SetParameterToValid(paramName, long.MinValue);
            SetParameterToValid(paramName, long.MaxValue);
            // Test non-integer types
            SetParameterToValid(paramName, min);
            SetParameterToValid(paramName, max);
            SetParameterToValid(paramName, (double)min);
            SetParameterToValid(paramName, (double)max);
            SetParameterToValid(paramName, (float)(min + 0.5f));
            SetParameterToValid(paramName, (float)(max - 0.5f));
            SetParameterToValid(paramName, (double)min + 0.5);
            SetParameterToValid(paramName, (double)max - 0.5);

            // Test out-of-bounds
            SetParameterToInvalid(paramName, oobLow);
            SetParameterToInvalid(paramName, oobHigh);

            // Strings may be ok, but not if they contain a decimal separator
            SetParameterToValid(paramName, min.ToString());
            SetParameterToValid(paramName, min.ToString("R"));
            SetParameterToInvalid(paramName, min.ToString("R") + ".");
            SetParameterToValid(paramName, min.ToString("F"));
            SetParameterToInvalid(paramName, min.ToString("F") + ".");
            SetParameterToValid(paramName, min.ToString("F1"));
            SetParameterToValid(paramName, min.ToString("F2"));
            SetParameterToValid(paramName, max.ToString());
            SetParameterToValid(paramName, max.ToString("R"));
            SetParameterToInvalid(paramName, max.ToString("R") + ".");
            SetParameterToValid(paramName, max.ToString("F"));
            SetParameterToInvalid(paramName, max.ToString("F") + ".");
            SetParameterToValid(paramName, max.ToString("F1"));
            SetParameterToValid(paramName, max.ToString("F2"));
            // out-of-bounds
            SetParameterToInvalid(paramName, oobLow.ToString());
            SetParameterToInvalid(paramName, oobLow.ToString("R"));
            SetParameterToInvalid(paramName, oobLow.ToString("R") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F"));
            SetParameterToInvalid(paramName, oobLow.ToString("F") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F1"));
            SetParameterToInvalid(paramName, oobLow.ToString("F2"));
            SetParameterToInvalid(paramName, oobHigh.ToString());
            SetParameterToInvalid(paramName, oobHigh.ToString("R"));
            SetParameterToInvalid(paramName, oobHigh.ToString("R") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F1"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F2"));
            // invalid values
            TestNonNumericValues(paramName);
        }

        private static void TestDoubleParam()
        {
            string paramName = "DoubleParam";
            log.Info("Testing a ParamDesc<double>");
            double min = double.MinValue;
            double max = double.MaxValue;

            // Test integer types
            SetParameterToValid(paramName, byte.MinValue);
            SetParameterToValid(paramName, byte.MaxValue);
            SetParameterToValid(paramName, short.MinValue);
            SetParameterToValid(paramName, short.MaxValue);
            SetParameterToValid(paramName, int.MinValue);
            SetParameterToValid(paramName, int.MaxValue);
            SetParameterToValid(paramName, long.MinValue);
            SetParameterToValid(paramName, long.MaxValue);
            // Test non-integer types
            SetParameterToValid(paramName, float.MinValue);
            SetParameterToValid(paramName, float.MaxValue);
            SetParameterToValid(paramName, min);
            SetParameterToValid(paramName, max);
            SetParameterToValid(paramName, (double)min + 0.5);
            SetParameterToValid(paramName, (double)max - 0.5);

            // Test out-of-bounds
            // impossible

            // Strings may be ok, but not if they contain a decimal separator
            // Extreme double values do not convert well with format specifier F or without a format specifier
            SetParameterToValid(paramName, float.MinValue.ToString());
            SetParameterToValid(paramName, min.ToString("R"));
            SetParameterToInvalid(paramName, min.ToString("R") + ".");
            SetParameterToValid(paramName, float.MinValue.ToString("F"));
            SetParameterToInvalid(paramName, float.MinValue.ToString("F") + ".");
            SetParameterToValid(paramName, float.MinValue.ToString("F1"));
            SetParameterToValid(paramName, float.MinValue.ToString("F2"));
            SetParameterToValid(paramName, float.MaxValue.ToString());
            SetParameterToValid(paramName, max.ToString("R"));
            SetParameterToInvalid(paramName, max.ToString("R") + ".");
            SetParameterToValid(paramName, float.MaxValue.ToString("F"));
            SetParameterToInvalid(paramName, float.MaxValue.ToString("F") + ".");
            SetParameterToValid(paramName, float.MaxValue.ToString("F1"));
            SetParameterToValid(paramName, float.MaxValue.ToString("F2"));
            // invalid values
            TestNonNumericValues(paramName);
        }
        #endregion

        #region Test Methods for List Parameters
        private static void TestEnumListParam()
        {
            string paramName = "EnumListParam";
            log.Info("Testing a ListParamDesc<TriggerModeDummy>");
            // allowed values are
            //TriggerModeDummy.FREERUN
            //TriggerModeDummy.SOFTWARE
            //TriggerModeDummy.HARDWARE

            SetParameterToValid(paramName, CameraTemplate.TriggerModeDummy.FREERUN);
            SetParameterToValid(paramName, CameraTemplate.TriggerModeDummy.SOFTWARE);
            SetParameterToValid(paramName, CameraTemplate.TriggerModeDummy.HARDWARE);

            SetParameterToInvalid(paramName, "freerun");
            SetParameterToInvalid(paramName, "software");
            SetParameterToInvalid(paramName, "hardware");

            SetParameterToInvalid(paramName, "Freerun");
            SetParameterToInvalid(paramName, "Software");
            SetParameterToInvalid(paramName, "Hardware");

            SetParameterToValid(paramName, "FREERUN");
            SetParameterToValid(paramName, "SOFTWARE");
            SetParameterToValid(paramName, "HARDWARE");

            TestNonNumericValues(paramName);
        }

        private static void TestFloatListParam()
        {
            string paramName = "FloatListParam";
            log.Info("Testing a ListParamDesc<float>");
            // allowed values are
            //allowedValues.Add(16f);
            //allowedValues.Add(18.0f);
            //allowedValues.Add(20.5f);
            //allowedValues.Add(24.07f);

            SetParameterToValid(paramName, 16f);
            SetParameterToValid(paramName, 18.0f);
            SetParameterToValid(paramName, 20.5f);
            SetParameterToValid(paramName, 24.07f);

            // integer values
            SetParameterToValid(paramName, 16);
            SetParameterToValid(paramName, 18);
            SetParameterToInvalid(paramName, 20);
            SetParameterToInvalid(paramName, 24);

            SetParameterToValid(paramName, "16");
            SetParameterToValid(paramName, "16.");
            SetParameterToValid(paramName, "16.0");
            SetParameterToValid(paramName, "16.00");
            SetParameterToValid(paramName, "20.5");
            SetParameterToValid(paramName, "20.50");
            SetParameterToValid(paramName, "24.07");
            SetParameterToValid(paramName, "24.070");
            SetParameterToInvalid(paramName, "24.069");
            SetParameterToInvalid(paramName, "24.071");

            TestNonNumericValues(paramName);
        }
        #endregion

        #region Test Methods for Range Parameters
        private static void TestByteRangeParam()
        {
            string paramName = "ByteRangeParam";
            log.Info("Testing a RangeParamDesc<byte>");
            Camera.IRangeParamDesc<byte> desc = (Camera.IRangeParamDesc<byte>)cam.GetParameter(paramName);
            byte min = desc.Min;
            byte max = desc.Max;
            long oobLow = min - 1;
            long oobHigh = max + 1;

            // Test integer types
            SetParameterToValid(paramName, min);
            SetParameterToValid(paramName, max);
            SetParameterToValid(paramName, (short)min);
            SetParameterToValid(paramName, (short)max);
            SetParameterToValid(paramName, (int)min);
            SetParameterToValid(paramName, (int)max);
            SetParameterToValid(paramName, (long)min);
            SetParameterToValid(paramName, (long)max);
            // Test non-integer types
            SetParameterToValid(paramName, (float)min);
            SetParameterToValid(paramName, (float)max);
            SetParameterToValid(paramName, (double)min);
            SetParameterToValid(paramName, (double)max);
            SetParameterToInvalid(paramName, (float)((float)min + 0.5f));
            SetParameterToInvalid(paramName, (float)((float)max - 0.5f));
            SetParameterToInvalid(paramName, (double)min + 0.5);
            SetParameterToInvalid(paramName, (double)max - 0.5);

            // Test out-of-bounds
            SetParameterToInvalid(paramName, oobLow);
            SetParameterToInvalid(paramName, oobHigh);

            // Strings may be ok, but not if they contain a decimal separator
            SetParameterToValid(paramName, min.ToString());
            SetParameterToInvalid(paramName, min.ToString("D") + ".");
            SetParameterToInvalid(paramName, min.ToString("F1"));
            SetParameterToInvalid(paramName, min.ToString("F2"));
            SetParameterToValid(paramName, max.ToString());
            SetParameterToInvalid(paramName, max.ToString("D") + ".");
            SetParameterToInvalid(paramName, max.ToString("F1"));
            SetParameterToInvalid(paramName, max.ToString("F2"));
            // out-of-bounds
            SetParameterToInvalid(paramName, oobLow.ToString());
            SetParameterToInvalid(paramName, oobLow.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F1"));
            SetParameterToInvalid(paramName, oobLow.ToString("F2"));
            SetParameterToInvalid(paramName, oobHigh.ToString());
            SetParameterToInvalid(paramName, oobHigh.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F1"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F2"));
            // invalid values
            TestNonNumericValues(paramName);
        }

        private static void TestShortRangeParam()
        {
            string paramName = "ShortRangeParam";
            log.Info("Testing a RangeParamDesc<short>");
            Camera.IRangeParamDesc<short> desc = (Camera.IRangeParamDesc<short>)cam.GetParameter(paramName);
            short min = desc.Min;
            short max = desc.Max;
            long oobLow = min - 1;
            long oobHigh = max + 1;

            // Test integer types
            SetParameterToValid(paramName, (byte)min);
            SetParameterToValid(paramName, (byte)max);
            SetParameterToValid(paramName, (short)min);
            SetParameterToValid(paramName, (short)max);
            SetParameterToValid(paramName, (int)min);
            SetParameterToValid(paramName, (int)max);
            SetParameterToValid(paramName, (long)min);
            SetParameterToValid(paramName, (long)max);
            // Test non-integer types
            SetParameterToValid(paramName, (float)min);
            SetParameterToValid(paramName, (float)max);
            SetParameterToValid(paramName, (double)min);
            SetParameterToValid(paramName, (double)max);
            SetParameterToInvalid(paramName, (float)((float)min + 0.5f));
            SetParameterToInvalid(paramName, (float)((float)max - 0.5f));
            SetParameterToInvalid(paramName, (double)min + 0.5);
            SetParameterToInvalid(paramName, (double)max - 0.5);

            // Test out-of-bounds
            SetParameterToInvalid(paramName, oobLow);
            SetParameterToInvalid(paramName, oobHigh);

            // Strings may be ok, but not if they contain a decimal separator
            SetParameterToValid(paramName, min.ToString());
            SetParameterToInvalid(paramName, min.ToString("D") + ".");
            SetParameterToInvalid(paramName, min.ToString("F1"));
            SetParameterToInvalid(paramName, min.ToString("F2"));
            SetParameterToValid(paramName, max.ToString());
            SetParameterToInvalid(paramName, max.ToString("D") + ".");
            SetParameterToInvalid(paramName, max.ToString("F1"));
            SetParameterToInvalid(paramName, max.ToString("F2"));
            // out-of-bounds
            SetParameterToInvalid(paramName, oobLow.ToString());
            SetParameterToInvalid(paramName, oobLow.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F1"));
            SetParameterToInvalid(paramName, oobLow.ToString("F2"));
            SetParameterToInvalid(paramName, oobHigh.ToString());
            SetParameterToInvalid(paramName, oobHigh.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F1"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F2"));
            // invalid values
            TestNonNumericValues(paramName);
        }

        private static void TestIntRangeParam()
        {
            string paramName = "IntRangeParam";
            log.Info("Testing a RangeParamDesc<int>");
            Camera.IRangeParamDesc<int> desc = (Camera.IRangeParamDesc<int>)cam.GetParameter(paramName);
            int min = desc.Min;
            int max = desc.Max;
            long oobLow = (long)min - 1L;
            long oobHigh = (long)max + 1L;

            // Test integer types
            SetParameterToValid(paramName, (byte)min);
            SetParameterToValid(paramName, (byte)max);
            SetParameterToValid(paramName, (short)min);
            SetParameterToValid(paramName, (short)max);
            SetParameterToValid(paramName, (int)min);
            SetParameterToValid(paramName, (int)max);
            SetParameterToValid(paramName, (long)min);
            SetParameterToValid(paramName, (long)max);
            // Test non-integer types
            SetParameterToValid(paramName, (float)min);
            SetParameterToValid(paramName, (float)max);
            SetParameterToValid(paramName, (double)min);
            SetParameterToValid(paramName, (double)max);
            //SetParameterToValid(paramName, (float)(min / 4)); // disabled due to float rounding issues
            //SetParameterToValid(paramName, (float)(max / 4)); // disabled due to float rounding issues
            //SetParameterToValid(paramName, (double)min);
            //SetParameterToValid(paramName, (double)max);
            //SetParameterToInvalid(paramName, (float)((float)min + 0.5f));
            //SetParameterToInvalid(paramName, (float)((float)max - 0.5f));
            //SetParameterToInvalid(paramName, (double)min + 0.5);
            //SetParameterToInvalid(paramName, (double)max - 0.5);

            // Test out-of-bounds
            SetParameterToInvalid(paramName, oobLow);
            SetParameterToInvalid(paramName, oobHigh);

            // Strings may be ok, but not if they contain a decimal separator
            SetParameterToValid(paramName, min.ToString());
            SetParameterToInvalid(paramName, min.ToString("D") + ".");
            SetParameterToInvalid(paramName, min.ToString("F1"));
            SetParameterToInvalid(paramName, min.ToString("F2"));
            SetParameterToValid(paramName, max.ToString());
            SetParameterToInvalid(paramName, max.ToString("D") + ".");
            SetParameterToInvalid(paramName, max.ToString("F1"));
            SetParameterToInvalid(paramName, max.ToString("F2"));
            // out-of-bounds
            SetParameterToInvalid(paramName, oobLow.ToString());
            SetParameterToInvalid(paramName, oobLow.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F1"));
            SetParameterToInvalid(paramName, oobLow.ToString("F2"));
            SetParameterToInvalid(paramName, oobHigh.ToString());
            SetParameterToInvalid(paramName, oobHigh.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F1"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F2"));
            // invalid values
            TestNonNumericValues(paramName);
        }

        private static void TestLongRangeParam()
        {
            string paramName = "LongRangeParam";
            log.Info("Testing a RangeParamDesc<long>");
            Camera.IRangeParamDesc<long> desc = (Camera.IRangeParamDesc<long>)cam.GetParameter(paramName);
            long min = desc.Min;
            long max = desc.Max;
            long oobLow = (long)min - 1L;
            long oobHigh = (long)max + 1L;

            // Test integer types
            SetParameterToValid(paramName, (byte)min);
            SetParameterToValid(paramName, (byte)max);
            SetParameterToValid(paramName, (short)min);
            SetParameterToValid(paramName, (short)max);
            SetParameterToValid(paramName, (int)min);
            SetParameterToValid(paramName, (int)max);
            SetParameterToValid(paramName, (long)min);
            SetParameterToValid(paramName, (long)max);
            // Test non-integer types
            SetParameterToValid(paramName, (float)min);
            SetParameterToValid(paramName, (float)max);
            SetParameterToValid(paramName, (double)min);
            SetParameterToValid(paramName, (double)max);
            //SetParameterToValid(paramName, (float)min); // disabled due to float rounding issues
            //SetParameterToValid(paramName, (float)max); // disabled due to float rounding issues
            //SetParameterToValid(paramName, (double)min); // disabled due to double rounding issues
            //SetParameterToValid(paramName, (double)max); // disabled due to double rounding issues
            //SetParameterToInvalid(paramName, (float)((float)min + 0.5f));
            //SetParameterToInvalid(paramName, (float)((float)max - 0.5f));
            //SetParameterToInvalid(paramName, (double)min + 0.5);
            //SetParameterToInvalid(paramName, (double)max - 0.5);

            // Test out-of-bounds
            SetParameterToInvalid(paramName, oobLow);
            SetParameterToInvalid(paramName, oobHigh);

            // Strings may be ok, but not if they contain a decimal separator
            SetParameterToValid(paramName, min.ToString());
            SetParameterToInvalid(paramName, min.ToString("D") + ".");
            SetParameterToInvalid(paramName, min.ToString("F1"));
            SetParameterToInvalid(paramName, min.ToString("F2"));
            SetParameterToValid(paramName, max.ToString());
            SetParameterToInvalid(paramName, max.ToString("D") + ".");
            SetParameterToInvalid(paramName, max.ToString("F1"));
            SetParameterToInvalid(paramName, max.ToString("F2"));
            // out-of-bounds
            SetParameterToInvalid(paramName, oobLow.ToString());
            SetParameterToInvalid(paramName, oobLow.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F1"));
            SetParameterToInvalid(paramName, oobLow.ToString("F2"));
            SetParameterToInvalid(paramName, oobHigh.ToString());
            SetParameterToInvalid(paramName, oobHigh.ToString("D") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F1"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F2"));
            // invalid values
            TestNonNumericValues(paramName);
        }

        private static void TestFloatRangeParam()
        {
            string paramName = "FloatRangeParam";
            log.Info("Testing a RangeParamDesc<float>");
            Camera.IRangeParamDesc<float> desc = (Camera.IRangeParamDesc<float>)cam.GetParameter(paramName);
            float min = desc.Min;
            float max = desc.Max;
            float oobLow = min - 0.1f;
            float oobHigh = max + 0.1f;

            // Test integer types
            SetParameterToValid(paramName, (byte)min);
            SetParameterToValid(paramName, (byte)max);
            SetParameterToValid(paramName, (short)min);
            SetParameterToValid(paramName, (short)max);
            SetParameterToValid(paramName, (int)min);
            SetParameterToValid(paramName, (int)max);
            SetParameterToValid(paramName, (long)min);
            SetParameterToValid(paramName, (long)max);
            // Test non-integer types
            SetParameterToValid(paramName, (float)min);
            SetParameterToValid(paramName, (float)max);
            SetParameterToValid(paramName, (double)min);
            SetParameterToValid(paramName, (double)max);
            SetParameterToValid(paramName, (float)(min + 0.5f));
            SetParameterToValid(paramName, (float)(max - 0.5f));
            SetParameterToValid(paramName, (double)min + 0.5);
            SetParameterToValid(paramName, (double)max - 0.5);

            // Test out-of-bounds
            SetParameterToInvalid(paramName, oobLow);
            SetParameterToInvalid(paramName, oobHigh);

            // Strings may be ok, but not if they contain a decimal separator
            SetParameterToValid(paramName, min.ToString());
            SetParameterToValid(paramName, min.ToString("R"));
            SetParameterToValid(paramName, min.ToString("R") + ".");
            SetParameterToValid(paramName, min.ToString("F"));
            SetParameterToInvalid(paramName, min.ToString("F") + ".");
            SetParameterToValid(paramName, min.ToString("F1"));
            SetParameterToValid(paramName, min.ToString("F2"));
            SetParameterToValid(paramName, max.ToString());
            SetParameterToValid(paramName, max.ToString("R"));
            SetParameterToValid(paramName, max.ToString("R") + ".");
            SetParameterToValid(paramName, max.ToString("F"));
            SetParameterToInvalid(paramName, max.ToString("F") + ".");
            SetParameterToValid(paramName, max.ToString("F1"));
            SetParameterToValid(paramName, max.ToString("F2"));
            // out-of-bounds
            SetParameterToInvalid(paramName, oobLow.ToString());
            SetParameterToInvalid(paramName, oobLow.ToString("R"));
            SetParameterToInvalid(paramName, oobLow.ToString("R") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F"));
            SetParameterToInvalid(paramName, oobLow.ToString("F") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F1"));
            SetParameterToInvalid(paramName, oobLow.ToString("F2"));
            SetParameterToInvalid(paramName, oobHigh.ToString());
            SetParameterToInvalid(paramName, oobHigh.ToString("R"));
            SetParameterToInvalid(paramName, oobHigh.ToString("R") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F1"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F2"));
            // invalid values
            TestNonNumericValues(paramName);
        }

        private static void TestDoubleRangeParam()
        {
            string paramName = "DoubleRangeParam";
            log.Info("Testing a RangeParamDesc<double>");
            Camera.IRangeParamDesc<double> desc = (Camera.IRangeParamDesc<double>)cam.GetParameter(paramName);
            double min = desc.Min;
            double max = desc.Max;
            double oobLow = min - 0.1;
            double oobHigh = max + 0.1;

            // Test integer types
            SetParameterToValid(paramName, (byte)min);
            SetParameterToValid(paramName, (byte)max);
            SetParameterToValid(paramName, (short)min);
            SetParameterToValid(paramName, (short)max);
            SetParameterToValid(paramName, (int)min);
            SetParameterToValid(paramName, (int)max);
            SetParameterToValid(paramName, (long)min);
            SetParameterToValid(paramName, (long)max);
            // Test non-integer types
            SetParameterToValid(paramName, (float)min);
            SetParameterToValid(paramName, (float)max);
            SetParameterToValid(paramName, (double)min);
            SetParameterToValid(paramName, (double)max);
            SetParameterToValid(paramName, (double)min + 0.5);
            SetParameterToValid(paramName, (double)max - 0.5);

            // Test out-of-bounds
            SetParameterToInvalid(paramName, oobLow);
            SetParameterToInvalid(paramName, oobHigh);

            // Strings may be ok, but not if they contain a decimal separator
            // Extreme double values do not convert well with format specifier F or without a format specifier
            SetParameterToValid(paramName, min.ToString());
            SetParameterToValid(paramName, min.ToString("R"));
            SetParameterToValid(paramName, min.ToString("R") + ".");
            SetParameterToValid(paramName, min.ToString("F"));
            SetParameterToInvalid(paramName, min.ToString("F") + ".");
            SetParameterToValid(paramName, min.ToString("F1"));
            SetParameterToValid(paramName, min.ToString("F2"));
            SetParameterToValid(paramName, max.ToString());
            SetParameterToValid(paramName, max.ToString("R"));
            SetParameterToValid(paramName, max.ToString("R") + ".");
            SetParameterToValid(paramName, max.ToString("F"));
            SetParameterToInvalid(paramName, max.ToString("F") + ".");
            SetParameterToValid(paramName, max.ToString("F1"));
            SetParameterToValid(paramName, max.ToString("F2"));
            // out-of-bounds
            SetParameterToInvalid(paramName, oobLow.ToString());
            SetParameterToInvalid(paramName, oobLow.ToString("R"));
            SetParameterToInvalid(paramName, oobLow.ToString("R") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F"));
            SetParameterToInvalid(paramName, oobLow.ToString("F") + ".");
            SetParameterToInvalid(paramName, oobLow.ToString("F1"));
            SetParameterToInvalid(paramName, oobLow.ToString("F2"));
            SetParameterToInvalid(paramName, oobHigh.ToString());
            SetParameterToInvalid(paramName, oobHigh.ToString("R"));
            SetParameterToInvalid(paramName, oobHigh.ToString("R") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F") + ".");
            SetParameterToInvalid(paramName, oobHigh.ToString("F1"));
            SetParameterToInvalid(paramName, oobHigh.ToString("F2"));
            // invalid values
            TestNonNumericValues(paramName);
        }
        #endregion

        #region Test Methods for Special Parameter Types
        private static void TestPoint3fParam()
        {
            string paramName = "Point3fParam";
            log.Info("Testing a ParamDesc<Point3f>");
            Point3f min = new Point3f(float.MinValue, float.MinValue, float.MinValue);
            Point3f max = new Point3f(float.MaxValue, float.MaxValue, float.MaxValue);
            float[] somevalues = new float[] { 0f, 0.5f, 1f, 5f, 10.0503f, 100f, 255f, 999.99999f, 1000f };
            List<Point3f> somepoints = new List<Point3f>();
            foreach (var item in somevalues)
            {
                somepoints.Add(new Point3f(item, item, item));
                somepoints.Add(new Point3f(-item, -item, -item));
            }

            // Test types
            SetParameterToValid(paramName, min);
            SetParameterToValid(paramName, max);

            // Strings should be ok
            foreach (var item in somepoints)
            {
                SetParameterToValid(paramName, item.ToString());
                SetParameterToValid(paramName, item.ToString("R"));
                SetParameterToValid(paramName, item.ToString("F"));
                SetParameterToValid(paramName, item.ToString("F1"));
                SetParameterToValid(paramName, item.ToString("F2"));
            }
            
            // invalid values
            TestNonNumericValues(paramName);
        }
        #endregion

        #region Generic Helper Methods
        private static void TestNonNumericValues(string paramName)
        {
            SetParameterToInvalid(paramName, true);
            SetParameterToInvalid(paramName, 'a');
            SetParameterToInvalid(paramName, "");
            SetParameterToInvalid(paramName, "abc");
            SetParameterToInvalid(paramName, new object());
        }

        private static void SetParameterToInvalid(string name, object value)
        {
            try
            {
                cam.SetParameter(name, value);
                log.ErrorFormat("Setting {0} to \"{1}\" should have thrown an Exception!", name, value);
            }
            catch (ArgumentException)
            {
                log.InfoFormat("Setting {0} to \"{1}\" expectedly caused an error.", name, value);
            }
            catch (ParameterNotSupportedException)
            {
                log.ErrorFormat("Setting {0} to \"{1}\" caused an unexpected error." + Environment.NewLine
                    + "Probably the camera does not support this parameter.", name, value);
                return;
            }
            LogParameterValue(name);
        }
        private static void SetParameterToValid(string name, object value)
        {
            try
            {
                cam.SetParameter(name, value);
            }
            catch (ArgumentException e)
            {
                log.ErrorFormat("Setting {0} to \"{1}\" caused an unexpected error. " + e.Message, name, value);
            }
            catch (ParameterNotSupportedException)
            {
                log.ErrorFormat("Setting {0} to \"{1}\" caused an unexpected error." + Environment.NewLine
                    + "Probably the camera does not support this parameter.", name, value);
                return;
            }
            LogParameterValue(name);
        }
#if false // unused in this unit test
        private static void TestSetParameterDisconnected(string name, object value)
        {
            try
            {
                cam.SetParameter(name, value);
                log.ErrorFormat("Setting \"{0}\" while being disconnected should have thrown an Exception!", name);
            }
            catch (ParameterNotSupportedException)
            {
                log.InfoFormat("Setting {0} to \"{1}\" while being disconnected caused an unexpected error." + Environment.NewLine
                    + "Probably the camera does not support this parameter.", name, value);
                return;
            }
            catch (InvalidOperationException)
            {
                log.InfoFormat("Setting \"{0}\" while being disconnected expectedly caused an error.", name);
            }
            LogParameterValue(name);
        }
#endif

        private static void LogParameterValue(string name)
        {
            log.Info(cam.GetParameter(name));
            if (Camera.ParamDesc.IsAutoParameterName(name))
            {
                log.Info(cam.GetParameter(Camera.ParamDesc.GetBaseParameterName(name)));
            }
        }
        #endregion
    }
}
