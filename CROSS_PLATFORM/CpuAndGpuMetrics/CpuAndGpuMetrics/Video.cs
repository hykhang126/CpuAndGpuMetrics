

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
            String lowercaseFilename = filename.ToLower();
            Codec codec = getCodec(lowercaseFilename);
            Chroma chroma = getChroma(lowercaseFilename);
            Resolution resolution = getResolution(lowercaseFilename);
            BitDepth bitDepth = getBitDepth(lowercaseFilename);

            return new Video(codec, chroma, resolution, bitDepth);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="substrings"></param>
        /// <returns></returns>
        private static bool containsAny(string filename, string[] substrings)
        {
            return substrings.Any(substring => filename.Contains(substring));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static Codec getCodec(string filename)
        {
            string[] h264indicators = { "h264", "libx264", "x264" };
            string[] h265indicators = { "h265", "hvec", "x265" };


            if (containsAny(filename, h264indicators)) {
                return Codec.H264;
            }
            else if (containsAny(filename, h265indicators))
            {
                return Codec.H265;
            }

            return Codec.Unknown;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static Chroma getChroma(string filename)
        {
            string[] chroma420 = { "420" };
            string[] chroma444 = { "444" };


            if (containsAny(filename, chroma420))
            {
                return Chroma.Subsampling_420;
            }
            else if (containsAny(filename, chroma444))
            {
                return Chroma.Subsampling_420;
            }

            return Chroma.Unknown;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static Resolution getResolution(string filename)
        {
            string[] uhdindicators = { "uhd", "4k" };
            string[] hdindicators = { "hd" };


            if (containsAny(filename, uhdindicators))
            {
                return Resolution.UHD;
            }
            else if (containsAny(filename, hdindicators))
            {
                return Resolution.HD;
            }

            return Resolution.Unknown;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static BitDepth getBitDepth(string filename)
        {
            string[] b8indicators = { "8bit", "b08" };
            string[] b10indicators = { "10bit", "b10" };


            if (containsAny(filename, b8indicators))
            {
                return BitDepth.Bit_8;
            }
            else if (containsAny(filename, b10indicators))
            {
                return BitDepth.Bit_10;
            }

            return BitDepth.Unknown;
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
