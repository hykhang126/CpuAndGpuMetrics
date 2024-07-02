using CpuAndGpuMetrics;
using OfficeOpenXml;
using System.Diagnostics;
using static CpuAndGpuMetrics.FFmpegProcess;
using static CpuAndGpuMetrics.Video;

/// <summary>
/// Class for the main entry point of the program
/// </summary>
class Program
{
    /// <summary>
    /// Set the path of the sources
    /// </summary>
    readonly private static string TESTSOURCESPATH = FilePath.TESTSOURCESPATH;

    /// <summary>
    /// SPECIFY PATH WHERE YOU WOULD LIKE THE EXCEL FILES TO BE DUMPED
    /// </summary>
    readonly private static string EXCELDIRECTORY = FilePath.EXCELDIRECTORY;

    /// <summary>
    /// Name of the excel file
    /// </summary>
    /// <value></value>
    readonly private static string EXCEL_FILE_NAME = ProgramSettings.EXCEL_FILE_NAME;

    /// <summary>
    /// Selected GPU index
    /// </summary>
    private static int gpuSelectedIndex = 0;

    /// <summary>
    /// Numbering the amount of test types
    /// </summary>
    private static int testNbr = 1;

    // Object for locking
    private static readonly object lockObject = new object();

    // number of parallel streams
    private static int parallelStreams = 1;

    /// <summary>
    /// Gather FPS data from a ffmpeg process
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    private static float GetFPSFromFFMPEGProcess(Process p)
    {
        // Start gathering fps value using process stderr
        float fps = -1;
        while (p != null && !p.StandardError.EndOfStream)
        {
            string? line = p.StandardError.ReadLine();
            //Console.WriteLine(line);
            if (line != null && line.ToLower().Contains("fps"))
            {
                int fpsIndex = line.ToLower().IndexOf("fps");

                string fpsString = line.ToLower()[(fpsIndex + 4)..];

                if (fpsString.Contains('q'))
                {
                    fpsString = fpsString[..(fpsString.IndexOf("q") - 1)].Trim();
                    fps = float.TryParse(fpsString, out float f) ? float.Parse(fpsString) : fps;
                }
            }
        }
        return fps;
    }

    static void ExecuteAutomatedTest()
    {
        // Set Sources path
        string[] fileNames = Directory.GetFiles(TESTSOURCESPATH);

        // Create a List of Tuples which store the Video info., it's performance and accel type
        List<Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator>> videoPerfData = [];

        // Declare the excel writer
        ExcelWriter excelWriter;

        // Convert all file names in source folder to an array of string tokens
        for (int i = 0; i < fileNames.Length; i++)
        {
            fileNames[i] = Path.GetFileName(fileNames[i]);
        }

        // Loop through all sources files with all the hardware accelerator
        foreach (HardwareAccel hardwareAccel in HardwareAccelerator.HardwareAcceleratorChooser(ProgramSettings.GPU))
        {

            foreach (string filename in fileNames)
            {
                // Create a video object that stores all video info
                Video video = FilenameToVideo(filename);

                // Create the hardware accelerator class containing info on hwaccel type, gpu type and whether hwaccel is on or off
                HardwareAccelerator hwaccel = new(hardwareAccel, ProgramSettings.GPU, ProgramSettings.IS_DECODE_ONLY_ON);

                // Create ffmpegProgress to generate ffmpeg command based on video and hwaccel settings 
                FFmpegProcess ffmpegProcess = FilenameToFFmpegProcess(filename, video, hwaccel);

                // Performance metrics container list
                List<PerformanceMetricsContainer> peakContainers = [];

                // List of final FPS from each ffmpeg parallel process
                List<float> finalFPSs = new List<float>();

                // Start the ffmpeg process
                List<Task> tasksList = new List<Task>();
                int j = parallelStreams;
                while (j > 0)
                {
                    int parallelHolder = j;
                    tasksList.Add(Task.Run(async () =>
                    {
                        var p = ffmpegProcess.StartProcess(HardwareAccelerator.IsDecodeAccel, gpuSelectedIndex, parallelHolder, video);

                        // Start data collection
                        if (p != null)
                        {
                            Console.WriteLine("-Program.cs Task Sleeping");
                            Thread.Sleep(1000);

                            // Start gathering fps value using process stderr
                            float fps = GetFPSFromFFMPEGProcess(p);

                            lock (lockObject)
                            {
                                finalFPSs.Add(fps);
                            }
                        }

                        // End process if not already ended
                        if (p != null && !p.HasExited)
                        {
                            p.Kill();
                        }


                        /* DEBUG
                        container.DisplayValues();
                        Console.WriteLine("-------------------------------------------------");
                        Console.WriteLine("Thread is terminating soon");
                        Console.WriteLine("-------------------------------------------------");
                        */
                    }));
                    j--;
                }
                // Task.WaitAll(tasksList.ToArray());

                while (!Task.WhenAll(tasksList.ToArray()).IsCompleted)
                {
                    PerformanceMetricsContainer container = new();
                    container.PopulateData(ProgramSettings.GPU);
                    peakContainers.Add(container);
                    // container.DisplayValues();
                }
                PerformanceMetricsContainer peakContainer = new PerformanceMetricsContainer();

                if (peakContainers.Count != 0) {
                    peakContainer = new(peakContainers.OrderByDescending(o => (hwaccel.HardwareAccel == HardwareAccel.None) ? o.CpuUsage :
                        o.Gpu3D + o.GpuCopy + o.VideoDecode0 + o.VideoDecode1 + (o.VideoDecode2 ?? 0) + (o.VideoEncode ?? 0))
                        .First());
                }

                peakContainer.MinFramesPerSecond = peakContainer.MaxFramesPerSecond = peakContainer.AverageFramesPerSecond = 0;
                if (finalFPSs.Count > 0) {
                    peakContainer.MaxFramesPerSecond = finalFPSs.Max();
                    peakContainer.MinFramesPerSecond = finalFPSs.Min();
                    peakContainer.AverageFramesPerSecond = finalFPSs.Average();

                }
                Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator> tuple = new(video, peakContainer, hwaccel);
                videoPerfData.Add(tuple);

                // DEBUG
                //peakContainer.DisplayValues();

                Console.WriteLine("-Program.cs Main Thread Sleeping");
                Thread.Sleep(2000);

            }
        }
        excelWriter = (ProgramSettings.IS_DECODE_ONLY_ON) ? new ExcelWriterDecodeOnly(testNbr) : new ExcelWriterEncodeOnly(testNbr);
        string EXCEL_FILE_PATH = Path.Combine(EXCELDIRECTORY, GetTimestamp(DateTime.Now) + "_" + EXCEL_FILE_NAME);
        excelWriter.DataListToExcel(videoPerfData, EXCEL_FILE_PATH, parallelStreams, gpuSelectedIndex);

        // Increment the counter for test type's numbering
        testNbr++;
    }

