namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Container for information related to GPU and CPU performance metrics.
    /// </summary>
    public class PerformanceMetricsContainer
    {
        /// <summary>Average fps.</summary>
        private float averagefps;

        /// <summary>Max fps.</summary>
        private float maxfps;

        /// <summary>Min fps.</summary>
        private float minfps;

        /// <summary>Overall GPU usage.</summary>
        private float gpuOverall;

        /// <summary>Overall 3D GPU usage.</summary>
        private float gpu3D;

        /// <summary>Overall GPU copy usage.</summary>
        private float gpuCopy;

        /// <summary>Overall GPU video decode usage on engine 0.</summary>
        private float videoDecode0;

        /// <summary>Overall GPU video decode usage on engine 1.</summary>
        private float videoDecode1;

        /// <summary>Overall GPU video decode usage on engine 2. (n/a on intel GPU)</summary>
        private float? videoDecode2;

        /// <summary>Overall GPU video encode usage </summary>
        private float? videoEncode;

        /// <summary>Overall CPU usage.</summary>
        private float cpuUsage;

        /// <summary>
        /// Current timestamp of when we populate data.
        /// </summary>
        private string timestampNow;

        /// <summary>
        /// Initializes an empty PerformanceMetricsContainer object.
        /// </summary>
        public PerformanceMetricsContainer()
        {
            this.gpuOverall = 0;
            this.gpu3D = 0;
            this.videoDecode0 = 0;
            this.videoDecode1 = 0;
            this.videoDecode2 = 0;
            this.videoEncode = 0;
            this.cpuUsage = 0;
            this.timestampNow = DateTime.Now.TimeOfDay.ToString();
        }

        /// <summary>
        /// Deep Copy constructor for PerformanceMetricsContainer object.
        /// </summary>
        /// <param name="container">Container to be copied.</param>
        public PerformanceMetricsContainer(PerformanceMetricsContainer container)
        {
            this.gpuOverall = container.GpuOverall;
            this.gpu3D = container.Gpu3D;
            this.videoDecode0 = container.VideoDecode0;
            this.videoDecode1 = container.VideoDecode1;
            this.videoDecode2 = container.VideoDecode2;
            this.videoEncode = container.VideoEncode;
            this.cpuUsage = container.CpuUsage;
            this.timestampNow = container.timestampNow;
        }

        /// <summary>
        /// Gets or sets the Average fps.
        /// </summary>
        public float AverageFramesPerSecond
        {
            get { return averagefps; }
            set { averagefps = value; }
        }

        /// <summary>
        /// Gets or sets the Max fps.
        /// </summary>
        public float MaxFramesPerSecond
        {
            get { return maxfps; }
            set { maxfps = value; }
        }

        /// <summary>
        /// Gets or sets the Min fps.
        /// </summary>
        public float MinFramesPerSecond
        {
            get { return minfps; }
            set { minfps = value; }
        }

        /// <summary>
        /// Gets or sets the overall gpu usage %.
        /// </summary>
        public float GpuOverall
        {
            get { return gpuOverall; }
            set { gpuOverall = value; }
        }

        /// <summary>
        /// Gets or sets the 3D gpu %.
        /// </summary>
        public float Gpu3D
        {
            get { return gpu3D; }
            set { gpu3D = value; }
        }

        /// <summary>
        /// Gets or sets the gpu copy %. 
        /// </summary>
        public float GpuCopy
        {
            get { return gpuCopy; }
            set { gpuCopy = value; }
        }

        /// <summary>
        /// Gets or sets the video decode gpu % (from engine 0).
        /// </summary>
        public float VideoDecode0
        {
            get { return videoDecode0; }
            set { videoDecode0 = value; }
        }

        /// <summary>
        /// Gets or sets the video decode gpu % (from engine 1).
        /// </summary>
        public float VideoDecode1
        {
            get { return videoDecode1; }
            set { videoDecode1 = value; }
        }

        /// <summary>
        /// Gets or sets the video decode gpu % (from engine 2).
        /// </summary>
        public float? VideoDecode2
        {
            get { return videoDecode2; }
            set { videoDecode2 = value; }
        }

        /// <summary>
        /// Gets or sets the video encode gpu %.
        /// </summary>
        public float? VideoEncode
        {
            get { return videoEncode; }
            set { videoEncode = value; }
        }

        /// <summary>
        /// Gets or sets the overall cpu usage %. 
        /// </summary>
        public float CpuUsage
        {
            get { return cpuUsage; }
            set { cpuUsage = value; }
        }

        /// <summary>
        /// Gets or sets the current timestamp in string. 
        /// </summary>
        public string TimeStampNow
        {
            get { return timestampNow; }
            set { timestampNow = value; }
        }

        /// <summary>
        /// Reads performance metrics from the system and populates the relevant fields.
        /// </summary>
        /// <param name="type">The Gpu type (Nvidia or Intel).</param>
        public async void PopulateData(GpuType type)
        {
            // Get CPU & GPU metrics
            var CpuUsageTask = CpuMetricRetriever.GetCpuUsage();
            var gpuMetricsTask = GpuMetricRetriever.GetGpuUsage(type);

            float CpuUsage = await CpuUsageTask;
            float[] gpuMetrics = await gpuMetricsTask;
            this.CpuUsage = CpuUsage;

            // Get timestamp
            TimeSpan timeStamp = DateTime.Now.TimeOfDay;
            this.timestampNow = timeStamp.ToString(); 

            // Metrics format and address changes depending on OS
            if (ProgramSettings.CURRENT_OS == OS.Windows)
            {
                // Function returns array in format: float[] { d3Utilization, intelEncodeUtilization?, copyUtilization, encodeUtilization, videoDecodeUtilization{0,1,2} }
                if (type != GpuType.Unknown)
                {
                    int metricsLength = gpuMetrics.Length;
                    this.VideoDecode0 = gpuMetrics[4];
                    // Move the following rows to the respective GPU after validating with different setups
                    this.VideoDecode1 = (metricsLength >= 6) ? gpuMetrics[5] : 0;
                    this.VideoDecode2 = (metricsLength >= 7) ? gpuMetrics[6] : null;
                    this.Gpu3D = gpuMetrics[0];
                    this.GpuCopy = gpuMetrics[2];
                    this.GpuOverall = gpuMetrics.Where((value, index) => index != 1).Max();

                    if (type == GpuType.Intel)
                    {
                        this.VideoEncode = (ProgramSettings.IS_DECODE_ONLY_ON) ? -1.0f : gpuMetrics[1]; // TO BE DETERMINED when encoding with multiple streams
                    }
                    else if (type == GpuType.Nvidia)
                    {
                        this.VideoEncode = gpuMetrics[3];
                    }
                }
            }
            else if (ProgramSettings.CURRENT_OS == OS.Linux)
            {
                // REQUIRES MORE VALIDATION
                this.GpuCopy = -1.0f;
                if (type == GpuType.Nvidia)
                {
                    this.GpuOverall = gpuMetrics[0];
                    this.VideoDecode0 = gpuMetrics[1];
                    this.VideoDecode1 = 0;
                    this.VideoDecode2 = 0;
                    this.VideoEncode = gpuMetrics[2];
                }

                else if (type == GpuType.Intel)
                {
                    this.Gpu3D = gpuMetrics[0];
                    this.VideoDecode0 = gpuMetrics[1];
                    this.VideoDecode1 = gpuMetrics[2];
                    this.VideoDecode2 = null;
                    this.VideoEncode = (ProgramSettings.IS_DECODE_ONLY_ON) ? -1.0f : Math.Max(this.VideoDecode0, this.VideoDecode1);
                    this.GpuOverall = gpuMetrics.Max();
                }
            }
            else this.GpuOverall = gpuMetrics[0];

            // DisplayValues();
        }

        /// <summary>
        /// Display all varibles' values of the container class
        /// </summary>
        public void DisplayValues()
        {
            Console.WriteLine("--- Performance Metrics: ---");
            // Console.WriteLine($"GPU Overall: {GpuOverall}");
            // Console.WriteLine($"GPU 3D: {Gpu3D}");
            // Console.WriteLine($"GPU Copy: {GpuCopy}");
            // Console.WriteLine($"Video Decode 0: {VideoDecode0}");
            // Console.WriteLine($"Video Decode 1: {VideoDecode1}");
            // Console.WriteLine($"Video Decode 2: {VideoDecode2?.ToString() ?? "N/A"}");
            Console.WriteLine($"Video Encode: {VideoEncode}");
            Console.WriteLine($"CPU Usage: {CpuUsage}");
        }
    }
}
