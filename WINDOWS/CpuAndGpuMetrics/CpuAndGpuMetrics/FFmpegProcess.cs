﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Handles FFmpeg processes.
    /// </summary>
    public class FFmpegProcess
    {
        /// <summary>Relative path of the test source folder.</summary>
        private readonly static string TESTSOURCESPATH = @"..\..\..\OfficialSources";

        /// <summary>Absolute path of ffmpeg.exe.</summary>
        private readonly static string FFMPEGPATH = "C:\\Users\\tester\\Downloads\\ffmpeg-master-latest-win64-gpl\\ffmpeg-master-latest-win64-gpl\\bin\\ffmpeg.exe";

        /// <summary>Filename of video to be decoded.</summary>
        private readonly string filename;

        /// <summary>Hardware accelerator mode.</summary>
        private readonly HardwareAccel hardwareAccel;

        /// <summary>Boolean indicating of process should be skipped.</summary>
        private readonly bool skip = false;

        /// <summary>
        /// Initializes a FFmpegProcess object.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="hardwareAccel"></param>
        /// <param name="skip"></param>
        public FFmpegProcess(string filename, HardwareAccel hardwareAccel, bool skip)
        {
            this.hardwareAccel = hardwareAccel;
            this.filename = filename;
            this.skip = skip;
        }

        /// <summary>
        /// Creates an FFmpeg process based on the filename, video details, GPU type, and hardware acceleration type.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="video"></param>
        /// <param name="gpuType"></param>
        /// <param name="hardwareAccel"></param>
        /// <returns>A new instance of FFmpegProcess/</returns>
        public static FFmpegProcess FilenameToFFmpegProcess(string filename, Video video, GpuType gpuType, HardwareAccel hardwareAccel)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException(nameof(filename) + " is Null or Empty!");
            }
            if (filename.Contains("README"))
            {
                return new FFmpegProcess(filename, HardwareAccel.Unknown, true);
            }

            HardwareAccel hwaccel;
            bool skip = false;
            
            if (gpuType == GpuType.Nvidia)
            {
                switch (hardwareAccel)
                {
                    case HardwareAccel.None:
                    case HardwareAccel.Cuda:
                    case HardwareAccel.D3D11VA:
                    case HardwareAccel.Vulkan:
                        hwaccel = hardwareAccel;
                        break;

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
                switch (hardwareAccel)
                {
                    case HardwareAccel.None:
                    case HardwareAccel.QSV:
                    case HardwareAccel.D3D11VA:
                    case HardwareAccel.Vulkan:
                        hwaccel = hardwareAccel;
                        break;

                    case HardwareAccel.Cuda:
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
            if (video.CodecExt == Video.Codec.H264 && video.ChromaExt == Video.Chroma.Subsampling_444)
            {
                skip = true;
            }

            return new FFmpegProcess(filename, hwaccel, skip);
        }

        /// <summary>
        /// Starts the FFmpeg process with the configured settings.
        /// </summary>
        /// <returns>The started process or null if the video format is skipped.</returns>
        public Process? StartProcess()
        {
            // Check if this video should be skipped. If not then generate ffmnpeg cmd.
            if (skip == true || hardwareAccel == HardwareAccel.Unknown)
            {
                Console.WriteLine("\n" + filename);
                Console.WriteLine("This Video Format is Skipped");
                return null;
            }
            else
            {
                string cmd;
                switch (hardwareAccel)
                {
                    // ffmpeg -hide_banner -v verbose -hwaccel cuda -hwaccel_device 0 -hwaccel_output_format cuda -i TestSources/i.mp4 -f null -
                    case HardwareAccel.Cuda:
                    // ffmpeg -hide_banner -v verbose -hwaccel qsv -hwaccel_device /dev/dri/card1 -hwaccel_output_format qsv -i TestSources/i.mp4 -f null -
                    case HardwareAccel.QSV:
                    // ffmpeg -hide_banner -v verbose -hwaccel vaapi -hwaccel_device /dev/dri/card0 -hwaccel_output_format vaapi -i TestSources/i.mp4 - f null -
                    case HardwareAccel.VAAPI:
                        cmd = 
                            $" -hide_banner -hwaccel {this.hardwareAccel.ToString().ToLower()} -hwaccel_device 0" +
                            $" -hwaccel_output_format {this.hardwareAccel.ToString().ToLower()} -i {this.filename} -f null -";
                        break;

                    // ffmpeg -hide_banner -v verbose -hwaccel d3d11va -hwaccel_device 0 -hwaccel_output_format d3d11 -i TestSources/i.mp4 -f null -
                    case HardwareAccel.D3D11VA:
                        cmd =
                            $" -hide_banner -hwaccel {this.hardwareAccel.ToString().ToLower()} -hwaccel_device 0" +
                            $" -hwaccel_output_format d3d11 -i {this.filename} -f null -";
                        break;

                    // ffmpeg -v verbose -hide_banner -init_hw_device "vulkan=vk:0" -hwaccel vulkan -hwaccel_output_format vulkan -i TestSources/h264/420/HD.mp4 -f null -
                    case HardwareAccel.Vulkan:
                        cmd =
                            $" -hide_banner -init_hw_device \"vulkan=vk:0\" " +
                            $" -hwaccel {this.hardwareAccel.ToString().ToLower()}" +
                            $" -hwaccel_output_format vulkan -i {this.filename} -f null -"; //changed d3d11 to vulkan
                        break;

                    // ffmpeg -hide_banner -verbose -i TestSources/HD.mp4 -f null -
                    case HardwareAccel.None:
                    default:
                        cmd = $" -hide_banner -i {this.filename} -f null -";
                        break;
                }

                // Debug cmd
                Console.WriteLine($"\n {cmd} \n");

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
}