    /// <summary>
    /// Main entry point of the Automation program
    /// </summary>
    static void Main()
    {
        // Output current OS information & set environment var. when neccessary
        Console.WriteLine("---Current OS: " + ProgramSettings.CURRENT_OS.ToString());
        if (ProgramSettings.CURRENT_OS.ToString() == "Linux")
        {
            Console.WriteLine("-- Setting some environment varibles for Linux --");
            ProgramSettings.ExportParamLinux();
        }

        // Determine GPU infomation and selection
        int gpuCount = ProgramSettings.GPU_LIST.Count;
        Console.WriteLine("Number of GPU is: " + gpuCount);
        Console.WriteLine("Please select between the following GPUS (first GPU is default)");
        for (int i = 0; i < gpuCount; i++)
        {
            Console.WriteLine($"{i}. GPU name is: " + ProgramSettings.GPU_LIST[i]);
        }
        if (!int.TryParse(Console.ReadLine(), out int gpuIndex) || gpuIndex < 0 || gpuIndex >= gpuCount)
        {
            Console.WriteLine("Invalid Input detected, selecting default GPU.");
        }
        else
        {
            gpuSelectedIndex = gpuIndex;
            ProgramSettings.setGPUInfo(gpuSelectedIndex);
        }
        Console.WriteLine("Selected GPU is " + ProgramSettings.GPU_LIST[gpuSelectedIndex]);
        Console.WriteLine("If selected gpu is wrong, please restart the program!");     
        
        // Main program loop
        bool continueTest = true;
        do
        {
            Console.WriteLine("---Current GPU type: " + ProgramSettings.GPU.ToString());

            // Insert Input to continue program
            Console.WriteLine(" Continue program? (true (default) or false)");
            try
            {
                continueTest = Convert.ToBoolean(Console.ReadLine());
                if (!continueTest) return;
            }
            catch {
                Console.WriteLine("INVALID CHOICE DEFAULT TO TRUE");
                continueTest = true;
            }

            // Is the type of sources raw?
            Console.WriteLine(" Is the type of sources raw? (on or off(default))");
            string? source_raw = Console.ReadLine();
            ProgramSettings.IS_RAW_SOURCE = (source_raw == "on");

            // Then choose whether it's speedhq test on or off
            Console.WriteLine(" SpeedHQ on or off? (on or off(default))");
            string? speedhq = Console.ReadLine();
            ProgramSettings.IS_SPEEDHQ = (speedhq == "on");

            // Then choose decode only on or off
            Console.WriteLine(" DecodeOnly on or off? (on (default) or off)");
            string? decodeAccelIn = Console.ReadLine();
            ProgramSettings.IS_DECODE_ONLY_ON = !(decodeAccelIn == "off");

            // software only decode
            if (!(decodeAccelIn == "off"))
            {
                Console.WriteLine(" Software only Decode on or off? (on (default) or off)");
                string? Software_only = Console.ReadLine();
                ProgramSettings.IS_SOFTWARE_ONLY_ON = !(Software_only == "off");
            }
            // software only encode
            else if (decodeAccelIn == "off")
            {
                Console.WriteLine(" Software only Encode on or off? (on (default) or off)");
                string? Software_only = Console.ReadLine();
                ProgramSettings.IS_SOFTWARE_ONLY_ON = !(Software_only == "off");
            }

            //if((Software_only == "off"))
            //{
            //    ProgramSettings.IS_SOFTWARE_ONLY_ON = false;
            //}
            //else if(!(Software_only == "off"))
            //{
            //    ProgramSettings.IS_SOFTWARE_ONLY_ON = true;
            //}

            // Choose number of streams to run parallel
            Console.WriteLine(" Choose number of streams to run in parallel. (1 (default), 2, 4)");
            if (!int.TryParse(Console.ReadLine(), out int parallelInputStreams) || parallelInputStreams <= 0 && parallelInputStreams != 2 && parallelInputStreams != 4)
            {
                Console.WriteLine("Invalid Input detected, selecting default parallel stream count.");
            }
            else parallelStreams = parallelInputStreams;

            ExecuteAutomatedTest();
        }
        while (continueTest);
    }

    /// <summary>
    /// Format the timestamp for file name
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static String GetTimestamp(DateTime value)
    {
        return value.ToString("yyyyMMddHHmmssffff");
    }
}
