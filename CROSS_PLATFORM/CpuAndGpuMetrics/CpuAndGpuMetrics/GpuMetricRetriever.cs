
﻿using System.Diagnostics;
using System.Reflection.Metadata;
using static CpuAndGpuMetrics.CounterReader;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Static class to retrieve GPU metrics.
    /// </summary>
    static internal class GpuMetricRetriever
    {
        /// <summary>The time to wait (in milliseconds) between initializing and reading the counter for more accuracy.</summary>
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
                string[] uniqueVideoDecode = instanceNames
                .Select(s => s.Split(new[] { "_eng_" }, StringSplitOptions.None))
                .Where(split => split.Length > 1 && split[1].Contains("VideoDecode"))
                .Select(split => split[1]).Distinct().OrderBy(str => str).ToArray(); 


                if (instanceNames == null || instanceNames.Length == 0)
                {
                    Console.WriteLine("No instances found for 'GPU Engine'.");
                    return new float[] { -99f };
                }

                Dictionary<string, float> engtypeValues = new Dictionary<string, float>
                {
                    { "engtype_3D", 0 },
                    { "engtype_VideoDecode", 0 },
                    { "engtype_Copy", 0 },
                    { "engtype_VideoEncode", 0 }
                };
                foreach (string instanceName in uniqueVideoDecode) engtypeValues.Add(instanceName, 0f);

                // Loop through all instances and populate values
                for (int i = 0; i < instanceNames.Length; i++)
                {

                    string instance = instanceNames[i];
                    PerformanceCounter counter = new("GPU Engine", "Utilization Percentage", instance);

                    float value;
                    foreach (var engtype in engtypeValues.Keys)
                    {
                        if (instance.Contains(engtype))
                        {
                            value = GetReading(counter, TIME);
                            engtypeValues[engtype] += value;
                        }
                    }

                    counter.Dispose();

                }

                return engtypeValues.Values.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new float[] { -99f };
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
