using System.Diagnostics;
using System.Drawing.Imaging;
using static CpuAndGpuMetrics.Video;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Handles FFmpeg processes.
    /// </summary>
    public class FFmpegProcess
    {
        /// <summary>Relative path of the test source folder.</summary>
        private readonly string TESTSOURCESPATH = FilePath.TESTSOURCESPATH;

        /// <summary>Absolute path of ffmpeg.exe.</summary>
        /// If statement for linux switch
        private readonly string FFMPEGPATH = (ProgramSettings.CURRENT_OS == OS.Windows) ? FilePath.WINDOWS_FFMPEGPATH : FilePath.LINUX_FFMPEGPATH;

        /// <summary>Boolean indicating of process should be skipped.</summary>
        private readonly bool skip = false;

        /// <summary>
        /// Hardware acceleration used for the command.
        /// </summary>
        private readonly HardwareAccel hardwareAccel;

        /// <summary>
        /// Filename and location.
        /// </summary>
        private readonly string filename;

        /// <summary>
        /// Initializes a FFmpegProcess object.
        /// </summary>
        /// <param name="filename">Filename of video.</param>
        /// <param name="hardwareAccel">Hardware Acceleration method chosen.</param>
        /// <param name="skip">Boolean statement if the current file should be skipped over.</param>
        public FFmpegProcess(string filename, HardwareAccel hardwareAccel, bool skip)
        {
            this.hardwareAccel = hardwareAccel;
            this.filename = filename;
            this.skip = skip;
        }

        /// <summary>
        /// Creates FFmpegProcess object based on filename, video formats and hardware acceleration.
        /// </summary>
        /// <param name="filename">Filename of the video.</param>
        /// <param name="video">Video format/details container.</param>
        /// <param name="hardwareAccelerator">Hardware Acceleration method.</param>
        /// <returns>ffmpeg process with the necessary information to create cmd.</returns>
        /// <exception cref="ArgumentNullException">Invalid filename format or empty filename.</exception>
        /// <exception cref="ArgumentException">GPU type unspecified.</exception>
        public static FFmpegProcess FilenameToFFmpegProcess(string filename, Video video, HardwareAccelerator hardwareAccelerator)
        {
            // Ignore invalid format or empty files.
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException(nameof(filename) + " is Null or Empty!");
            }
            if (filename.Contains("README") || filename.Contains("out") || filename.Contains(".mp4"))
            {
                return new FFmpegProcess(filename, HardwareAccel.Unknown, true);
            }

            // Hardware acceleration compability with GPU brands
            HardwareAccel hwaccel;
            GpuType gpuType = hardwareAccelerator.Gpu;
            bool skip = false;
            
            if (gpuType == GpuType.Nvidia)
            {
                switch (hardwareAccelerator.HardwareAccel)
                {
                    // Supported Hardware Accelerations
                    case HardwareAccel.None:
                    case HardwareAccel.Cuda:
                    case HardwareAccel.VDPAU:
                    case HardwareAccel.D3D11VA:
                    case HardwareAccel.Vulkan:
                    case HardwareAccel.D3D12VA:
                        hwaccel = hardwareAccelerator.HardwareAccel;
                        break;

                    // Unsupported hardware accelerations
                    case HardwareAccel.VAAPI:
                    case HardwareAccel.QSV:
                        hwaccel = HardwareAccel.None;

                        skip = true;
                        Console.WriteLine("\nhwaccel and GPU type is incompatible!");
                        break;

                    default:
                        hwaccel = HardwareAccel.Unknown;
                        break;
                }
            }
            else if (gpuType == GpuType.Intel)
            {
                switch (hardwareAccelerator.HardwareAccel)
                {
                    // Supported hardware accelerations
                    case HardwareAccel.None:
                    case HardwareAccel.QSV:
                    case HardwareAccel.D3D11VA:
                    case HardwareAccel.VAAPI:
                    case HardwareAccel.Vulkan:
                    case HardwareAccel.D3D12VA:
                        hwaccel = hardwareAccelerator.HardwareAccel;         
                        break;

                    // Unsupported hardware accelerations
                    case HardwareAccel.Cuda:
                    case HardwareAccel.VDPAU:
                        hwaccel = HardwareAccel.None;
                        skip = true;
                        Console.WriteLine("\nhwaccel and GPU type is incompatible!");
                        break;

                    default:
                        hwaccel = HardwareAccel.Unknown;
                        break;
                }
            }
            else
            {
                throw new ArgumentException("GPU type not specified.");
            }

            // ADD CRITERIAS FOR VIDEOS' SPECS. THAT SHOULD BE SKIPPED HERE:
            // h264 && yuv444 for both 8bit and 10bit
            if (video.CodecExt == Video.Codec.H264 && video.ChromaExt == Video.Chroma.YUV_444)
            {
                skip = true;
            }

            return new FFmpegProcess(filename, hwaccel, skip);
        }

        /// <summary>
        /// Command arguments to use with ffmpeg decoding (depends on hardware acceleration).
        /// </summary>
        /// <param name="gpuIndex">Chosen GPU index.</param>
        /// <returns>String containing the command arguments.</returns>
        public string? GenerateDecodeFFmpegCmd(int gpuIndex, Video video)
        {
            string? cmd;
            // get the resolution of the video
            string resolution = (video.ResolutionExt == Resolution.UHD) ? "3840x2160" : "1920x1080";
            // get the pixel format arg 
            string pix_fmt = ExtractPixFmtFromFile(video);

            // Set the extra raw source settings
            string rawSource_extra = (ProgramSettings.IS_RAW_SOURCE) ? $"-stream_loop 990 -pix_fmt {pix_fmt} -s {resolution}" : "";

            switch (hardwareAccel)
            {
                // Examples:
                // ffmpeg -hide_banner -v verbose -hwaccel cuda -hwaccel_device 0 -hwaccel_output_format cuda -i TestSources/i.mp4 -f null -
                // ffmpeg -hide_banner -v verbose -hwaccel vaapi -hwaccel_device /dev/dri/card0 -hwaccel_output_format vaapi -i TestSources/i.mp4 - f null -
                // ffmpeg -v verbose -hide_banner -init_hw_device "vulkan=vk:0" -hwaccel vulkan -hwaccel_output_format vulkan -i TestSources/h264/420/HD.mp4 -f null -
                // ffmpeg -hide_banner -verbose -i TestSources/HD.mp4 -f null -
                // ffmpeg -hide_banner -v verbose -hwaccel cuda -hwaccel_device 0 -hwaccel_output_format cuda -i TestSources/h264/420/HD.mp4 -f null -

                case HardwareAccel.Cuda:

                case HardwareAccel.VDPAU:
                    cmd = 
                        $" -hide_banner -hwaccel {this.hardwareAccel.ToString().ToLower()} -hwaccel_device {gpuIndex}" +
                        $" -hwaccel_output_format {this.hardwareAccel.ToString().ToLower()} -i {this.filename} -f null -";
                    break;

                case HardwareAccel.QSV:
                    if (ProgramSettings.CURRENT_OS == OS.Linux)
                    {
                        cmd =
                           $" -hide_banner -hwaccel {this.hardwareAccel.ToString().ToLower()} -qsv_device /dev/dri/card{gpuIndex}" +
                           $" -hwaccel_output_format {this.hardwareAccel.ToString().ToLower()} -i {this.filename} -f null -";
                    }
                    else
                    {
                        cmd =
                           $" -hide_banner -hwaccel {this.hardwareAccel.ToString().ToLower()} -qsv_device {gpuIndex}" +
                           $" -hwaccel_output_format {this.hardwareAccel.ToString().ToLower()} -i {this.filename} -f null -";
                    }
                    break;

                case HardwareAccel.VAAPI:
                    cmd = 
                        $" -hide_banner -hwaccel {this.hardwareAccel.ToString().ToLower()} -hwaccel_device /dev/dri/card{gpuIndex}" +
                        $" -hwaccel_output_format {this.hardwareAccel.ToString().ToLower()} -i {this.filename} -f null -";
                    break;

                case HardwareAccel.D3D11VA:
                    cmd =
                        $" -hide_banner -hwaccel {this.hardwareAccel.ToString().ToLower()} -hwaccel_device {gpuIndex}" +
                        $" -hwaccel_output_format d3d11 -i {this.filename} -f null -";
                    break;

                case HardwareAccel.D3D12VA:
                    cmd =
                        $" -hide_banner -hwaccel {this.hardwareAccel.ToString().ToLower()} -hwaccel_device {gpuIndex}" +
                        $" -hwaccel_output_format d3d12 -i {this.filename} -f null -";
                    break;

                case HardwareAccel.Vulkan:
                    cmd =
                        $" -hide_banner -init_hw_device \"vulkan=vk:{gpuIndex}\" " +
                        $" -hwaccel {this.hardwareAccel.ToString().ToLower()}" +
                        $" -hwaccel_output_format vulkan -i {this.filename} -f null -";
                    break;

                case HardwareAccel.None:
                default:
                    cmd = (ProgramSettings.IS_SPEEDHQ) ? $" -hide_banner {rawSource_extra} -i {this.filename} -vcodec speedhq -y -an -f null -"
                        : $" -hide_banner {rawSource_extra} -i {this.filename} -f null -";
                    break;
            }
            // Debug cmd
            Console.WriteLine($"\n {cmd} \n");

            return cmd;
        }

        /// <summary>
        /// Command arguments to use with ffmpeg encoding (depends on hardware acceleration).
        /// </summary>
        /// <returns>String containing the command arguments.</returns>
        public string? GenerateEncodeFFmpegCmd(int gpuIndex, int outputIndex, Video video)
        {
            string? cmd;
            string codec;
            float time = 20f;
            string lowerfilename = filename.ToLower();
            // get the resolution of the video
            string resolution = (video.ResolutionExt == Resolution.UHD) ? "3840x2160" : "1920x1080";
            // get the pixel format arg 
            string pix_fmt = ExtractPixFmtFromFile(video);

            // Set the extra raw source settings
            string rawSource_extra = (ProgramSettings.IS_RAW_SOURCE) ? $"-stream_loop 99 -pix_fmt {pix_fmt} -s {resolution}" : "";


            // Set the codec
            if (lowerfilename.Contains("h265") || lowerfilename.Contains("hevc") || lowerfilename.Contains("x265"))
            {
                codec = "hevc";
            }
            else
            {
                codec = "h264";
            }



            // A dictionary to map the bitrate values
            Dictionary<string, int> bitrateDictionary = new Dictionary<string, int>()
            {
                {"10mbps", 10 },
                {"15mbps", 15 },
                {"20mbps", 20 },
                {"30mbps", 30 },

            };
            //extract bitrate from file name
            int bitrate = ExtractBitRateFromFile(lowerfilename, bitrateDictionary);
            if (bitrate == 0)
            {
                Console.WriteLine("There is no bitrate found in file name");
                return null;
            }
            // Setting the bitrate based on the extracted values from the source video
            string bitrateExtracted = $"{bitrate}M";

            int renderVaapiIndex = 128 + gpuIndex;

            // Choose the correct cmd
            switch (hardwareAccel)
            {
                // Examples:
                // ffmpeg -hide_banner -v verbose -i TestSources/h264/420/HD.mp4 -c:v h264 -t 60 output.mp4 -y
                // ffmpeg -hide_banner -v verbose -i TestSources/h264/420/HD.mp4 -c:v h264_nvenc -t 60 output.mp4 -y
                // ffmpeg -hide_banner -v verbose -i TestSources/h264/420/HD.mp4 -c:v h264_qsv -t 60 output.mp4 -y
                // ffmpeg -hide_banner -v verbose -i TestSources/h264/420/HD.mp4 -c:v h264_vaapi -t 60 output.mp4 -y

                case HardwareAccel.Cuda:
                case HardwareAccel.VDPAU:
                    cmd = $"-y -hide_banner -i {this.filename} -hwaccel_device {gpuIndex}-c:v {codec}_nvenc -b:v {bitrateExtracted} -t {time} out{outputIndex}.mp4";
                    break;

                case HardwareAccel.QSV:
                    if (ProgramSettings.CURRENT_OS == OS.Linux)
                    {
                        cmd = $"-y -hide_banner -i {this.filename} -qsv_device /dev/dri/card{gpuIndex} -c:v {codec}_qsv -b:v {bitrateExtracted} -t {time} out{outputIndex}.mp4";
                    }
                    else
                    {
                        cmd = $"-y -hide_banner -i {this.filename} -qsv_device {gpuIndex} -c:v {codec}_qsv -b:v {bitrateExtracted} -t {time} out{outputIndex}.mp4";
                    }
                    break;

                case HardwareAccel.VAAPI:
                    cmd = $"-y -hide_banner -vaapi_device /dev/dri/renderD{renderVaapiIndex} -i {this.filename} -c:v {codec}_vaapi -vf format=nv12,hwupload -b:v {bitrateExtracted} -t {time} out{outputIndex}.mp4";
                    break;

                //case HardwareAccel.D3D11VA:
                //case HardwareAccel.D3D12VA:
                //case HardwareAccel.Vulkan:
                case HardwareAccel.None:
                default:
                    cmd = $"-y -hide_banner {rawSource_extra} -i {this.filename} -c:v {codec} -b:v {bitrateExtracted} -t {time} out{outputIndex}.mp4";
                    break;
            }
            // Debug cmd
            Console.WriteLine($"\n {cmd} \n");

            return cmd;
        }


        private int ExtractBitRateFromFile(string filename, Dictionary<string, int> bitrateDictionary)
        {
            foreach (var keyValueMatch in bitrateDictionary)
            {
                if (filename.Contains(keyValueMatch.Key))
                {
                    return keyValueMatch.Value;
                }
            }
            //if the bitrate is not found then return 0
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="video"></param>
        /// <returns></returns>
        private string ExtractPixFmtFromFile(Video video)
        {
            string pix_fmt = "yuv420p";

            if (video.ChromaExt == Video.Chroma.YUV_422)
            {
                pix_fmt = "yuv422p";
            }
            else if (video.ChromaExt == Video.Chroma.YUV_444)
            {
                pix_fmt = "yuv444p";
            }

            if (video.BitDepthExt == BitDepth.Bit_10)
            {
                pix_fmt += "10le";
            }

            return pix_fmt;
        }

        /// <summary>
        /// Check if video should be skipped.
        /// </summary>
        /// <returns>Boolean value for if the video should be skipped.</returns>
        private bool IsVideoSkipped()
        {
            // Check if this video should be skipped. If not then generate ffmpeg cmd.
            if (skip == true || hardwareAccel == HardwareAccel.Unknown)
            {
                Console.WriteLine("\n" + filename);
                Console.WriteLine("This Video is Skipped");
            }
            return skip;
        }

        /// <summary>
        /// Starts the FFmpeg process with the configured settings.
        /// </summary>
        /// <param name="isHardwareAccel">Check if process is decode or encode.</param>
        /// <param name="gpuIndex">Chosen GPU index.</param>
        /// <returns>The started process or null if the video format is skipped.</returns>
        public Process? StartProcess(bool isHardwareAccel, int gpuIndex, int outputIndex, Video video)
        {
            // Check if the video is skipped
            if (IsVideoSkipped()) return null;

            // Get Cmd
            string? cmd = (isHardwareAccel) ? GenerateDecodeFFmpegCmd(gpuIndex, video) : GenerateEncodeFFmpegCmd(gpuIndex, outputIndex, video);

            // Start a new process and set its parameters. Then returns the process to its caller
            // FFMPEG output is cache on stderr not stdout
            Process p = new();
            string workingDir = TESTSOURCESPATH;

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.WorkingDirectory = p.StartInfo.WorkingDirectory + workingDir;
            p.StartInfo.FileName = FFMPEGPATH; // NEED TO AUTO DETECT / MANUAL INPUT THIS TOO
            p.StartInfo.Arguments = $"{cmd}";

            p.Start();

            return p;
        }   
    }
}
