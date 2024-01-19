

using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace CpuAndGpuMetrics
{
    public static class FilePath
    {
        /// <summary>Relative path of the test source folder.</summary>
        public readonly static string TESTSOURCESPATH = @"../../../../../OfficialSources";

        /// <summary>Absolute path of ffmpeg.exe on Linux OS.</summary>
        public readonly static string LINUX_FFMPEGPATH = " / usr/bin/ffmpeg";

        /// <summary>Absolute path of ffmpeg.exe on Windows OS.</summary>
        public readonly static string WINDOWS_FFMPEGPATH = "C:\\Users\\jjiang\\Downloads\\ffmpeg-master-latest-win64-gpl\\ffmpeg-master-latest-win64-gpl\\bin\\ffmpeg";

        /// <summary>
        /// SPECIFY PATH WHERE YOU WOULD LIKE THE EXCEL FILES TO BE DUMPED
        /// </summary>
        public readonly static string EXCELDIRECTORY = @"../../../../../";
    }

    public static class ProgramSettings
    {
        static ProgramSettings()
        {
            CURRENT_OS = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OS.Linux : OS.Windows;
            GPU = GpuType.Nvidia;
        }

        /// <summary>
        /// HARD-CODED GPU TYPE
        /// </summary>
        public readonly static GpuType GPU;

        /// <summary>
        /// EXCEL FILE NAME
        /// </summary>
        public readonly static string EXCEL_FILE_NAME = $"AutomatedData_{GPU}.xlsx";

        /// <summary>
        /// DEFAULT OPTION FOR HW ACCEL
        /// </summary>
        public readonly static bool DEFAULT_IS_DECODE_ACCEL_ON = true;

        /// <summary>
        /// Current OS of the machine. Default is Windows
        /// </summary>
        public readonly static OS CURRENT_OS;
    }

    public enum OS
    {
        Unknown = 0,
        Windows = 1,
        Linux = 2,
    }

    public enum State
    {
        Transcode = 0,
        Decode = 1,
        Encode = 2,
    }
}