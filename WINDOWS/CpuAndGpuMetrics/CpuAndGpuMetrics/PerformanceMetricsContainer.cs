using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Container for information related to GPU and CPU performance metrics.
    /// </summary>
    public class PerformanceMetricsContainer
    {
        /// <summary>Frames per second.</summary>
        private float? framesPerSecond;

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
        private float videoEncode;

        /// <summary>Overall CPU usage.</summary>
        private float cpuUsage;

        /// <summary>
        /// Initializes a PerformanceMetricsContainer object.
        /// </summary>
        public PerformanceMetricsContainer() { }

        /// <summary>
        /// Gets or sets the frames per seconds.
        /// </summary>
        public float? FramesPerSecond
        {
            get { return framesPerSecond; }
            set { framesPerSecond = value; }
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
            set { videoEncode = (float)value; }
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
        /// Reads performance metrics from the system and populates the relevant fields.
        /// </summary>
        /// <param name="type">The Gpu type (Nvidia or Intel).</param>
        public void PopulateData(GpuType type)
        {
            float[] gpuMetrics = GpuMetricRetriever.GetGpuUsage();

            CpuUsage = CpuMetricRetriever.GetCpuUsage();

            // Khang added these 2 lines -> How can we get all the decode processes? The decode value that we return is only the average
            Gpu3D = gpuMetrics[0];

            GpuCopy = gpuMetrics[1];

            if (type == GpuType.Intel)
            {
                VideoDecode0 = gpuMetrics[2];
                VideoDecode1 = 0;
                GpuOverall = new[] { Gpu3D, VideoDecode0, GpuCopy }.Max();
                VideoEncode = gpuMetrics[3];
            } 
            else if (type == GpuType.Nvidia)
            {
                VideoDecode0 = VideoDecode1 = (float)(VideoDecode2 = gpuMetrics[2] / 3);
                GpuOverall = new[] { Gpu3D, VideoDecode0, GpuCopy }.Max();
                VideoEncode = gpuMetrics[3];
            }
        }
        
        /*
         public void DisplayValues()
         {
             Console.WriteLine("Performance Metrics:");
             Console.WriteLine($"Frames Per Second: {FramesPerSecond?.ToString() ?? "N/A"}");
             Console.WriteLine($"GPU Overall: {GpuOverall}");
             Console.WriteLine($"GPU 3D: {Gpu3D}");
             Console.WriteLine($"GPU Copy: {GpuCopy}");
             Console.WriteLine($"Video Decode 0: {VideoDecode0}");
             Console.WriteLine($"Video Decode 1: {VideoDecode1}");
             Console.WriteLine($"Video Decode 2: {VideoDecode2?.ToString() ?? "N/A"}");
             Console.WriteLine($"CPU Usage: {CpuUsage}");
         }
        */
    }
}
