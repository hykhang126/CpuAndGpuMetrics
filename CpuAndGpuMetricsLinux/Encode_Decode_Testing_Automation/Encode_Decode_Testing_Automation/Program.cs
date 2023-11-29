﻿using CpuAndGpuMetrics;
using OfficeOpenXml;
//main namespace for EPPlus library (allows us to manipulate the excel file)
using System.Xml.Schema;
using static CpuAndGpuMetrics.FFmpegProcess;
using static CpuAndGpuMetrics.Video;
using System;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Drawing;

/// <summary>
/// Class for the main entry point of the program
/// </summary>
class Program
{
    /// <summary>
    /// Setting the path
    /// </summary>
    readonly private static string TESTSOURCESPATH = @"OfficialSources";
    //SPECIFY PATH WHERE YOU WOULD LIKE THE EXCEL FILES TO BE DUMPED
    readonly private static string EXCELDIRECTORY = @"./";

    // Set Gpu type (Placeholder) and type of hwaccels based on that gpu
    // NEED TO FIND A WAY TO AUTO DETECT GPU / OR AT LEAST MANUALLY INPUT ; ADD CODE AT "GpuType.cs"
    private static GpuType gpu = GpuType.Nvidia;

    private static int testno = 1;

    //Combining file name and path
    private static string fileName = $"AutomatedData_#List of configurations. Add new configurations or edit existing ones by using IntelliSense.


        // Set Sources path
        string[] fileNames = Directory.GetFiles(TESTSOURCESPATH);

        // Create a List of Tuples which store the Video info., it's performance and accel type
        List<Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator>> videoPerfData = new();

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
                HardwareAccelerator hwaccel = new(hardwareAccel, gpu, true);

                FFmpegProcess ffmpegProcess = FilenameToFFmpegProcess(filename, video, hwaccel);
                
                // Start the ffmpeg process
                var p = ffmpegProcess.StartProcess(hwaccel.IsHardwareAccel);

                // Start data collection
                if (p != null)
                {
                    Thread.Sleep(1500); // variable 1-2 secs (experiment)

                    container.PopulateData(gpu);
                    Console.WriteLine("Data Populated.");

                    Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator> tuple = new(video, container, hwaccel);
                    videoPerfData.Add(tuple);
                }

                // Start gathering fps value using process stderr
                string fps = "NOT FOUND";
                while ( p!=null && !p.StandardError.EndOfStream)
                {
                    string? line = p.StandardError.ReadLine();
                    //Console.WriteLine(line);
                    if (line != null && line.ToLower().Contains("fps"))
                    {
                        int fpsIndex = line.ToLower().IndexOf("fps");

                        string fpsString = line.ToLower()[(fpsIndex + 4)..];

                        if (fpsString.Contains('q')) fps = fpsString[..(fpsString.IndexOf("q") - 1)].Trim();
                    }
                }
                if (fps != "NOT FOUND") container.FramesPerSecond = float.Parse(fps);
                else container.FramesPerSecond = -1.0f;

                // End process if not already ended
                if (p != null && !p.HasExited)
                {
                    p.Kill();
                }

