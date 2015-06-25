//*****************************************************************************
//
// Microsoft Windows Azure Media Encoder MP4 Object Model
// Copyright © Microsoft Corporation. All rights reserved.
//
//*****************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Cloud.Media.Common.Fmp4.ObjectModel
{
    /// <summary>
    /// This class stores the known box types (both compact boxtype and extended),
    /// and keeps a lookup table of the corresponding object type which understands that
    /// box type.
    /// </summary>
    public static class MP4BoxType
    {
        //===================================================================
        // Known Box Types (Compact)
        //===================================================================
        public const UInt32 uuid = 0x75756964; // 'uuid'
        public const UInt32 mfro = 0x6D66726F; // 'mfro'
        public const UInt32 tfra = 0x74667261; // 'tfra'
        public const UInt32 mfra = 0x6D667261; // 'mfra'
        public const UInt32 moof = 0x6D6F6F66; // 'moof'
        public const UInt32 mfhd = 0x6D666864; // 'mfhd'
        public const UInt32 traf = 0x74726166; // 'traf'
        public const UInt32 tfhd = 0x74666864; // 'tfhd'
        public const UInt32 trun = 0x7472756E; // 'trun'
        public const UInt32 sdtp = 0x73647470; // 'sdtp'
        public const UInt32 mdat = 0x6D646174; // 'mdat'
        public const UInt32 moov = 0x6D6F6F76; // 'moov'
        public const UInt32 mvhd = 0x6D766864; // 'mvhd'
        public const UInt32 trak = 0x7472616B; // 'trak'
        public const UInt32 tkhd = 0x746B6864; // 'tkhd'
        public const UInt32 mvex = 0x6D766578; // 'mvex'
        public const UInt32 trex = 0x74726578; // 'trex'
        public const UInt32 mdia = 0x6D646961; // 'mdia'
        public const UInt32 hdlr = 0x68646C72; // 'hdlr'
        public const UInt32 mdhd = 0x6D646864; // 'mdhd'


        //===================================================================
        // Known Box Types (Extended 'uuid')
        //===================================================================


        //===================================================================
        // Registered Box Objects (Compact Type)
        //===================================================================
        public static Dictionary<UInt32,Type> CompactType = new Dictionary<UInt32,Type>()
        {
            { mfro, typeof(mfroBox) },
            { tfra, typeof(tfraBox) },
            { mfra, typeof(mfraBox) },
            { moof, typeof(moofBox) },
            { mfhd, typeof(mfhdBox) },
            { traf, typeof(trafBox) },
            { tfhd, typeof(tfhdBox) },
            { trun, typeof(trunBox) },
            { sdtp, typeof(sdtpBox) },
            { moov, typeof(moovBox) },
            { mvhd, typeof(mvhdBox) },
            { trak, typeof(trakBox) },
            { tkhd, typeof(tkhdBox) },
            { mvex, typeof(mvexBox) },
            { trex, typeof(trexBox) },
            { mdia, typeof(mdiaBox) },
            { hdlr, typeof(hdlrBox) },
            { mdhd, typeof(mdhdBox) },
        };

        //===================================================================
        // Registered Box Objects (Extended 'uuid' Type)
        //===================================================================
        public static Dictionary<Guid, Type> ExtendedType = new Dictionary<Guid, Type>()
        {
        };

        /// <summary>
        /// Helper function to convert a string type to its integer value.
        /// </summary>
        /// <param name="type">The string representation of the type of an mp4 box e.g: "mfra"</param>
        /// <returns></returns>
        public static UInt32 StringToCompactType(string type)
        {
            if (type.Length != 4)
            {
                throw new ArgumentException(
                    String.Format("Invalid box type {0}. Box type must have only 4 characters", type));
            }
            //combine the ASCII values of each character to form the 32 bit type.
            UInt32 value = type.Aggregate<char, UInt32>(0, (w, c) => ((w << 8) + (byte)c));
            return value;
        }



    }

}
