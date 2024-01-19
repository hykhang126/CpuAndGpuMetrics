

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Represents a hardware accelerator.
    /// </summary>
    public class HardwareAccelerator
    {
        /// <summary>Gets or sets the GPU type.</summary>
        public GpuType Gpu { get; set; }

        /// <summary>Gets or sets the hardware acceleration type.</summary>
        public HardwareAccel HardwareAccel { get; set; }

        /// <summary>Gets or sets the boolean to use hw acceleration or not.</summary>
        public static bool IsDecodeAccel { get; set; }

        /// <summary>
        /// Initializes a HardwareAccelerator object.
        /// </summary>
        /// <param name="hardwareAccel"></param>
        /// <param name="gpu"></param>
        public HardwareAccelerator(HardwareAccel hardwareAccel, GpuType gpu, bool isDecodeAccel = false)
        {
            this.HardwareAccel = hardwareAccel;
            this.Gpu = gpu;
            HardwareAccelerator.IsDecodeAccel = isDecodeAccel;
        }

        /// <summary>
        /// Chooses the appropriate hardware acceleration types based on the given GPU type.
        /// </summary>
        /// <param name="gpu">The GPU type, which can be null.</param>
        /// <returns>An array representing the compatible hardware acceleration types of the given Gpu type.</returns>
        public static HardwareAccel[] HardwareAcceleratorChooser(GpuType? gpu)
        {
            HardwareAccel[] HardwareAccels;

            if (ProgramSettings.CURRENT_OS == OS.Windows)
            {
                HardwareAccels = gpu switch
                {
                    GpuType.Nvidia => new[] { HardwareAccel.Cuda, HardwareAccel.D3D11VA, HardwareAccel.Vulkan, HardwareAccel.None },
                    GpuType.Intel => new[] { HardwareAccel.QSV, HardwareAccel.D3D11VA, HardwareAccel.Vulkan, HardwareAccel.VAAPI, HardwareAccel.None },
                    _ => new[] { HardwareAccel.None },
                };
            }
            else // if (ProgramSettings.CURRENT_OS == OS.Linux)
            {
                HardwareAccels = gpu switch
                {
                    GpuType.Nvidia => [HardwareAccel.Cuda, HardwareAccel.VDPAU, HardwareAccel.Vulkan, HardwareAccel.None],
                    GpuType.Intel => [HardwareAccel.QSV, HardwareAccel.VAAPI, HardwareAccel.Vulkan, HardwareAccel.None],
                    _ => [HardwareAccel.None],
                };
            }
            
            // Other OS in the future?

            return HardwareAccels;
        }

    }

    /// <summary>
    /// Possible hardware acceleration types.
    /// </summary>
    public enum HardwareAccel
    {
        Unknown = 0,
        None = 1,
        Cuda = 2,
        QSV = 3,
        D3D11VA = 4,
        Vulkan = 5,
        VAAPI = 6,
        VDPAU = 7,
    }

    /// <summary>
    /// Possible Gpu types.
    /// </summary>
    public enum GpuType
    {
        Unknown = 0,
        Nvidia = 1,
        Intel = 2,
    }
}