                // DEBUG
                container.DisplayValues();
                // Write to Excel
                DataListToExcel(videoPerfData, filePath);
            }

        }
    }

    /// <summary>
    /// Receive a List of tuples data to convert them into an excel sheet
    /// </summary>
    /// <param name="videoPerfData"></param>
    /// <param name="file_path"></param>
    static void DataListToExcel
        (List<Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator>> videoPerfData, 
        string file_path)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        //Creating new excel package named CPUandGPU_Decode
        using ExcelPackage CPUandGPU_Decode = new ExcelPackage();

        //adding a new worksheet
        ExcelWorksheet worksheet = CPUandGPU_Decode.Workbook.Worksheets.Add("Automated Utilization Data");

        //Creating the Headers
        string[] headers = new string[]
        {
        "No.", "OS", "GPU Type", "Decode Method", "Hwaccel", "Codec", "Chroma", "Bit-depth",
        "Resolution", "Final FPS", "CPU Overall", "GPU Overall",
        "GPU 3D", "Video Decode 0", "Video Decode 1", "Video Decode 2"
        };

        //Resizing the columns
        worksheet.Column(1).Width = 5;
        for (int col = 2; col < headers.Length + 1; col++)
        {
            worksheet.Column(col).Width = 18;
        }

        //Formatting the Excel sheet
        if (worksheet.Dimension == null)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];

                //Centering the headers
                worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                //Changing color of headers row to grey using EPPlus
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

            }
        }
        //Adding filters to headers
        worksheet.Cells[1, 1, 1, headers.Length].AutoFilter = true;

        int testCounts = 1; // 1st row is header; Data start from the 2nd

        foreach (Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator> tupleEntry in videoPerfData)
        {
            Video video; PerformanceMetricsContainer container; HardwareAccelerator hwaccel;
            video = tupleEntry.Item1;
            container = tupleEntry.Item2;
            hwaccel = tupleEntry.Item3;

            WriteToExcel(worksheet, video, container, hwaccel.HardwareAccel.ToString(), hwaccel.Gpu, testCounts++);
        }

        ////Converting entire ExcelPackage to a byte array (prep for writing data to excel)
        //byte[] draft1 = CPUandGPU_Decode.GetAsByteArray();

        ////Writes the array to a file at the specified path
        //File.WriteAllBytes(file_path, draft1);

        byte[] fileBytes = CPUandGPU_Decode.GetAsByteArray();
        File.WriteAllBytes(file_path, fileBytes);


        Console.WriteLine("Data successfully written to Excel.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="worksheet"></param>
    /// <param name="video"></param>
    /// <param name="container"></param>
    /// <param name="hardwareAccel"></param>
    /// <param name="gpu"></param>
    /// <param name="testCounts"></param>
    static void WriteToExcel
        (ExcelWorksheet worksheet, 
        Video video, PerformanceMetricsContainer container, string hardwareAccel,  GpuType? gpu, int testCounts)
    {
        //Can add if-else statements to change the color of the cells depending on chroma and color format

        //Video Information from Video obj
        string codec = video.CodecExt.ToString();
        string chroma = video.ChromaExt.ToString();
        string bitDepth = video.BitDepthExt.ToString();
        string resolution = video.ResolutionExt.ToString();

        // From container obj
        //Data we are collecting
        float? finalFPS = container.FramesPerSecond;
        float? cpuUsage = container.CpuUsage;
        float? gpuUsage = container.GpuOverall;
        float? gpu3d = container.Gpu3D;
        float? vidDec0 = container.VideoDecode0;
        float? vidDec1 = container.VideoDecode1;
        float? vidDec2 = container.VideoDecode2;

        // Information from Misc. obj
        string OS = "Windows"; // HARD-CODED
        string? gpuType = gpu?.ToString();
        string decodeMethod = (hardwareAccel == "none") ? "CPU Decoding" : "GPU Decoding";
        string hwaccel = hardwareAccel;
        int newRow = testCounts + 1;

        //Organizing the Column headers into their correct positions
        worksheet.Cells[newRow, 1].Value = testCounts;
        worksheet.Cells[newRow, 2].Value = OS;
        worksheet.Cells[newRow, 3].Value = gpuType;
        worksheet.Cells[newRow, 4].Value = decodeMethod;
        worksheet.Cells[newRow, 5].Value = hwaccel;
        worksheet.Cells[newRow, 6].Value = codec;
        worksheet.Cells[newRow, 7].Value = chroma;
        worksheet.Cells[newRow, 8].Value = bitDepth;
        worksheet.Cells[newRow, 9].Value = resolution;

        worksheet.Cells[newRow, 10].Value = finalFPS;
        worksheet.Cells[newRow, 11].Value = cpuUsage;
        worksheet.Cells[newRow, 12].Value = gpuUsage;
        worksheet.Cells[newRow, 13].Value = gpu3d;
        worksheet.Cells[newRow, 14].Value = vidDec0;
        worksheet.Cells[newRow, 15].Value = vidDec1;
        worksheet.Cells[newRow, 16].Value = vidDec2;


        //More formatting: Coloring the hwaccel, codec, and chroma for further organization
        if (hwaccel == "Cuda")
        {
            worksheet.Cells[newRow, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[newRow, 5].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
        }
        else if (hwaccel == "D3D11VA")
        {
            worksheet.Cells[newRow, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[newRow, 5].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
        }
        else if (hwaccel == "Vulkan")
        {
            worksheet.Cells[newRow, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[newRow, 5].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
        }
        else if (hwaccel == "VAAPI")
        {
            worksheet.Cells[newRow, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[newRow, 5].Style.Fill.BackgroundColor.SetColor(Color.LightSalmon);
        }
        else if (hwaccel == "None")
        {
            worksheet.Cells[newRow, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[newRow, 5].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
        }
        else if (hwaccel == "QSV")
        {
            worksheet.Cells[newRow, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[newRow, 5].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
        }
        

        if (codec == "h264" || codec == "H264")
        {
            worksheet.Cells[newRow, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[newRow, 6].Style.Fill.BackgroundColor.SetColor(Color.Salmon);
        }
        else if (codec == "h265" || codec == "H265")
        {
            worksheet.Cells[newRow, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[newRow, 6].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
        }


        if (chroma == "Subsampling_420")
        {
            worksheet.Cells[newRow, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[newRow, 7].Style.Fill.BackgroundColor.SetColor(Color.Green);
        }
        else if (chroma == "Subsampling_444")
        {
            worksheet.Cells[newRow, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[newRow, 7].Style.Fill.BackgroundColor.SetColor(Color.Red);
        }
    }
}