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
        public static async Task<float[]> GetGpuUsage(GpuType type)
        {
            if (ProgramSettings.CURRENT_OS == OS.Windows)
            {
                // Function returns array in format: float[] { d3Utilization, copyUtilization, decodeUtilization, encodeUtilization }
                return RunWindowPerformanceCounter();
            }
            else if (ProgramSettings.CURRENT_OS == OS.Linux)
            {
                if (type == GpuType.Nvidia)
                {
                    string command = "nvidia-smi --query-gpu=utilization.gpu,utilization.decoder,utilization.encoder --format=csv,noheader | awk '{print $1,$3,$5}'";

                    string result = await ExecuteBashCommand(command);
                    string[] resultTokens = result.Split(' ');

                    float gpuUsage = float.Parse(resultTokens[0]);
                    float gpuDecode = float.Parse(resultTokens[1]);
                    float gpuEncode = float.Parse(resultTokens[2]);

                    return [gpuUsage, gpuDecode, gpuEncode];
                }
                else if (type == GpuType.Intel)
                {
                    string password = "matrox";
                    float timeout = 5.0f;
                    string command = $"echo ${password} | sudo -S timeout ${timeout} intel_gpu_top -o - | tail -n +3 | awk '{{print $5,$11,$14}}'";

                    string result = await ExecuteBashCommand(command);
                    string[] resultTokens = result.Split(' ');

                    float gpu3D = float.Parse(resultTokens[0]);
                    float gpuDecode0 = float.Parse(resultTokens[1]);
                    float gpuDecode1 = float.Parse(resultTokens[2]);
                    float gpuEncode = Math.Max(gpuDecode0, gpuDecode1);

                    return [gpu3D, gpuDecode0, gpuDecode1, gpuEncode];
                }
                else return [-23.04f];
            }

            Console.WriteLine("ERROR GETTING CORRECT CPU USAGE! Cpu usage return -1 by default");
            return [-1f];
        }

        static float[] RunWindowPerformanceCounter()
        {
            try
            {
                // Initialize PerformanceCounters for GPU metrics
                PerformanceCounterCategory category = new("GPU Engine");
                string[] instanceNames = category.GetInstanceNames();

                if (instanceNames == null || instanceNames.Length == 0)
                {
                    Console.WriteLine("No instances found for 'GPU Engine'.");
                    return new float[] { -99f };
                }

                // float[] totalValues = new float[instanceNames.Length];
                float[] decodeValues = new float[instanceNames.Length];
                float[] d3Values = new float[instanceNames.Length];
                float[] copyValues = new float[instanceNames.Length];
                float[] encodeValues = new float[instanceNames.Length];

                string[] engtypeValues = ["engtype_3D", "engtype_VideoDecode", "engtype_Copy", "engtype_VideoEncode"];

                // Loop through all instances and populate values
                for (int i = 0; i < instanceNames.Length; i++)
                {

                    string instance = instanceNames[i];
                    PerformanceCounter counter = new("GPU Engine", "Utilization Percentage", instance);

                    float value;

                    // Mapping/hash set
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

                    if (instance.Contains("engtype_VideoEncode"))
                    {
                        value = GetReading(counter, TIME);
                        encodeValues[i] = value;
                    }

                    counter.Dispose();

                }

                float d3Utilization = d3Values.Sum();
                float decodeUtilization = decodeValues.Sum();
                float copyUtilization = copyValues.Sum();
                float encodeUtilization = encodeValues.Sum();
                Console.WriteLine($"3d: {d3Utilization}, decode: {decodeUtilization}, copy: {copyUtilization}",
                    d3Utilization, decodeUtilization, copyUtilization);

                return new float[] { d3Utilization, copyUtilization, decodeUtilization, encodeUtilization };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new float[] {-99f};
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        static async Task<string> ExecuteBashCommand(string command)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{command}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string result = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();

                return result.Trim();
            }
        }
    }
}
