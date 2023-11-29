using System.Diagnostics;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Static class to retrieve CPU usage metrics.
    /// </summary>
    static internal class CpuMetricRetriever
    {
        public static async Task<float> GetCpuUsage()
        {
            string command = "top -b -n 2 | grep 'Cpu(s)' | tail -n 1 | awk '{print $2 + $4}'";
            string result = await ExecuteBashCommand(command);
            return float.TryParse(result, out float cpuUsage) ? cpuUsage : 0.0f;
        }

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