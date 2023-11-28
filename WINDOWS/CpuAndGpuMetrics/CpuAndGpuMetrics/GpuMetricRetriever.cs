using System.Diagnostics;
using System.Reflection.Metadata;
using static CpuAndGpuMetrics.CounterReader;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Static class to retrieve GPU metrics.
    /// </summary>
    static internal class GpuMetricRetriever
    {
        /// <summary>Time (in ms) before reading GPU usage metrics.</summary>
        static readonly int TIME = 10;


        /// <summary>
        /// Retrieves GPU usage metrics.
        /// </summary>
        /// <returns>
        /// An array containing, in this order, the 3D utilization, Copy utilization, and 
        /// Decode Utilization. If an error occurs, and empty array is returned.
        /// </returns>
        public static float[] GetGpuUsage()
        {
            try
            {
                // Initialize PerformanceCounters for GPU metrics
                PerformanceCounterCategory category = new("GPU Engine");
                string[] instanceNames = category.GetInstanceNames();

                if (instanceNames == null || instanceNames.Length == 0)
                {
                    Console.WriteLine("No instances found for 'GPU Engine'.");
                    return new float[0];
                }

                // float[] totalValues = new float[instanceNames.Length];
                float[] decodeValues = new float[instanceNames.Length];
                float[] d3Values = new float[instanceNames.Length];
                float[] copyValues = new float[instanceNames.Length];

                // Loop through all instances and populate values
                for (int i = 0; i < instanceNames.Length; i++)
                {

                    string instance = instanceNames[i];
                    PerformanceCounter counter = new("GPU Engine", "Utilization Percentage", instance);

                    float value;
                    
                    if (instance.Contains("engtype_3D"))
                    {
                        value = GetReading(counter, TIME);
                        d3Values[i] = value;
                    }

                    if (instance.Contains("engtype_VideoDecode"))
                    {
                        value = GetReading(counter, TIME);
                        decodeValues[i] = value;
                    }

                    if (instance.Contains("engtype_Copy"))
                    {
                        value = GetReading(counter, TIME);
                        copyValues[i] = value;
                    }

                    counter.Dispose();
                    
                }

                float d3Utilization = d3Values.Sum();
                float decodeUtilization = decodeValues.Sum();
                float copyUtilization = copyValues.Sum();
                Console.WriteLine($"3d: {d3Utilization}, decode: {decodeUtilization}, copy: {copyUtilization}", 
                    d3Utilization, decodeUtilization, copyUtilization);

                return new float[] { d3Utilization, copyUtilization, decodeUtilization, };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new float[0];
            }
        }
    }
}
