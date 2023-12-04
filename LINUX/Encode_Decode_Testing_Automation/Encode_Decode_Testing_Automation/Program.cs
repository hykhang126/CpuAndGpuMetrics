﻿using System.Net;
using CpuAndGpuMetrics;
using static CpuAndGpuMetrics.FFmpegProcess;
using static CpuAndGpuMetrics.Video;

/// <summary>
/// Class for the main entry point of the program
/// </summary>
class Program
{
    // Set the path of the sources
    readonly private static string TESTSOURCESPATH = FilePath.TESTSOURCESPATH;

    // SPECIFY PATH WHERE YOU WOULD LIKE THE EXCEL FILES TO BE DUMPED
    readonly private static string EXCELDIRECTORY = FilePath.EXCELDIRECTORY;

    // Set Gpu type (Placeholder) and type of hwaccels based on that gpu
    // NEED TO FIND A WAY TO AUTO DETECT GPU / OR AT LEAST MANUALLY INPUT ; ADD CODE AT "GpuType.cs"
    private static GpuType gpu = FilePath.GPU;

    /// <summary>
    /// Indicate whether hardware decode is on or not
    /// </summary>
    private static bool isDecodeAccel = FilePath.IS_DECODE_ACCEL_ON;

    /// <summary>
    /// Numbering the amount of test runs being carried out
    /// </summary>
    private static int testNbr = 1;

    // Combining file name and path
    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    private static string fileName = $"AutomatedData_{gpu}.xlsx";

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private static string filePath = Path.Combine(EXCELDIRECTORY, fileName);

    static void ExecuteAutomatedTest()
    {
        // Choose

        // Set Sources path
        string[] fileNames = Directory.GetFiles(TESTSOURCESPATH);

        // Create a List of Tuples which store the Video info., it's performance and accel type
        List<Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator>> videoPerfData = [];

        // Declare the excel writer
        ExcelWriter excelWriter;

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
                // read from cmd line
                HardwareAccelerator hwaccel = new(hardwareAccel, gpu, isDecodeAccel);

                FFmpegProcess ffmpegProcess = FilenameToFFmpegProcess(filename, video, hwaccel);
                
                // Start the ffmpeg process
                var p = ffmpegProcess.StartProcess(HardwareAccelerator.IsDecodeAccel);

                // Start data collection
                if (p != null)
                {
                    Thread.Sleep(2000); // variable 1-2 secs (experiment)

                    container.PopulateData(gpu);
                    Console.WriteLine("Data Populated.");

                    Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator> tuple = new(video, container, hwaccel);
                    videoPerfData.Add(tuple);
                }

                // Start gathering fps value using process stderr
                float fps = -1;
                while ( p!=null && !p.StandardError.EndOfStream)
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

                // End process if not already ended
                if (p != null && !p.HasExited)
                {
                    p.Kill();
                }

                // Write to Excel
                excelWriter = (HardwareAccelerator.IsDecodeAccel) ? new ExcelWriterDecodeOnly(testNbr) : new ExcelWriterEncodeOnly(testNbr);
                excelWriter.DataListToExcel(videoPerfData, filePath);



                // DEBUG
                container.DisplayValues();
                //isDecodeAccel = !isDecodeAccel;
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
        bool continueTest = true;
        do 
        {
            // Insert Input to continue program
            Console.WriteLine(" Continue program? (true or false)");
            continueTest = Convert.ToBoolean(Console.ReadLine());
            if (!continueTest) return;

            // Then choose hwaccel on or off
            Console.WriteLine(" DecodeAccel on or off? (on or off)");
            string? decodeAccelIn = Console.ReadLine();
            Program.isDecodeAccel = (decodeAccelIn == "on") ? true : false;

            ExecuteAutomatedTest();
        }
        while (continueTest);
    }
}