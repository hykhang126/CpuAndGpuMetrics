using System.Diagnostics;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Static class for reading performance metrics
    /// </summary>
    static class CounterReader
    {
        /// <summary>
        /// Gets the current reading from a List of PerformanceCounter.
        /// </summary>
        /// <param name="gpuCounters">The PerformanceCounter List to read from.</param>
        /// <param name="time">The time to wait (in milliseconds) between initializing and reading the counter for more accuracy.</param>
        /// <returns>A float representing the list of counter's current reading.</returns>
        public static float GetReading(List<PerformanceCounter> gpuCounters, int time)
        {
            // This part shouldn't run if OS is not Windows
            if (ProgramSettings.CURRENT_OS != OS.Windows) return -1.0f;

            // Get initial reading for every counter in the list
            Parallel.ForEach(gpuCounters, x => TryGetMetric(x));
            Thread.Sleep(time);

            // Get the more accurate reading for every counter and sum them to get the total usage for this engine
            float result = 0;
            object lockObject = new object();
            Parallel.ForEach(gpuCounters, x =>
            {
                float metric = TryGetMetric(x);
                lock (lockObject)
                {
                    result += metric;
                }
            });

            return result;
        }

        /// <summary>
        /// Get NextValue from Performance Counter.
        /// </summary>
        /// <param name="x">Performance Counter which we want value from.</param>
        /// <returns>Float representing the value of the Performance Counter.</returns>
        private static float TryGetMetric(PerformanceCounter x)
        {
            try
            {
                return x.NextValue();
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"An error occurred: {ex.Message}");
                return 0; // Return a default value
            }
        }
    }

}
