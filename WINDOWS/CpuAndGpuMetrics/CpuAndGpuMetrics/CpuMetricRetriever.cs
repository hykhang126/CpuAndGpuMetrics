using System.Diagnostics;
using static CpuAndGpuMetrics.CounterReader;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Static class to retrieve CPU usage metrics.
    /// </summary>
    static internal class CpuMetricRetriever
    {
        /// <summary>Time (in ms) before reading CPU usage.</summary>
        static readonly int TIME = 100;

        /// <summary>
        /// Gets the current CPU usage and prints it to the console.
        /// </summary>
        /// <returns>
        /// A float indicating the current Cpu usage.
        /// </returns>
        public static float GetCpuUsage()
        {
            float cpuUsage = 0;
            PerformanceCounter? cpuCounter = null;
      
            try
            {
                cpuCounter = new("Processor", "% Processor Time", "_Total");
                cpuUsage = GetReading(cpuCounter, TIME);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                cpuCounter?.Dispose();
            }

            return cpuUsage;
        }
    }
}