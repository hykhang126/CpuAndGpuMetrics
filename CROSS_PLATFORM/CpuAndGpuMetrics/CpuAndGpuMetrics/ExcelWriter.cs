using OfficeOpenXml;
//main namespace for EPPlus library (allows us to manipulate the excel file)
using System.Drawing;
using static CpuAndGpuMetrics.Video;

namespace CpuAndGpuMetrics
{
    public abstract class ExcelWriter()
    {
		public int TestNbr { get ; set; }

        public string[] Headers { get; set; } = ["NO WRITER"];

        public string Name { get; set; } = "Default";

        public static ExcelPackage CPUandGPUExcelPack = new();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sheetName"></param>
		/// <returns></returns> 
		public ExcelWorksheet CheckExcelWorksheetExist(string sheetName)
		{
			try
			{
				var sheet = CPUandGPUExcelPack.Workbook.Worksheets.FirstOrDefault(w => w.Name.Equals(sheetName));
				if (sheet!=null) return sheet;
				else return CPUandGPUExcelPack.Workbook.Worksheets.Add(sheetName);
			}
			catch
			{
				return CPUandGPUExcelPack.Workbook.Worksheets.Add(sheetName);
			}
		}


		public void excelColoring(ExcelWorksheet worksheet, string hwaccel, string codec, string chroma, int newRow) 
		{
			// Mapping string to coolor and use .{} to fix
            if (hwaccel != "Unknown")
            {
                worksheet.Cells[newRow, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            }
            if (hwaccel == "Cuda")
            {
                worksheet.Cells[newRow, 5].Style.Fill.BackgroundColor.SetColor(Color.LightSlateGray);
            }
            else if (hwaccel == "D3D11VA")
            {
                worksheet.Cells[newRow, 5].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
            }
            else if (hwaccel == "Vulkan")
            {
                worksheet.Cells[newRow, 5].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
            }
            else if (hwaccel == "VAAPI")
            {
                worksheet.Cells[newRow, 5].Style.Fill.BackgroundColor.SetColor(Color.LightSalmon);
            }
            else if (hwaccel == "None")
            {
                worksheet.Cells[newRow, 5].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
            }
            else if (hwaccel == "QSV")
            {
                worksheet.Cells[newRow, 5].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
            }
			// Missing VDPAU


            if (codec != "Unknown")
            {
                worksheet.Cells[newRow, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            }
            if (codec == "h264" || codec == "H264")
            {
                worksheet.Cells[newRow, 6].Style.Fill.BackgroundColor.SetColor(Color.Salmon);
            }
            else if (codec == "h265" || codec == "H265")
            {
                worksheet.Cells[newRow, 6].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
            }


            if (chroma != "Unknown")
            {
                worksheet.Cells[newRow, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            }
            if (chroma == "Subsampling_420")
            {
                worksheet.Cells[newRow, 7].Style.Fill.BackgroundColor.SetColor(Color.Green);
            }
            else if (chroma == "Subsampling_444")
            {
                worksheet.Cells[newRow, 7].Style.Fill.BackgroundColor.SetColor(Color.Red);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="videoPerfData"></param>
        /// <param name="file_path"></param>
        public void DataListToExcel
			(List<Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator>> videoPerfData, 
			string file_path)
		{
			// Set licensing context
			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

			// adding new worksheets for data to be written to
			var worksheet = CheckExcelWorksheetExist($"Automated {Name} Data");

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

			foreach (Tuple<Video, PerformanceMetricsContainer, HardwareAccelerator> tupleEntry in videoPerfData)
			{
				Video video; PerformanceMetricsContainer container; HardwareAccelerator hwaccel;
				video = tupleEntry.Item1;
				container = tupleEntry.Item2;
				hwaccel = tupleEntry.Item3;

                WriteToExcel(worksheet, video, container, hwaccel.HardwareAccel.ToString(), hwaccel.Gpu, testCounts);

				testCounts++;
			}

			// Writes the array to a file at the specified path
			byte[] fileBytes = CPUandGPUExcelPack.GetAsByteArray();
			File.WriteAllBytes(file_path, fileBytes);


			Console.WriteLine("Data successfully written to Excel.");
		}


	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="worksheet"></param>
		/// <param name="video"></param>
		/// <param name="containepublicr"></param>
		/// <param name="hardwareAccel"></param>
		/// <param name="gpu"></param>
		/// <param name="testCounts"></param>
		protected abstract void WriteToExcel
			(ExcelWorksheet worksheet,
			Video video, PerformanceMetricsContainer container, string hardwareAccel,  GpuType? gpu, int testCounts);
		
	}

    public class ExcelWriterDecodeOnly : ExcelWriter
    {
        public ExcelWriterDecodeOnly(int TestNbr)
        {
			this.TestNbr = TestNbr;
			this.Name = "Decode";
			this.Headers = 
			[
            "No.", "OS", "GPU Type", "Decode Method", "Hwaccel", "Codec", "Chroma", "Bit-depth",
			"Resolution", "Final FPS", "CPU Overall", "GPU Overall",
			"GPU 3D", "Video Decode 0", "Video Decode 1", "Video Decode 2"
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
			string OS = ProgramSettings.CURRENT_OS.ToString(); // HARD-CODED
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
			worksheet.Cells[newRow, 16].Value = vidDec2.HasValue ? vidDec2 : "N/A";

			excelColoring(worksheet, hwaccel, chroma, bitDepth, newRow);

			//More formatting: Coloring the hwaccel, codec, and chroma for further organization
			
		}
    }

    public class ExcelWriterEncodeOnly : ExcelWriter
    {
        public ExcelWriterEncodeOnly(int TestNbr)
        {
			this.TestNbr = TestNbr;
			this.Name = "Encode";
			this.Headers = 
			[
            "No.", "OS", "GPU Type", "Decode Method", "Hwaccel", "Codec", "Chroma", "Bit-depth",
			"Resolution", "Final FPS", "CPU Overall", "GPU Overall", "Video Encode"
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
			float? vidEnc = container.VideoEncode;

			// Information from Misc. obj
			string OS = ProgramSettings.CURRENT_OS.ToString(); // HARD-CODED
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
			worksheet.Cells[newRow, 13].Value = vidEnc;

            //More formatting: Coloring the hwaccel, codec, and chroma for further organization
            excelColoring(worksheet, hwaccel, chroma, bitDepth, newRow);

        }
    }

}