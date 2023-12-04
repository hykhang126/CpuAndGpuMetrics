

using System.Reflection.Metadata;

namespace CpuAndGpuMetrics
{
    public abstract class FilePath
    {
        /// <summary>Relative path of the test source folder.</summary>
        public readonly static string TESTSOURCESPATH = @"OfficialSources";

        /// <summary>Absolute path of ffmpeg.exe.</summary>
        public readonly static string FFMPEGPATH = "/usr/bin/ffmpeg";

        //SPECIFY PATH WHERE YOU WOULD LIKE THE EXCEL FILES TO BE DUMPED
        public readonly static string EXCELDIRECTORY = @"./";

        public readonly static GpuType GPU = GpuType.Nvidia;

        public readonly static string FILE_NAME = $"AutomatedData_{GPU}.xlsx";

        public readonly static bool IS_DECODE_ACCEL_ON = true;
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