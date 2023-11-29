using System.Diagnostics;
using System.Reflection.Metadata;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Static class to retrieve GPU metrics.
    /// </summary>
    static internal class GpuMetricRetriever
    {
        public static async Task<float[]> GetGpuUsage(GpuType type)
        {
            switch (type)
            {
                case GpuType.Nvidia:
                {                    
                    string command = "nvidia-smi --query-gpu=utilization.gpu,utilization.decoder,utilization.encoder --format=csv,noheader | awk '{print $1,$3,$5}'";

                    string result = await ExecuteBashCommand(command);
                    string[] resultTokens = result.Split(' ');

                    float gpuUsage = float.Parse(resultTokens[0]);
                    float gpuDecode = float.Parse(resultTokens[1]);
                    float gpuEncode = float.Parse(resultTokens[2]);

                    return new float[] {gpuUsage, gpuDecode, gpuEncode};
                }

                case GpuType.Intel:
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

                    return new float[] {gpu3D, gpuDecode0, gpuDecode1, gpuEncode};
                }

                default:
                    return new float[] {-1, -1, -1};
            }
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
