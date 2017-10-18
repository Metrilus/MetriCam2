﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetriCam2.Cameras
{
    public class AdvancedMode
    {
        public enum Preset
        {
            NONE,
            SHORT_RANGE,
            HAND,
            HIGH_ACCURACY,
            HIGH_DENSITY,
            MEDIUM_DENSITY
        }

        public static string GetPreset(Preset p)
        {
            switch(p)
            {
                case Preset.SHORT_RANGE:
                    return SHORT_RANGE;

                case Preset.HAND:
                    return HAND;

                case Preset.HIGH_DENSITY:
                    return HIGH_DENSITY;

                case Preset.MEDIUM_DENSITY:
                    return MEDIUM_DENSITY;

                case Preset.HIGH_ACCURACY:
                    return HIGH_ACCURACY;

                case Preset.NONE:
                default:
                    return "";
            }
        }

        private const string SHORT_RANGE = @"
            {
                ""aux-param-autoexposure-setpoint"": ""1700"",
                ""aux-param-colorcorrection1"": ""0.129883"",
                ""aux-param-colorcorrection10"": ""-0.441406"",
                ""aux-param-colorcorrection11"": ""-0.441406"",
                ""aux-param-colorcorrection12"": ""-0.0390625"",
                ""aux-param-colorcorrection2"": ""0.399414"",
                ""aux-param-colorcorrection3"": ""0.399414"",
                ""aux-param-colorcorrection4"": ""-0.0693359"",
                ""aux-param-colorcorrection5"": ""-0.198242"",
                ""aux-param-colorcorrection6"": ""-0.40332"",
                ""aux-param-colorcorrection7"": ""-0.40332"",
                ""aux-param-colorcorrection8"": ""1.00586"",
                ""aux-param-colorcorrection9"": ""0.921875"",
                ""aux-param-depthclampmax"": ""65535"",
                ""aux-param-depthclampmin"": ""0"",
                ""aux-param-disparitymultiplier"": ""0"",
                ""aux-param-disparityshift"": ""0"",
                ""controls-autoexposure-auto"": ""True"",
                ""controls-autoexposure-manual"": ""8000"",
                ""controls-color-autoexposure-auto"": ""True"",
                ""controls-color-autoexposure-manual"": ""-6"",
                ""controls-color-backlight-compensation"": ""0"",
                ""controls-color-brightness"": ""0"",
                ""controls-color-contrast"": ""50"",
                ""controls-color-gain"": ""64"",
                ""controls-color-gamma"": ""300"",
                ""controls-color-hue"": ""0"",
                ""controls-color-power-line-frequency"": ""3"",
                ""controls-color-saturation"": ""64"",
                ""controls-color-sharpness"": ""50"",
                ""controls-color-white-balance-auto"": ""True"",
                ""controls-color-white-balance-manual"": ""4600"",
                ""controls-depth-gain"": ""16"",
                ""controls-laserpower"": ""150"",
                ""controls-laserstate"": ""on"",
                ""ignoreSAD"": ""0"",
                ""param-autoexposure-setpoint"": ""1700"",
                ""param-censusenablereg-udiameter"": ""9"",
                ""param-censusenablereg-vdiameter"": ""9"",
                ""param-censususize"": ""9"",
                ""param-censusvsize"": ""9"",
                ""param-colorcorrection1"": ""0.129883"",
                ""param-colorcorrection10"": ""-0.441406"",
                ""param-colorcorrection11"": ""-0.441406"",
                ""param-colorcorrection12"": ""-0.0390625"",
                ""param-colorcorrection2"": ""0.399414"",
                ""param-colorcorrection3"": ""0.399414"",
                ""param-colorcorrection4"": ""-0.0693359"",
                ""param-colorcorrection5"": ""-0.198242"",
                ""param-colorcorrection6"": ""-0.40332"",
                ""param-colorcorrection7"": ""-0.40332"",
                ""param-colorcorrection8"": ""1.00586"",
                ""param-colorcorrection9"": ""0.921875"",
                ""param-depthclampmax"": ""65535"",
                ""param-depthclampmin"": ""0"",
                ""param-depthunits"": ""1000"",
                ""param-disableraucolor"": ""0"",
                ""param-disablesadcolor"": ""0"",
                ""param-disablesadnormalize"": ""0"",
                ""param-disablesloleftcolor"": ""0"",
                ""param-disableslorightcolor"": ""0"",
                ""param-disparitymode"": ""0"",
                ""param-disparityshift"": ""0"",
                ""param-lambdaad"": ""2100"",
                ""param-lambdacensus"": ""26"",
                ""param-leftrightthreshold"": ""70"",
                ""param-maxscorethreshb"": ""1023"",
                ""param-medianthreshold"": ""528"",
                ""param-minscorethresha"": ""10"",
                ""param-neighborthresh"": ""20"",
                ""param-raumine"": ""1"",
                ""param-rauminn"": ""1"",
                ""param-rauminnssum"": ""1"",
                ""param-raumins"": ""1"",
                ""param-rauminw"": ""1"",
                ""param-rauminwesum"": ""1"",
                ""param-regioncolorthresholdb"": ""0.586106"",
                ""param-regioncolorthresholdg"": ""0.586106"",
                ""param-regioncolorthresholdr"": ""0.586106"",
                ""param-regionshrinku"": ""1"",
                ""param-regionshrinkv"": ""1"",
                ""param-robbinsmonrodecrement"": ""10"",
                ""param-robbinsmonroincrement"": ""10"",
                ""param-rsmdiffthreshold"": ""7"",
                ""param-rsmrauslodiffthreshold"": ""3"",
                ""param-rsmremovethreshold"": ""0.190476"",
                ""param-scanlineedgetaub"": ""300"",
                ""param-scanlineedgetaug"": ""300"",
                ""param-scanlineedgetaur"": ""300"",
                ""param-scanlinep1"": ""44"",
                ""param-scanlinep1onediscon"": ""23"",
                ""param-scanlinep1twodiscon"": ""219"",
                ""param-scanlinep2"": ""502"",
                ""param-scanlinep2onediscon"": ""237"",
                ""param-scanlinep2twodiscon"": ""113"",
                ""param-secondpeakdelta"": ""27"",
                ""param-texturecountthresh"": ""0"",
                ""param-texturedifferencethresh"": ""0"",
                ""param-usersm"": ""1"",
                ""param-zunits"": ""1000""
            }
        ";

        private const string HAND = @"
            {
                ""aux-param-autoexposure-setpoint"": ""1700"",
                ""aux-param-colorcorrection1"": ""0.129883"",
                ""aux-param-colorcorrection10"": ""-0.441406"",
                ""aux-param-colorcorrection11"": ""-0.441406"",
                ""aux-param-colorcorrection12"": ""-0.0390625"",
                ""aux-param-colorcorrection2"": ""0.399414"",
                ""aux-param-colorcorrection3"": ""0.399414"",
                ""aux-param-colorcorrection4"": ""-0.0693359"",
                ""aux-param-colorcorrection5"": ""-0.198242"",
                ""aux-param-colorcorrection6"": ""-0.40332"",
                ""aux-param-colorcorrection7"": ""-0.40332"",
                ""aux-param-colorcorrection8"": ""1.00586"",
                ""aux-param-colorcorrection9"": ""0.921875"",
                ""aux-param-depthclampmax"": ""65535"",
                ""aux-param-depthclampmin"": ""0"",
                ""aux-param-disparitymultiplier"": ""0"",
                ""aux-param-disparityshift"": ""0"",
                ""controls-autoexposure-auto"": ""True"",
                ""controls-autoexposure-manual"": ""8000"",
                ""controls-color-autoexposure-auto"": ""True"",
                ""controls-color-autoexposure-manual"": ""-6"",
                ""controls-color-backlight-compensation"": ""0"",
                ""controls-color-brightness"": ""0"",
                ""controls-color-contrast"": ""50"",
                ""controls-color-gain"": ""64"",
                ""controls-color-gamma"": ""300"",
                ""controls-color-hue"": ""0"",
                ""controls-color-power-line-frequency"": ""3"",
                ""controls-color-saturation"": ""64"",
                ""controls-color-sharpness"": ""50"",
                ""controls-color-white-balance-auto"": ""True"",
                ""controls-color-white-balance-manual"": ""4600"",
                ""controls-depth-gain"": ""16"",
                ""controls-laserpower"": ""150"",
                ""controls-laserstate"": ""on"",
                ""ignoreSAD"": ""0"",
                ""param-autoexposure-setpoint"": ""1700"",
                ""param-censusenablereg-udiameter"": ""9"",
                ""param-censusenablereg-vdiameter"": ""9"",
                ""param-censususize"": ""9"",
                ""param-censusvsize"": ""9"",
                ""param-colorcorrection1"": ""0.129883"",
                ""param-colorcorrection10"": ""-0.441406"",
                ""param-colorcorrection11"": ""-0.441406"",
                ""param-colorcorrection12"": ""-0.0390625"",
                ""param-colorcorrection2"": ""0.399414"",
                ""param-colorcorrection3"": ""0.399414"",
                ""param-colorcorrection4"": ""-0.0693359"",
                ""param-colorcorrection5"": ""-0.198242"",
                ""param-colorcorrection6"": ""-0.40332"",
                ""param-colorcorrection7"": ""-0.40332"",
                ""param-colorcorrection8"": ""1.00586"",
                ""param-colorcorrection9"": ""0.921875"",
                ""param-depthclampmax"": ""65535"",
                ""param-depthclampmin"": ""0"",
                ""param-depthunits"": ""1000"",
                ""param-disableraucolor"": ""0"",
                ""param-disablesadcolor"": ""0"",
                ""param-disablesadnormalize"": ""0"",
                ""param-disablesloleftcolor"": ""0"",
                ""param-disableslorightcolor"": ""1"",
                ""param-disparitymode"": ""0"",
                ""param-disparityshift"": ""0"",
                ""param-lambdaad"": ""1001"",
                ""param-lambdacensus"": ""7"",
                ""param-leftrightthreshold"": ""20"",
                ""param-maxscorethreshb"": ""791"",
                ""param-medianthreshold"": ""240"",
                ""param-minscorethresha"": ""24"",
                ""param-neighborthresh"": ""110"",
                ""param-raumine"": ""3"",
                ""param-rauminn"": ""1"",
                ""param-rauminnssum"": ""4"",
                ""param-raumins"": ""3"",
                ""param-rauminw"": ""1"",
                ""param-rauminwesum"": ""14"",
                ""param-regioncolorthresholdb"": ""0.0489237"",
                ""param-regioncolorthresholdg"": ""0.072407"",
                ""param-regioncolorthresholdr"": ""0.137965"",
                ""param-regionshrinku"": ""3"",
                ""param-regionshrinkv"": ""1"",
                ""param-robbinsmonrodecrement"": ""20"",
                ""param-robbinsmonroincrement"": ""3"",
                ""param-rsmdiffthreshold"": ""3.8125"",
                ""param-rsmrauslodiffthreshold"": ""0.46875"",
                ""param-rsmremovethreshold"": ""0.553571"",
                ""param-scanlineedgetaub"": ""130"",
                ""param-scanlineedgetaug"": ""244"",
                ""param-scanlineedgetaur"": ""618"",
                ""param-scanlinep1"": ""63"",
                ""param-scanlinep1onediscon"": ""14"",
                ""param-scanlinep1twodiscon"": ""119"",
                ""param-scanlinep2"": ""45"",
                ""param-scanlinep2onediscon"": ""21"",
                ""param-scanlinep2twodiscon"": ""12"",
                ""param-secondpeakdelta"": ""31"",
                ""param-texturecountthresh"": ""0"",
                ""param-texturedifferencethresh"": ""783"",
                ""param-usersm"": ""1"",
                ""param-zunits"": ""1000""
            }
        ";

        private const string HIGH_ACCURACY = @"
            {
                ""aux-param-autoexposure-setpoint"": ""1700"",
                ""aux-param-colorcorrection1"": ""0.129883"",
                ""aux-param-colorcorrection10"": ""-0.441406"",
                ""aux-param-colorcorrection11"": ""-0.441406"",
                ""aux-param-colorcorrection12"": ""-0.0390625"",
                ""aux-param-colorcorrection2"": ""0.399414"",
                ""aux-param-colorcorrection3"": ""0.399414"",
                ""aux-param-colorcorrection4"": ""-0.0693359"",
                ""aux-param-colorcorrection5"": ""-0.198242"",
                ""aux-param-colorcorrection6"": ""-0.40332"",
                ""aux-param-colorcorrection7"": ""-0.40332"",
                ""aux-param-colorcorrection8"": ""1.00586"",
                ""aux-param-colorcorrection9"": ""0.921875"",
                ""aux-param-depthclampmax"": ""65535"",
                ""aux-param-depthclampmin"": ""0"",
                ""aux-param-disparitymultiplier"": ""0"",
                ""aux-param-disparityshift"": ""0"",
                ""controls-autoexposure-auto"": ""True"",
                ""controls-autoexposure-manual"": ""8000"",
                ""controls-color-autoexposure-auto"": ""True"",
                ""controls-color-autoexposure-manual"": ""-6"",
                ""controls-color-backlight-compensation"": ""0"",
                ""controls-color-brightness"": ""0"",
                ""controls-color-contrast"": ""50"",
                ""controls-color-gain"": ""64"",
                ""controls-color-gamma"": ""300"",
                ""controls-color-hue"": ""0"",
                ""controls-color-power-line-frequency"": ""3"",
                ""controls-color-saturation"": ""64"",
                ""controls-color-sharpness"": ""50"",
                ""controls-color-white-balance-auto"": ""True"",
                ""controls-color-white-balance-manual"": ""4600"",
                ""controls-depth-gain"": ""16"",
                ""controls-laserpower"": ""150"",
                ""controls-laserstate"": ""on"",
                ""ignoreSAD"": ""0"",
                ""param-autoexposure-setpoint"": ""1700"",
                ""param-censusenablereg-udiameter"": ""9"",
                ""param-censusenablereg-vdiameter"": ""9"",
                ""param-censususize"": ""9"",
                ""param-censusvsize"": ""9"",
                ""param-colorcorrection1"": ""0.129883"",
                ""param-colorcorrection10"": ""-0.441406"",
                ""param-colorcorrection11"": ""-0.441406"",
                ""param-colorcorrection12"": ""-0.0390625"",
                ""param-colorcorrection2"": ""0.399414"",
                ""param-colorcorrection3"": ""0.399414"",
                ""param-colorcorrection4"": ""-0.0693359"",
                ""param-colorcorrection5"": ""-0.198242"",
                ""param-colorcorrection6"": ""-0.40332"",
                ""param-colorcorrection7"": ""-0.40332"",
                ""param-colorcorrection8"": ""1.00586"",
                ""param-colorcorrection9"": ""0.921875"",
                ""param-depthclampmax"": ""65535"",
                ""param-depthclampmin"": ""0"",
                ""param-depthunits"": ""1000"",
                ""param-disableraucolor"": ""0"",
                ""param-disablesadcolor"": ""0"",
                ""param-disablesadnormalize"": ""0"",
                ""param-disablesloleftcolor"": ""0"",
                ""param-disableslorightcolor"": ""1"",
                ""param-disparitymode"": ""0"",
                ""param-disparityshift"": ""0"",
                ""param-lambdaad"": ""751"",
                ""param-lambdacensus"": ""6"",
                ""param-leftrightthreshold"": ""10"",
                ""param-maxscorethreshb"": ""2893"",
                ""param-medianthreshold"": ""796"",
                ""param-minscorethresha"": ""4"",
                ""param-neighborthresh"": ""108"",
                ""param-raumine"": ""6"",
                ""param-rauminn"": ""3"",
                ""param-rauminnssum"": ""7"",
                ""param-raumins"": ""2"",
                ""param-rauminw"": ""2"",
                ""param-rauminwesum"": ""12"",
                ""param-regioncolorthresholdb"": ""0.785714"",
                ""param-regioncolorthresholdg"": ""0.565558"",
                ""param-regioncolorthresholdr"": ""0.985323"",
                ""param-regionshrinku"": ""3"",
                ""param-regionshrinkv"": ""0"",
                ""param-robbinsmonrodecrement"": ""25"",
                ""param-robbinsmonroincrement"": ""2"",
                ""param-rsmdiffthreshold"": ""1.65625"",
                ""param-rsmrauslodiffthreshold"": ""0.71875"",
                ""param-rsmremovethreshold"": ""0.809524"",
                ""param-scanlineedgetaub"": ""13"",
                ""param-scanlineedgetaug"": ""15"",
                ""param-scanlineedgetaur"": ""30"",
                ""param-scanlinep1"": ""155"",
                ""param-scanlinep1onediscon"": ""160"",
                ""param-scanlinep1twodiscon"": ""59"",
                ""param-scanlinep2"": ""190"",
                ""param-scanlinep2onediscon"": ""507"",
                ""param-scanlinep2twodiscon"": ""493"",
                ""param-secondpeakdelta"": ""647"",
                ""param-texturecountthresh"": ""0"",
                ""param-texturedifferencethresh"": ""1722"",
                ""param-usersm"": ""1"",
                ""param-zunits"": ""1000""
            }
        ";

        private const string HIGH_DENSITY = @"
            {
                ""aux-param-autoexposure-setpoint"": ""1700"",
                ""aux-param-colorcorrection1"": ""0.129883"",
                ""aux-param-colorcorrection10"": ""-0.441406"",
                ""aux-param-colorcorrection11"": ""-0.441406"",
                ""aux-param-colorcorrection12"": ""-0.0390625"",
                ""aux-param-colorcorrection2"": ""0.399414"",
                ""aux-param-colorcorrection3"": ""0.399414"",
                ""aux-param-colorcorrection4"": ""-0.0693359"",
                ""aux-param-colorcorrection5"": ""-0.198242"",
                ""aux-param-colorcorrection6"": ""-0.40332"",
                ""aux-param-colorcorrection7"": ""-0.40332"",
                ""aux-param-colorcorrection8"": ""1.00586"",
                ""aux-param-colorcorrection9"": ""0.921875"",
                ""aux-param-depthclampmax"": ""65535"",
                ""aux-param-depthclampmin"": ""0"",
                ""aux-param-disparitymultiplier"": ""0"",
                ""aux-param-disparityshift"": ""0"",
                ""controls-autoexposure-auto"": ""True"",
                ""controls-autoexposure-manual"": ""8000"",
                ""controls-color-autoexposure-auto"": ""True"",
                ""controls-color-autoexposure-manual"": ""-6"",
                ""controls-color-backlight-compensation"": ""0"",
                ""controls-color-brightness"": ""0"",
                ""controls-color-contrast"": ""50"",
                ""controls-color-gain"": ""64"",
                ""controls-color-gamma"": ""300"",
                ""controls-color-hue"": ""0"",
                ""controls-color-power-line-frequency"": ""3"",
                ""controls-color-saturation"": ""64"",
                ""controls-color-sharpness"": ""50"",
                ""controls-color-white-balance-auto"": ""True"",
                ""controls-color-white-balance-manual"": ""4600"",
                ""controls-depth-gain"": ""16"",
                ""controls-laserpower"": ""150"",
                ""controls-laserstate"": ""on"",
                ""ignoreSAD"": ""0"",
                ""param-autoexposure-setpoint"": ""1700"",
                ""param-censusenablereg-udiameter"": ""9"",
                ""param-censusenablereg-vdiameter"": ""9"",
                ""param-censususize"": ""9"",
                ""param-censusvsize"": ""9"",
                ""param-colorcorrection1"": ""0.129883"",
                ""param-colorcorrection10"": ""-0.441406"",
                ""param-colorcorrection11"": ""-0.441406"",
                ""param-colorcorrection12"": ""-0.0390625"",
                ""param-colorcorrection2"": ""0.399414"",
                ""param-colorcorrection3"": ""0.399414"",
                ""param-colorcorrection4"": ""-0.0693359"",
                ""param-colorcorrection5"": ""-0.198242"",
                ""param-colorcorrection6"": ""-0.40332"",
                ""param-colorcorrection7"": ""-0.40332"",
                ""param-colorcorrection8"": ""1.00586"",
                ""param-colorcorrection9"": ""0.921875"",
                ""param-depthclampmax"": ""65535"",
                ""param-depthclampmin"": ""0"",
                ""param-depthunits"": ""1000"",
                ""param-disableraucolor"": ""0"",
                ""param-disablesadcolor"": ""0"",
                ""param-disablesadnormalize"": ""0"",
                ""param-disablesloleftcolor"": ""0"",
                ""param-disableslorightcolor"": ""0"",
                ""param-disparitymode"": ""0"",
                ""param-disparityshift"": ""0"",
                ""param-lambdaad"": ""618"",
                ""param-lambdacensus"": ""15"",
                ""param-leftrightthreshold"": ""18"",
                ""param-maxscorethreshb"": ""1443"",
                ""param-medianthreshold"": ""789"",
                ""param-minscorethresha"": ""96"",
                ""param-neighborthresh"": ""12"",
                ""param-raumine"": ""2"",
                ""param-rauminn"": ""1"",
                ""param-rauminnssum"": ""6"",
                ""param-raumins"": ""3"",
                ""param-rauminw"": ""3"",
                ""param-rauminwesum"": ""7"",
                ""param-regioncolorthresholdb"": ""0.109589"",
                ""param-regioncolorthresholdg"": ""0.572407"",
                ""param-regioncolorthresholdr"": ""0.0176125"",
                ""param-regionshrinku"": ""4"",
                ""param-regionshrinkv"": ""0"",
                ""param-robbinsmonrodecrement"": ""6"",
                ""param-robbinsmonroincrement"": ""21"",
                ""param-rsmdiffthreshold"": ""1.21875"",
                ""param-rsmrauslodiffthreshold"": ""0.28125"",
                ""param-rsmremovethreshold"": ""0.488095"",
                ""param-scanlineedgetaub"": ""8"",
                ""param-scanlineedgetaug"": ""200"",
                ""param-scanlineedgetaur"": ""279"",
                ""param-scanlinep1"": ""55"",
                ""param-scanlinep1onediscon"": ""326"",
                ""param-scanlinep1twodiscon"": ""134"",
                ""param-scanlinep2"": ""235"",
                ""param-scanlinep2onediscon"": ""506"",
                ""param-scanlinep2twodiscon"": ""206"",
                ""param-secondpeakdelta"": ""222"",
                ""param-texturecountthresh"": ""0"",
                ""param-texturedifferencethresh"": ""2466"",
                ""param-usersm"": ""1"",
                ""param-zunits"": ""1000""
            }
        ";

        private const string MEDIUM_DENSITY = @"
            {
                ""aux-param-autoexposure-setpoint"": ""1700"",
                ""aux-param-colorcorrection1"": ""0.129883"",
                ""aux-param-colorcorrection10"": ""-0.441406"",
                ""aux-param-colorcorrection11"": ""-0.441406"",
                ""aux-param-colorcorrection12"": ""-0.0390625"",
                ""aux-param-colorcorrection2"": ""0.399414"",
                ""aux-param-colorcorrection3"": ""0.399414"",
                ""aux-param-colorcorrection4"": ""-0.0693359"",
                ""aux-param-colorcorrection5"": ""-0.198242"",
                ""aux-param-colorcorrection6"": ""-0.40332"",
                ""aux-param-colorcorrection7"": ""-0.40332"",
                ""aux-param-colorcorrection8"": ""1.00586"",
                ""aux-param-colorcorrection9"": ""0.921875"",
                ""aux-param-depthclampmax"": ""65535"",
                ""aux-param-depthclampmin"": ""0"",
                ""aux-param-disparitymultiplier"": ""0"",
                ""aux-param-disparityshift"": ""0"",
                ""controls-autoexposure-auto"": ""True"",
                ""controls-autoexposure-manual"": ""8000"",
                ""controls-color-autoexposure-auto"": ""True"",
                ""controls-color-autoexposure-manual"": ""-6"",
                ""controls-color-backlight-compensation"": ""0"",
                ""controls-color-brightness"": ""0"",
                ""controls-color-contrast"": ""50"",
                ""controls-color-gain"": ""64"",
                ""controls-color-gamma"": ""300"",
                ""controls-color-hue"": ""0"",
                ""controls-color-power-line-frequency"": ""3"",
                ""controls-color-saturation"": ""64"",
                ""controls-color-sharpness"": ""50"",
                ""controls-color-white-balance-auto"": ""True"",
                ""controls-color-white-balance-manual"": ""4600"",
                ""controls-depth-gain"": ""16"",
                ""controls-laserpower"": ""150"",
                ""controls-laserstate"": ""on"",
                ""ignoreSAD"": ""0"",
                ""param-autoexposure-setpoint"": ""1700"",
                ""param-censusenablereg-udiameter"": ""9"",
                ""param-censusenablereg-vdiameter"": ""9"",
                ""param-censususize"": ""9"",
                ""param-censusvsize"": ""9"",
                ""param-colorcorrection1"": ""0.129883"",
                ""param-colorcorrection10"": ""-0.441406"",
                ""param-colorcorrection11"": ""-0.441406"",
                ""param-colorcorrection12"": ""-0.0390625"",
                ""param-colorcorrection2"": ""0.399414"",
                ""param-colorcorrection3"": ""0.399414"",
                ""param-colorcorrection4"": ""-0.0693359"",
                ""param-colorcorrection5"": ""-0.198242"",
                ""param-colorcorrection6"": ""-0.40332"",
                ""param-colorcorrection7"": ""-0.40332"",
                ""param-colorcorrection8"": ""1.00586"",
                ""param-colorcorrection9"": ""0.921875"",
                ""param-depthclampmax"": ""65535"",
                ""param-depthclampmin"": ""0"",
                ""param-depthunits"": ""1000"",
                ""param-disableraucolor"": ""0"",
                ""param-disablesadcolor"": ""0"",
                ""param-disablesadnormalize"": ""0"",
                ""param-disablesloleftcolor"": ""0"",
                ""param-disableslorightcolor"": ""1"",
                ""param-disparitymode"": ""0"",
                ""param-disparityshift"": ""0"",
                ""param-lambdaad"": ""935"",
                ""param-lambdacensus"": ""26"",
                ""param-leftrightthreshold"": ""19"",
                ""param-maxscorethreshb"": ""887"",
                ""param-medianthreshold"": ""1021"",
                ""param-minscorethresha"": ""54"",
                ""param-neighborthresh"": ""97"",
                ""param-raumine"": ""3"",
                ""param-rauminn"": ""1"",
                ""param-rauminnssum"": ""6"",
                ""param-raumins"": ""3"",
                ""param-rauminw"": ""5"",
                ""param-rauminwesum"": ""11"",
                ""param-regioncolorthresholdb"": ""0.0136986"",
                ""param-regioncolorthresholdg"": ""0.707436"",
                ""param-regioncolorthresholdr"": ""0.181996"",
                ""param-regionshrinku"": ""3"",
                ""param-regionshrinkv"": ""1"",
                ""param-robbinsmonrodecrement"": ""23"",
                ""param-robbinsmonroincrement"": ""3"",
                ""param-rsmdiffthreshold"": ""1.8125"",
                ""param-rsmrauslodiffthreshold"": ""1"",
                ""param-rsmremovethreshold"": ""0.482143"",
                ""param-scanlineedgetaub"": ""16"",
                ""param-scanlineedgetaug"": ""259"",
                ""param-scanlineedgetaur"": ""896"",
                ""param-scanlinep1"": ""132"",
                ""param-scanlinep1onediscon"": ""77"",
                ""param-scanlinep1twodiscon"": ""234"",
                ""param-scanlinep2"": ""342"",
                ""param-scanlinep2onediscon"": ""390"",
                ""param-scanlinep2twodiscon"": ""151"",
                ""param-secondpeakdelta"": ""600"",
                ""param-texturecountthresh"": ""0"",
                ""param-texturedifferencethresh"": ""0"",
                ""param-usersm"": ""1"",
                ""param-zunits"": ""1000""
            }
        ";
}
}