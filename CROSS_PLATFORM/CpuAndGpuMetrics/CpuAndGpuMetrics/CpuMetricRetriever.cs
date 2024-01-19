using System.Diagnostics;
using static CpuAndGpuMetrics.CounterReader;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Static class to retrieve CPU usage metrics.
    /// </summary>
    static internal class CpuMetricRetriever
    {
        /// <summary>The time to wait (in milliseconds) between initializing and reading the counter for more accuracy.</summary>
        static readonly int TIME = 100;

        /// <summary>
        /// Gets the current CPU usage and prints it to the console.
        /// </summary>
        /// <returns>
        /// A float indicating the current Cpu usage.
        /// </returns>
        public static async Task<float> GetCpuUsage()
        {
            if (ProgramSettings.CURRENT_OS == OS.Windows)
            {
                return RunWindowPerformanceCounter();
            }

            else if (ProgramSettings.CURRENT_OS == OS.Linux)
            {
                string command = "top -b -n 2 | grep 'Cpu(s)' | tail -n 1 | awk '{print $2 + $4}'";
                string result = await ExecuteLinuxBashCommand(command);
                return float.TryParse(result, out float cpuUsage) ? cpuUsage : -23.04f;
            }

            Console.WriteLine("ERROR GETTING CORRECT CPU USAGE! Cpu usage return -1 by default");
            return -1.0f;
        }

        /// <summary>
        /// Call performance counter to get usage data after TIME.
        /// </summary>
        /// <returns></returns>
        static float RunWindowPerformanceCounter()
        {
            float cpuUsage = -99.0f;
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

        /// <summary>
        /// Execute a shell-based cmd to get cpu usage.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        static async Task<string> ExecuteLinuxBashCommand(string command)
        {
            using Process process = new();
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