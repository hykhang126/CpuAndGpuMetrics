using CpuAndGpuMetrics;
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
    /// Path to the excel file
    /// </summary>
    /// <returns></returns>
    readonly private static string EXCEL_FILE_PATH = Path.Combine(EXCELDIRECTORY, EXCEL_FILE_NAME);

    // Set Gpu type (Placeholder) and type of hwaccels based on that gpu
    // NEED TO FIND A WAY TO AUTO DETECT GPU / OR AT LEAST MANUALLY INPUT ; ADD CODE AT "GpuType.cs"
    private static GpuType gpu = ProgramSettings.GPU;

    /// <summary>
    /// Numbering the amount of test types
    /// </summary>
    private static int testNbr = 1;

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
        foreach (var hardwareAccel in HardwareAccelerator.HardwareAcceleratorChooser(gpu))
        {
            foreach (var filename in fileNames)
            {
                Video video = FilenameToVideo(filename);

                PerformanceMetricsContainer container = new();

                // Create the hardware accelerator class containing info on hwaccel type, gpu type and whether hwaccel is on or off
                HardwareAccelerator hwaccel = new(hardwareAccel, gpu, ProgramSettings.IS_DECODE_ONLY_ON);

                FFmpegProcess ffmpegProcess = FilenameToFFmpegProcess(filename, video, hwaccel);
                
                // Start the ffmpeg process
                var p = ffmpegProcess.StartProcess(HardwareAccelerator.IsDecodeAccel);

                // Start data collection
                if (p != null)
                {
                    Console.WriteLine("...Sleeping in Program.cs...");
                    Thread.Sleep(3000);
                    Console.WriteLine("Finish sleeping! \n");

                    container.PopulateData(gpu);
                    Console.WriteLine("Data Populated.");

                    Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator> tuple = new(video, container, hwaccel);
                    videoPerfData.Add(tuple);

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
                    container.FramesPerSecond = fps;

                    // Write to Excel
                    excelWriter = (HardwareAccelerator.IsDecodeAccel) ? new ExcelWriterDecodeOnly(testNbr) : new ExcelWriterEncodeOnly(testNbr);
                    excelWriter.DataListToExcel(videoPerfData, EXCEL_FILE_PATH);
                }

                // End process if not already ended
                if (p != null && !p.HasExited)
                {
                    p.Kill();
                }

                // DEBUG
                container.DisplayValues();
            }
        }
        // Increment the counter for test type's numbering
        testNbr++;
    }

    /// <summary>
    /// Main entry point of the Automation program
    /// </summary>
    static void Main()
    {
        bool continueTest;

        do 
        {
            // Output current OS
            Console.WriteLine("---Current OS: " + ProgramSettings.CURRENT_OS.ToString());
            // Insert Input to continue program
            Console.WriteLine(" Continue program? (true or false)");
            continueTest = Convert.ToBoolean(Console.ReadLine());
            if (!continueTest) return;

            // Then choose hwaccel on or off
            Console.WriteLine(" DecodeOnly on or off? (on or off)");
            string? decodeAccelIn = Console.ReadLine();
            ProgramSettings.IS_DECODE_ONLY_ON = (decodeAccelIn == "on");

            ExecuteAutomatedTest();
        }
        while (continueTest);
    }
}