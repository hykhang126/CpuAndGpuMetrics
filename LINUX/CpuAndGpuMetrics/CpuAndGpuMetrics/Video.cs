

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Contains relevant information about a video file.
    /// </summary>
    public class Video
    {
        /// <summary>The codec (H264, H265).</summary>
        private Codec codec;

        /// <summary>The chroma subsampling of the video (420, 444).</summary>
        private Chroma chroma;

        /// <summary>The resolution of the video (HD, UHD).</summary>
        private Resolution resolution;

        /// <summary>The bitdepth of the video (8 bit, 10 bit).</summary>
        private BitDepth bitDepth;

        /// <summary>
        /// Gets or sets the codec.
        /// </summary>
        public Codec CodecExt 
        {
            get { return codec; }
            set {  codec = value; }
        }

        /// <summary>
        /// Gets or sets the chroma.
        /// </summary>
        public Chroma ChromaExt 
        {
            get { return chroma; } 
            set {  chroma = value; } 
        }

        /// <summary>
        /// Gets or sets the resolution. 
        /// </summary>
        public Resolution ResolutionExt 
        { 
            get { return resolution; } 
            set {  resolution = value; } 
        }

        /// <summary>
        /// Gets or sets the bit depth. 
        /// </summary>
        public BitDepth BitDepthExt 
        { 
            get { return bitDepth; }
            set {  bitDepth = value; }
        }

        /// <summary>
        /// Instantiates a Video object.
        /// </summary>
        /// <param name="codec">The codec of the video.</param>
        /// <param name="chroma">The chroma subsampling of the video.</param>
        /// <param name="resolution">The resolution of the video.</param>
        /// <param name="bitDepth">The bit depth of the video.</param>
        public Video(Codec codec, Chroma chroma, Resolution resolution, BitDepth bitDepth)
        {
            this.codec = codec;
            this.chroma = chroma;
            this.resolution = resolution;
            this.bitDepth = bitDepth;
        }

        /// <summary>
        /// Converts a filename into a Video object.
        /// </summary>
        /// <param name="filename">Filename of video following a naming convention.</param>
        /// <returns>Video object.</returns>
        public static Video FilenameToVideo(string filename)
        {
            Codec codec;
            Chroma chroma;
            Resolution resolution;
            BitDepth bitDepth;

            if (filename.Contains("H264") || filename.Contains("h264") || filename.Contains("libx264") || filename.Contains("x264") )
            {
                codec = Codec.H264;
            }
            else if (filename.Contains("H265") || filename.Contains("h265") || filename.Contains("hevc") || filename.Contains("x265"))
            { 
                codec= Codec.H265;
            }
            else
            {
                codec = Codec.Unknown;
            }

            if (filename.Contains("420"))
            {
                chroma = Chroma.Subsampling_420;
            }
            else if (filename.Contains("444"))
            {
                chroma = Chroma.Subsampling_444;
            }
            else
            {
                 chroma = Chroma.Unknown;
            }

            if (filename.Contains("UHD") || filename.Contains("4k") || filename.Contains("4K"))
            {
                resolution = Resolution.UHD;
            }
            else if (filename.Contains("HD") || filename.Contains("hd"))
            {
                resolution = Resolution.HD;
            }
            else 
            { 
                resolution = Resolution.Unknown; 
            }

            if (filename.Contains("8bit") || filename.Contains("b08"))
            {
                bitDepth = BitDepth.Bit_8;
            }
            else if (filename.Contains("10bit") || filename.Contains("b10"))
            {  
                bitDepth = BitDepth.Bit_10; 
            }
            else
            {
                bitDepth = BitDepth.Unknown;
            }

            return new Video(codec, chroma, resolution, bitDepth);
        }

        /// <summary>
        /// Possible Codecs
        /// </summary>
        public enum Codec
        {
            Unknown = 0,
            H264 = 1,
            H265 = 2,
        }

        /// <summary>
        /// Possible chroma subsamplings
        /// </summary>
        public enum Chroma
        {
            Unknown = 0,
            Subsampling_420 = 1,
            Subsampling_444 = 2,
        }

        /// <summary>
        /// Possible bit depths
        /// </summary>
        public enum BitDepth
        {
            Unknown = 0,
            Bit_8 = 1,
            Bit_10 = 2,
        }

        /// <summary>
        /// Possible resolutions.
        /// </summary>
        public enum Resolution
        {
            Unknown = 0,
            HD = 1,
            UHD = 2,
        }

    }
}
