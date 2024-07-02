
using Hardware.Info;
using OfficeOpenXml;
//main namespace for EPPlus library (allows us to manipulate the excel file)
using System.Drawing;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Excel Writer class (concerns everything related to writing the data in the excel sheet)
    /// </summary>
    public abstract class ExcelWriter()
    {
        /// <summary>
        /// Hardware info object to get specs of the current PC
        /// </summary>
        public IHardwareInfo hardwareInfo = new HardwareInfo();

        /// <summary>
        /// To be removed?
        /// </summary>
        public int TestNbr { get; set; }

        /// <summary>
        /// Header column names
        /// </summary>
        public string[] Headers { get; set; } = ["NO WRITER"];

        /// <summary>
        /// Name of the excel file
        /// </summary>
        public string Name { get; set; } = "Default";

        /// <summary>
        /// Excel package item
        /// </summary>
        public static ExcelPackage CPUandGPUExcelPack = new();

        /// <summary>
        /// Check if excel exist
        /// </summary>
        /// <param name="sheetName"></param>
        /// <returns>Excel worksheet</returns> 
        public ExcelWorksheet CheckExcelWorksheetExist(string sheetName)
        {
            try
            {
                //Searches to see if a worksheet with the given name exists
                var sheet = CPUandGPUExcelPack.Workbook.Worksheets.FirstOrDefault(w => w.Name.Equals(sheetName));
                if (sheet != null) return sheet;
                else
                {
                    //creating a new worksheet with that name 
                    Console.WriteLine("---Creating new excel sheet");
                    return CPUandGPUExcelPack.Workbook.Worksheets.Add(sheetName);
                }
            }
            catch
            {
                //incase of an exception add a new worksheet with the given name
                return CPUandGPUExcelPack.Workbook.Worksheets.Add(sheetName);
            }
        }

        /// <summary>
        /// Excel Coloring
        /// </summary>
        /// <param name="worksheet"></param>
        /// <param name="hwaccel"></param>
        /// <param name="codec"></param>
        /// <param name="chromaAndBitDepth"></param>
        /// <param name="newRow"></param>
        public void ExcelColoring(ExcelWorksheet worksheet, string hwaccel, string codec, string chromaAndBitDepth, int newRow)
        {
            //Dictionary with key value pairs for assigning colors to different hwaccells
            Dictionary<string, Color> hwaccelColor = new Dictionary<string, Color>()
            {
                 {"Cuda",Color.LightSlateGray },
                 {"D3D11VA", Color.LightSteelBlue},
                 {"D3D12VA", Color.PowderBlue},
                 {"Vulkan", Color.LightYellow},
                 {"VAAPI", Color.LightSalmon},
                 {"None", Color.Yellow},
                 {"QSV", Color.LightSkyBlue},
                 {"VDPAU", Color.Linen},
                 {"Unknown", Color.White}

            };

            if (hwaccelColor.ContainsKey(hwaccel))
            {
                //setting the color to the row and the respective column
                worksheet.Cells[newRow, 9].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[newRow, 9].Style.Fill.BackgroundColor.SetColor(hwaccelColor[hwaccel]);
            }

            //Dictionary for assigning colors based on the source codec
            Dictionary<string, Color> codecsColor = new Dictionary<string, Color>()
            {
                 {"h264", Color.Plum},
                 {"H264", Color.Plum},
                 {"h265", Color.SeaShell},
                 {"H265", Color.SeaShell},
                 {"Unknown", Color.White}

            };

            if (codecsColor.ContainsKey(codec))
            {
                worksheet.Cells[newRow, 10].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[newRow, 10].Style.Fill.BackgroundColor.SetColor(codecsColor[codec]);
            }

            //Dictionary for assigning colors based on the source chroma and bit_depth
            Dictionary<string, Color> chromaAndBitDepthColor = new Dictionary<string, Color>()
            {
                {"YUV_420_Bit_8", Color.MistyRose},
                {"YUV_420_Bit_10", Color.Lavender},
                {"YUV_422_Bit_8", Color.Tan},
                {"YUV_422_Bit_10", Color.CornflowerBlue},
                {"YUV_444_Bit_8", Color.Pink},
                {"YUV_444_Bit_10", Color.LightCyan},
                {"Unknown", Color.White}
            };

            if (chromaAndBitDepthColor.ContainsKey(chromaAndBitDepth))
            {
                worksheet.Cells[newRow, 11].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[newRow, 11].Style.Fill.BackgroundColor.SetColor(chromaAndBitDepthColor[chromaAndBitDepth]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="videoPerfData"></param>
        /// <param name="file_path"></param>
        /// <param name="parallelStreams"></param>
        /// <param name="gpuIndex"></param>
        public void DataListToExcel
            (List<Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator>> videoPerfData,
            string file_path, int parallelStreams, int gpuIndex)
        {
            // Refreshing the hardware information
            hardwareInfo.RefreshAll();

            // Set licensing context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Adding new worksheets for data to be written to
            var worksheet = CheckExcelWorksheetExist($"Test {TestNbr} Automated {Name} Data");

            // Resizing the columns 
            worksheet.Column(1).Width = 5;

            for (int col = 2; col < Headers.Length + 1; col++)
            {
                worksheet.Column(col).Width = 18;
            }

            // Formatting the Excel sheet
            if (worksheet.Dimension == null)
            {
                for (int i = 0; i < Headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = Headers[i];

                    //Centering the headers
                    worksheet.Cells[1, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    //Changing color of headers row to grey using EPPlus
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);

                }
            }

            // Adding filters to headers
            worksheet.Cells[1, 1, 1, Headers.Length].AutoFilter = true;

            int testCounts = 1; // 1st row is header; Data start from the 2nd

            // Add every row of data from videoPerfData to the excel worksheet
            foreach (Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator> tupleEntry in videoPerfData)
            {
                Video video; PerformanceMetricsContainer container; HardwareAccelerator hwaccel;
                video = tupleEntry.Item1;
                container = tupleEntry.Item2;
                hwaccel = tupleEntry.Item3;

                WriteToExcel(worksheet, video, container, hwaccel.HardwareAccel.ToString(), hwaccel.Gpu, testCounts, parallelStreams, gpuIndex);

                testCounts++;
            }

            // Writes the array to a file at the specified path
            byte[] fileBytes = CPUandGPUExcelPack.GetAsByteArray();
            File.WriteAllBytes(file_path, fileBytes);

            Console.WriteLine("Data successfully written to Excel.");
        }

        /// <summary>
        /// Excel Writing format
        /// </summary>
        /// <param name="worksheet">Excel Worksheet/Current page to write data in</param>
        /// <param name="video">Contains information about video formats, etc...</param>
        /// <param name="hardwareAccel">Hardware acceleration name.</param>
        /// <param name="gpu">Gpu Brand name.</param>
        /// <param name="testCounts"></param>
        /// <param name="parallelStreams">Number of parallel streams tested with.</param>
        protected abstract void WriteToExcel
            (ExcelWorksheet worksheet,
            Video video, PerformanceMetricsContainer container, string hardwareAccel, GpuType? gpu, int testCounts, int parallelStreams, int gpuIndex);
    }

    /// <summary>
    /// Excel Writer class for Decode option
    /// </summary>
    public class ExcelWriterDecodeOnly : ExcelWriter
    {
        /// <summary>
        /// Constructor for ExcelWriterDecodeOnly (Mostly writing the header/column name)
        /// </summary>
        /// <param name="TestNbr"></param>
        public ExcelWriterDecodeOnly(int TestNbr)
        {
            this.TestNbr = TestNbr;
            this.Name = "Decode";
            this.Headers =
            [
                "No.",
                "Time Stamp",
                "OS",
                "CPU Man.",
                "Motherboard Man.",
                "Motherboard Product",
                "GPU Brand",
                "GPU Name",
                "Hwaccel",
                "Codec",
                "Chroma & Bit_depth",
                "Resolution",
                "No. of Streams",
                "Average FPS",
                "Max FPS",
                "Min FPS",
                "CPU Overall",
                "GPU Overall",
                "GPU 3D",
                "Video Decode 0",
                "Video Decode 1",
                "Video Decode 2"
            ];
        }

        /// <summary>
        /// Writing data to worksheet for Decode Option
        /// </summary>
        /// <param name="worksheet">Excel Worksheet/Current page to write data in</param>
        /// <param name="video">Contains information about video formats, etc...</param>
        /// <param name="hardwareAccel">Hardware acceleration name.</param>
        /// <param name="gpu">Gpu Brand name.</param>
        /// <param name="testCounts"></param>
        /// <param name="parallelStreams">Number of parallel streams tested with.</param>
        protected override void WriteToExcel
            (ExcelWorksheet worksheet,
            Video video, PerformanceMetricsContainer container, string hardwareAccel, GpuType? gpu, int testCounts, int parallelStreams, int gpuIndex)
        {

            //Collecting hardware information from the system         
            string osName = hardwareInfo.OperatingSystem.Name;
            string cpuName = hardwareInfo.CpuList[0].Name;
            string moboManufacturer = hardwareInfo.MotherboardList[0].Manufacturer;
            string moboProduct = hardwareInfo.MotherboardList[0].Product;

            //Collecting GPU manufacturer and model from Program settings (ONLY LINUX)

            //Video Information from Video obj
            string codec = video.CodecExt.ToString();
            string chroma = video.ChromaExt.ToString();
            string bitDepth = video.BitDepthExt.ToString();
            string resolution = video.ResolutionExt.ToString();

            //Adding chroma and bit_depth strings
            String chromaAndBit = $"{chroma}_{bitDepth}";

            // From container obj
            //Data we are collecting
            float? averageFPS = container.AverageFramesPerSecond;
            float? maxFPS = container.MaxFramesPerSecond;
            float? minFPS = container.MinFramesPerSecond;

            string timestamp = container.TimeStampNow;
            float? cpuUsage = container.CpuUsage;
            float? gpuUsage = container.GpuOverall;
            float? gpu3d = container.Gpu3D;
            float? vidDec0 = container.VideoDecode0;
            float? vidDec1 = container.VideoDecode1;
            float? vidDec2 = container.VideoDecode2;

            // Information from Misc. obj
            string OS = ProgramSettings.CURRENT_OS.ToString(); // HARD-CODED
            string? gpuType = gpu?.ToString();
            string? gpuName = ProgramSettings.GPU_LIST[gpuIndex];
            string hwaccel = hardwareAccel;
            int newRow = testCounts + 1;

            //Organizing the Column headers into their correct positions
            worksheet.Cells[newRow, 1].Value = testCounts;
            worksheet.Cells[newRow, 2].Value = timestamp;
            worksheet.Cells[newRow, 3].Value = osName;
            worksheet.Cells[newRow, 4].Value = cpuName;
            worksheet.Cells[newRow, 5].Value = moboManufacturer;
            worksheet.Cells[newRow, 6].Value = moboProduct;

            worksheet.Cells[newRow, 7].Value = gpuType;
            worksheet.Cells[newRow, 8].Value = gpuName;
            worksheet.Cells[newRow, 9].Value = hwaccel;
            worksheet.Cells[newRow, 10].Value = codec;
            worksheet.Cells[newRow, 11].Value = chromaAndBit;
            worksheet.Cells[newRow, 12].Value = resolution;
            worksheet.Cells[newRow, 13].Value = parallelStreams;

            worksheet.Cells[newRow, 14].Value = averageFPS;
            worksheet.Cells[newRow, 15].Value = maxFPS;
            worksheet.Cells[newRow, 16].Value = minFPS;
            worksheet.Cells[newRow, 17].Value = cpuUsage;
            worksheet.Cells[newRow, 18].Value = gpuUsage;
            worksheet.Cells[newRow, 19].Value = gpu3d;
            worksheet.Cells[newRow, 20].Value = vidDec0;
            worksheet.Cells[newRow, 21].Value = vidDec1;
            worksheet.Cells[newRow, 22].Value = vidDec2.HasValue ? vidDec2 : "N/A";

            //Ensuring automatic resizing of the cells for the data
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            //Coloring the hwaccel, codec, and chroma
            ExcelColoring(worksheet, hwaccel, codec, chromaAndBit, newRow);
            //Ensuring automatic resizing of the cells for the data
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            //Coloring the hwaccel, codec, and chroma
            ExcelColoring(worksheet, hwaccel, codec, chromaAndBit, newRow);
   }
    }

    /// <summary>
    /// Excel Writer class for Encode option
    /// </summary>
    public class ExcelWriterEncodeOnly : ExcelWriter
    {
        /// <summary>
        /// Constructor for ExcelWriterEncodeOnly (Mostly writing the header/column name)
        /// </summary>
        /// <param name="TestNbr"></param>
        public ExcelWriterEncodeOnly(int TestNbr)
        {
            this.TestNbr = TestNbr;
            this.Name = "Encode";
            this.Headers =
            [
                "No.",
                "Time Stamp",
                "OS",
                "CPU Man.",
                "Motherboard Man.",
                "Motherboard Product",
                "GPU Brand",
                "GPU Name",
                "Hwaccel",
                "Codec",
                "Chroma & Bit_depth",
                "Resolution",
                "No. of Streams",
                "Average FPS",
                "Max FPS",
                "Min FPS",
                "CPU Overall",
                "GPU Overall",
                "Video Encode"
            ];
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
        protected override void WriteToExcel
            (ExcelWorksheet worksheet,
            Video video, PerformanceMetricsContainer container, string hardwareAccel, GpuType? gpu, int testCounts, int parallelStreams, int gpuIndex)
        {
            // IHardwareInfo hardwareInfo = new HardwareInfo();
            // hardwareInfo.RefreshAll();
            //refreshing hardware information
            // hardwareInfo.RefreshAll();

            //Collecting hardware information from the system
            string osName = hardwareInfo.OperatingSystem.Name;
            string cpuName = hardwareInfo.CpuList[0].Name;
            string moboManufacturer = hardwareInfo.MotherboardList[0].Manufacturer;
            string moboProduct = hardwareInfo.MotherboardList[0].Product;

            //Video Information from Video obj
            string codec = video.CodecExt.ToString();
            string chroma = video.ChromaExt.ToString();
            string bitDepth = video.BitDepthExt.ToString();
            string resolution = video.ResolutionExt.ToString();

            //Adding chroma and bit_depth strings
            String chromaAndBitEncode = $"{chroma}_{bitDepth}";

            // From container obj
            //Data we are collecting
            float? finalFPS = container.AverageFramesPerSecond;

            // From container obj
            //Data we are collecting
            float? averageFPS = container.AverageFramesPerSecond;
            float? maxFPS = container.MaxFramesPerSecond;
            float? minFPS = container.MinFramesPerSecond;
            float? cpuUsage = container.CpuUsage;
            float? gpuUsage = container.GpuOverall;
            float? vidEnc = container.VideoEncode;
            string? timestamp = container.TimeStampNow;

            // Information from Misc. obj fake comment
            string OS = ProgramSettings.CURRENT_OS.ToString(); // HARD-CODED
            string? gpuType = gpu?.ToString();
            string? gpuName = ProgramSettings.GPU_LIST[gpuIndex];
            string hwaccel = hardwareAccel;
            int newRow = testCounts + 1;

            //Organizing the Column headers into their correct positions
            worksheet.Cells[newRow, 1].Value = testCounts;
            worksheet.Cells[newRow, 2].Value = timestamp;
            worksheet.Cells[newRow, 3].Value = osName;
            worksheet.Cells[newRow, 4].Value = cpuName;
            worksheet.Cells[newRow, 5].Value = moboManufacturer;
            worksheet.Cells[newRow, 6].Value = moboProduct;

            worksheet.Cells[newRow, 7].Value = gpuType;
            worksheet.Cells[newRow, 8].Value = gpuName;
            worksheet.Cells[newRow, 9].Value = hwaccel;
            worksheet.Cells[newRow, 10].Value = codec;
            worksheet.Cells[newRow, 11].Value = chromaAndBitEncode;
            worksheet.Cells[newRow, 12].Value = resolution;
            worksheet.Cells[newRow, 13].Value = parallelStreams;


            worksheet.Cells[newRow, 14].Value = averageFPS;
            worksheet.Cells[newRow, 15].Value = maxFPS;
            worksheet.Cells[newRow, 16].Value = minFPS;
            worksheet.Cells[newRow, 17].Value = cpuUsage;
            worksheet.Cells[newRow, 18].Value = gpuUsage;
            worksheet.Cells[newRow, 19].Value = vidEnc;

            //Ensuring automatic resizing of the cells for the data
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            //Coloring the hwaccel, codec, and chroma
            ExcelColoring(worksheet, hwaccel, codec, chromaAndBitEncode, newRow);

        }
    }
}
