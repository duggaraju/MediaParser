//*****************************************************************************
//
// Microsoft Windows Azure Media Encoder MP4 Object Model
// Copyright © Microsoft Corporation. All rights reserved.
//
//*****************************************************************************

using System;

namespace Microsoft.Cloud.Media.Common.Fmp4.ObjectModel
{
    /// <summary>
    /// This class is used to report errors which have occurred during MP4 deserialization.
    /// </summary>
    public class MP4DeserializeException : Exception
    {
        /// <summary>
        /// Creates an MP4DeserializeException.
        /// 
        /// Example error message:
        ///
        /// "Error deserializing 'tfra' at offset 512: Could not read Version and Flags for FullBox,
        /// only 3 bytes left in reported size, expected 4"
        /// 
        /// </summary>
        /// <param name="boxTypeDescription">The description of the box type which could not be
        /// deserialized, eg. 'tfra'.</param>
        /// <param name="relativeOffset">The offset to add to initialOffset to produce the final
        /// offset. Can be negative. In general, the expected value here is the value which will
        /// produce the offset to the start of the current box. In other words, if an error occurred
        /// while deserializing the version/flags for FullBox, the offset we report should point
        /// to the start of the current Box, ie. should point to the MSB of the Box.Size field.</param>
        /// <param name="initialOffset">Typically the initialOffset of the stream currently being
        /// read from. This is simply added to relativeOffset to make a single number.</param>
        /// <param name="message">The error message.</param>
        public MP4DeserializeException(string boxTypeDescription,
                                       long relativeOffset,
                                       long initialOffset,
                                       String message)
            : base(String.Format("Error deserializing {0} at offset {1}: {2}",
                 boxTypeDescription, initialOffset + relativeOffset, message))
        {
        }

        /// <summary>
        /// Creates an MP4DeserializeException.
        /// 
        /// Example error message:
        ///
        /// "Error deserializing 'tfra' at offset 512: Could not read Version and Flags for FullBox,
        /// only 3 bytes left in reported size, expected 4"
        /// 
        /// </summary>
        /// <param name="boxTypeDescription">The description of the box type which could not be
        /// deserialized, eg. 'tfra'.</param>
        /// <param name="relativeOffset">The offset to add to initialOffset to produce the final
        /// offset. Can be negative. In general, the expected value here is the value which will
        /// produce the offset to the start of the current box. In other words, if an error occurred
        /// while deserializing the version/flags for FullBox, the offset we report should point
        /// to the start of the current Box, ie. should point to the MSB of the Box.Size field.</param>
        /// <param name="initialOffset">Typically the initialOffset of the stream currently being
        /// read from. This is simply added to relativeOffset to make a single number.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The exception which caused this MP4DeserializeException.</param>
        public MP4DeserializeException(string boxTypeDescription,
                                       long relativeOffset,
                                       long initialOffset,
                                       String message,
                                       Exception innerException)
            : base(String.Format("Error deserializing {0} at offset {1}: {2}",
                 boxTypeDescription, initialOffset + relativeOffset, message, innerException))
        {
        }
    }
}
