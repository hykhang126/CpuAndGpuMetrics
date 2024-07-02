﻿using System.Diagnostics;
using static CpuAndGpuMetrics.CounterReader;
using System.Text.RegularExpressions;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Static class to retrieve GPU metrics.
    /// </summary>
    static internal class GpuMetricRetriever
    {
        /// <summary>
        /// Retrieves GPU usage metrics.
        /// </summary>
        /// <param name="type">Gpu Type/Brand.</param>
        /// <returns>
        /// An array containing, in this order, the 3D utilization, Decode Engine 0 Utilization, and 
        /// Decode Engine 1 Utilization. If an error occurs, and empty array is returned.
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

                    string result = ExecuteBashCommand(command);
                    result = Regex.Replace(result, @"\n", " ");
                    string[] resultTokens = result.Split(' ');

                    float gpuUsage = float.Parse(resultTokens[0]);
                    float gpuDecode = float.Parse(resultTokens[1]);
                    float gpuEncode = float.Parse(resultTokens[2]);

                    return [gpuUsage, gpuDecode, gpuEncode];
                }
                else if (type == GpuType.Intel)
                {
                    string password = "matrox";
                    string? awkCmd = ProgramSettings.LINUX_AWK_CMD;
                    float timeout = 2.0f;
                    string command = $"echo {password} | sudo -S timeout {timeout} intel_gpu_top -o - | tail -n 1 | awk {awkCmd}";

                    string result = ExecuteBashCommand(command);
                    string[] resultTokens = result.Split(' ');
                    float gpu3D = -1.0f, gpuDecode0 = -1.0f, gpuDecode1 = -1.0f;
                    if (resultTokens.Length > 0) 
                        _ = float.TryParse(resultTokens[0], out gpu3D);
                    if (resultTokens.Length > 1) 
                        _ = float.TryParse(resultTokens[1], out gpuDecode0);
                    if (resultTokens.Length > 2)
                    {
                        _ = float.TryParse(resultTokens[2], out gpuDecode1);
                    }
                    // float gpu3D = float.Parse(resultTokens[0]);
                    float gpuEncode = Math.Max(gpuDecode0, gpuDecode1);

                    return [gpu3D, gpuDecode0, gpuDecode1, gpuEncode];
                }
                else return [-1.0f];
            }

            Console.WriteLine("ERROR GETTING CORRECT CPU USAGE! Cpu usage return -1 by default");
            return [-1.0f];
        }

        /// <summary>
        /// Get GPU performance counters on Windows.
        /// </summary>
        /// <returns>List of values for each GPU engine.</returns>
        static float[] RunWindowPerformanceCounter()
        {
            try
            {
                /*
                // Initialize PerformanceCounters for GPU metrics
                PerformanceCounterCategory category = new("GPU Engine");
                string[] instanceNames = category.GetInstanceNames();
                // Get number of video decode engine
                string[] uniqueVideoDecode = instanceNames
                .Select(s => s.Split(new[] { "_eng_" }, StringSplitOptions.None))
                .Where(split => split.Length > 1 && split[1].Contains("VideoDecode"))
                .Select(split => split[1]).Distinct().OrderBy(str => str).ToArray(); 

                if (instanceNames == null || instanceNames.Length == 0)
                {
                    Console.WriteLine("No instances found for 'GPU Engine'.");
                    return new float[] { -99f };
                }

                // Initiatlizing dictionary to keep track of engine name and utilization
                Dictionary<string, float> engtypeValues = new Dictionary<string, float>
                {
                    { "engtype_3D", 0 },
                    { "engtype_VideoDecode", 0 }, // This might be encoding
                    { "engtype_Copy", 0 },
                    { "engtype_VideoEncode", 0 }
                };
                foreach (string instanceName in uniqueVideoDecode) engtypeValues.Add(instanceName, 0f);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                // Loop through all instances and populate values
                for (int i = 0; i < instanceNames.Length; i++)
                {
                    string instance = instanceNames[i];
                    PerformanceCounter counter = new("GPU Engine", "Utilization Percentage", instance);
                    float value;

                    foreach (var engtype in engtypeValues.Keys)
                    {
                        // Increment utilization number/%
                        if (instance.Contains(engtype))
                        {
                            value = GetReading(counter, TIME);
                            engtypeValues[engtype] += value;
                        }
                    }

                    counter.Dispose();
                }
                stopwatch.Stop();
                Console.WriteLine("TIME ELAPSE IS" + stopwatch.Elapsed.TotalSeconds);

                // Function returns array in format: float[] { d3Utilization, intelEncodeUtilization?, copyUtilization, encodeUtilization, videoDecodeUtilization{0,1,2} }
                return engtypeValues.Values.ToArray();
                */

                // Stopwatch stopwatch = new Stopwatch();
                // stopwatch.Start();

                // Get list of gpu engines names in performance counter
                var category = new PerformanceCounterCategory("GPU Engine");
                var instanceNames = category.GetInstanceNames();
                List<string> result = new List<string> { "engtype_3D", "engtype_VideoDecode", "engtype_Copy", "engtype_VideoEncode" };

                // Get number of video decode engine
                var uniqueVideoDecode = instanceNames
                    .Select(s => s.Split(new[] { "_eng_" }, StringSplitOptions.None))
                    .Where(split => split.Length > 1 && split[1].Contains("VideoDecode"))
                    .Select(split => split[1])
                    .Distinct()
                    .OrderBy(str => str)
                    .ToArray(); 

                // Append new list of video decode engine names to the previous list of engine names
                result.AddRange(uniqueVideoDecode);
                Dictionary<int, float> ListgpuCounters = new Dictionary<int, float>();
                instanceNames = category.GetInstanceNames();

                // Get GPU usage of every engine in the list (done in parallel to get a snapshot)
                Parallel.ForEach(result.Select((gpuEngine, index) => new { gpuEngine, index }), item =>
                {
                    // Get list of performance counter corresponding to the name of the engine (and counter needs to be Utilization %)
                    List<PerformanceCounter> gpuCounters = instanceNames
                        .Where(counterNameEnd => counterNameEnd.EndsWith(item.gpuEngine))
                        .SelectMany(counterName =>
                        {
                            try
                            {
                                return category.GetCounters(counterName)
                                    .Where(counter => counter.CounterName.Equals("Utilization Percentage"));
                            }
                            catch (Exception ex)
                            {
                                // Log the exception if needed
                                return Enumerable.Empty<PerformanceCounter>();
                            }
                        })
                        .ToList();

                    // Stopwatch stopwatch1 = new Stopwatch();
                    // stopwatch1.Start();

                    // Get value of list of performance counter
                    float temp = GetReading(gpuCounters, 100);
                    // stopwatch1.Stop();
                    // Console.WriteLine("TIME ELAPSE IS before getting the list " + stopwatch1.Elapsed.TotalSeconds);

                    // Console.WriteLine($"Reading was done for {item.gpuEngine} at time {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")}");

                    // Add value and index corresponding to engine name to return list
                    lock (ListgpuCounters)
                    {
                        ListgpuCounters.Add(item.index, temp);
                    }
                });

                // stopwatch.Stop();
                // Console.WriteLine("TIME ELAPSE IS " + stopwatch.Elapsed.TotalSeconds);

                // Returns float value array in format: float[] { d3Utilization, intelEncodeUtilization?, copyUtilization, encodeUtilization, videoDecodeUtilization{0,1,2} }
                return ListgpuCounters.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray();
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"An error occurred: {ex.Message}");
                return new float[] { -1.0f };
            }
        }

        /// <summary>
        /// Runs bash command on Linux.
        /// </summary>
        /// <param name="command">Command parameters.</param>
        /// <returns>Read bash console output.</returns>
        static string ExecuteBashCommand(string command)
        {
            using Process process = new();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"{command}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            /* 
            the old cmd here was ' string result = process.StandardOutput.ReadToEndAsync(); ' 
            which makes the whoever call PopulateData() in Perf.Container.cs not wait for the PopulateData() to finish (async behavior)
            */

            process.WaitForExit();

            return result.Trim();
        }
    }
}
