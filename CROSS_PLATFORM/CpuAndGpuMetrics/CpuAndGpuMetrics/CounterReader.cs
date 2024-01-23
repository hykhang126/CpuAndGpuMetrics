using System.Diagnostics;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Static class for reading performance metrics
    /// </summary>
    static class CounterReader
    {
        /// <summary>
        /// Gets the current reading from a PerformanceCounter.
        /// </summary>
        /// <param name="counter">The PerformanceCounter to read from.</param>
        /// <param name="time">The time to wait (in milliseconds) between initializing and reading the counter for more accuracy.</param>
        /// <returns>A float representing the counter's current reading.</returns>
        public static float GetReading(PerformanceCounter counter, int time)
        {
            if (ProgramSettings.CURRENT_OS != OS.Windows) return -1.0f;

            float value = 0;
            try
            {
                value = counter.NextValue(); // Call this to initialize the counter, this value will be inaccurate
                Thread.Sleep(time);  // Wait a second to get a more accurate reading
                value = counter.NextValue();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error retrieving performance counter - " + counter.CounterName + ": " + e.Message);
            }
            return value;

            // Example command
            //mobzmobz
        }
    }
}