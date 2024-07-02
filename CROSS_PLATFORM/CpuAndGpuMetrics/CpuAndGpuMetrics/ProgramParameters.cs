using Hardware.Info;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.CodeDom;
using System;
using System.Collections.Generic;
using OfficeOpenXml.Core.ExcelPackage;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Contains all necessary path locations. 
    /// </summary>
    public static class FilePath
    {
        /// <summary>Relative path of the test source folder.</summary>
        public readonly static string TESTSOURCESPATH = @"../OfficialSources";

        /// <summary>Absolute path of ffmpeg.exe on Linux OS.</summary>
        public readonly static string LINUX_FFMPEGPATH = @"/usr/bin/ffmpeg";

        /// <summary>Absolute path of ffmpeg.exe on Windows OS.</summary>
        public readonly static string WINDOWS_FFMPEGPATH = "ffmpeg";

        /// <summary>
        /// SPECIFY PATH WHERE YOU WOULD LIKE THE EXCEL FILES TO BE DUMPED
        /// </summary>
        public readonly static string EXCELDIRECTORY = @"../";
    }

    /// <summary>
    /// Contains all information about the current setup.
    /// </summary>
    public static class ProgramSettings
    {
        /// <summary>
        /// Current OS of the machine. Default is Windows
        /// </summary>
        public readonly static OS CURRENT_OS;

        /// <summary>
        /// GPU TYPE/BRAND of current system
        /// </summary>
        public static GpuType GPU;

        public static List<string> GPU_LIST = new List<string>();

        /// <summary>
        /// EXCEL FILE NAME
        /// </summary>
        /// 
        public readonly static string EXCEL_FILE_NAME;

        /// <summary>
        /// To grab the correct column when running linux awk cmd
        /// </summary>
        public readonly static string? LINUX_AWK_CMD;

        /// <summary>
        /// Indicate whether hardware decode is on or not
        /// </summary>
        public static bool IS_DECODE_ONLY_ON { get; set; }

        /// <summary>
        /// Indicate whether Software decode/encode is on or not
        /// </summary>
        public static bool IS_SOFTWARE_ONLY_ON { get; set; }

        /// <summary>
        /// Indicate whether the source is a raw video file or not
        /// </summary>
        public static bool IS_RAW_SOURCE { get; set; }

        /// <summary>
        /// Indicate whether the source is a raw video file or not
        /// </summary>
        public static bool IS_SPEEDHQ { get; set; }

        /// <summary>
        /// Contains hardware information of the current PC
        /// </summary>
        private static IHardwareInfo hardwareInfo = new HardwareInfo();

        /// <summary>
        /// Default constructor for ProgramSettings
        /// </summary>
        static ProgramSettings()
        {
            CURRENT_OS = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OS.Linux : OS.Windows;
            GetGPUInfo();
            EXCEL_FILE_NAME = $"AutomatedData_{GPU}.xlsx";
            if (CURRENT_OS == OS.Linux) LINUX_AWK_CMD = GetIntelGpuTopHeader();
        }

        /// <summary>
        /// Get GPU type from system.
        /// </summary>
        /// <returns>GPU value.</returns>
        private static void GetGPUInfo()
        {
            if (CURRENT_OS == OS.Linux)
            {
                // Creating a new process to launch command on the terminal
                using Process process = new();
                string command = "lspci | grep VGA | cut -f 2- -d ' '";
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{command}\"";
                // Console.WriteLine(process.StartInfo.Arguments);
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                // int i = 0;
                string[] lines = output.TrimEnd('\n').Split('\n');
                foreach (var temp in lines)
                {
                    var gpu_full_name = temp.Split(':')[1];
                    // Console.WriteLine($"{i}." + gpu_full_name);
                    GPU_LIST.Add(gpu_full_name);
                    // i++;
                }
            }
            else
            {
                // Use hardwareInfo library on Windows
                hardwareInfo.RefreshVideoControllerList();
                foreach (var gpu in hardwareInfo.VideoControllerList)
                {
                    GPU_LIST.Add(gpu.Name);
                }
            }
            // Set GPU default to 0
            setGPUInfo(0);
        }

        /// <summary>
        /// Set GPU info function.
        /// </summary>
        /// <param name="index">Index of GPU we want to use/set.</param>
        public static void setGPUInfo(int index)
        {
            // Determine GPU type in current system
            string manufacturer = GPU_LIST[index].ToUpper();
            if (manufacturer.Contains("NVIDIA"))
            {
                GPU = GpuType.Nvidia;
            }
            else if (manufacturer.Contains("INTEL") || manufacturer.Contains("MATROX"))
            {
                GPU = GpuType.Intel;
            }
            else if (manufacturer.Contains("VGA"))
            {
                GPU = GpuType.Intel;
            }
        }

        /// <summary>
        /// Environment variables needed to run Vulkan and VAAPI in Linux
        /// </summary>
        public static void ExportParamLinux()
        {
            using (Process process = new Process())
            {
                string command = "export LIBVA_DRIVER_NAME=iHD && export ANV_VIDEO_DECODE=1 && echo";
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{command}\"";
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                // Set the process env. variables
                Environment.SetEnvironmentVariable("LIBVA_DRIVER_NAME", "iHD");
                Environment.SetEnvironmentVariable("ANV_VIDEO_DECODE", "1");

                // DEBUG:


                process.Start();
                process.WaitForExit();
            }
        }

        /// <summary>
        /// Get CPU headers when running CPU get metrics
        /// </summary>
        /// <returns>Command to get right CPU metrics from column.</returns>
        public static string GetIntelGpuTopHeader()
        {
            using Process process = new();
            string password = "matrox";
            float timeout = 1.0f;
            string command = $"echo {password} | sudo -S timeout {timeout} intel_gpu_top -o - | head -n 2";

            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"{command}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;

            process.Start();
            string[] resultLines = process.StandardOutput.ReadToEnd().Trim().Split("\n");
            process.WaitForExit();

            if (resultLines.Length != 2)
            {
                throw new ArgumentException("-ProgramSettings.cs: NOT ENOUGH LINES TO DETERMINE LINUX AWK CMD");
            }

            string[] headerLineTokens = System.Text.RegularExpressions.Regex.Split(resultLines[0].Trim(), @"\s+");
            string[] varLineTokens = System.Text.RegularExpressions.Regex.Split(resultLines[1].Trim(), @"\s+");

            int headerCounter = 0, varCounter = 0;
            bool isFirstCol = true;
            string awkCmd = "'{print ";

            while (headerCounter < headerLineTokens.Length)
            {
                if (varCounter >= varLineTokens.Length) break;

                //Debug
                // Console.WriteLine(varCounter + varLineTokens[varCounter]);
                // Console.WriteLine(headerCounter + headerLineTokens[headerCounter]);

                if (varLineTokens[varCounter] == "req" || varLineTokens[varCounter] == "act" || varLineTokens[varCounter] == "/s"
                || varLineTokens[varCounter] == "gpu" || varLineTokens[varCounter] == "pkg")
                {
                    varCounter++;
                    headerCounter++;
                }
                else if (varLineTokens[varCounter] == "%")
                {
                    if (headerLineTokens[headerCounter] == "RCS/0" || headerLineTokens[headerCounter] == "VCS/0" || headerLineTokens[headerCounter] == "VCS/1")
                    {
                        if (!isFirstCol)
                        {
                            awkCmd += ",";
                        }
                        else isFirstCol = false;

                        awkCmd += $"${varCounter + 1}";
                    }
                    varCounter++;
                    headerCounter++;
                }
                else
                {
                    varCounter++;
                }
            }
            awkCmd += "}'";

            //DEBUG
            Console.WriteLine("-ProgramSettings.cs: " + awkCmd);

            return awkCmd;
        }
    }

    /// <summary>
    /// Possible Operating Systems.
    /// </summary>
    public enum OS
    {
        Unknown = 0,
        Windows = 1,
        Linux = 2,
    }
}
