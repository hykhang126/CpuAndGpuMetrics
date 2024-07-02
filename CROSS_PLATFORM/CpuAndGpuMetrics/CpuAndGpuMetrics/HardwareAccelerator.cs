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
        /// <param name="hardwareAccel">Hardware Acceleration method.</param>
        /// <param name="gpu">GPU type/brand.</param>
        /// <param name="isDecodeAccel">Boolean if we're doing hardware Decoding.</param>
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

            // Hardware Acceleration changes depending on the OS (VAAPI needs to be added to Windows later)
            if (ProgramSettings.CURRENT_OS == OS.Linux)
            {
                if (ProgramSettings.IS_DECODE_ONLY_ON)
                {
                    //Checks to see if user wants to do software decode/Encode
                    HardwareAccels = ProgramSettings.IS_SOFTWARE_ONLY_ON switch
                    {
                        true => new[] { HardwareAccel.None },
                        _ => gpu switch
                        {
                            GpuType.Nvidia => new[] { HardwareAccel.Cuda, HardwareAccel.VDPAU, HardwareAccel.Vulkan, HardwareAccel.None },
                            GpuType.Intel => new[] { HardwareAccel.QSV, HardwareAccel.VAAPI, HardwareAccel.Vulkan, HardwareAccel.None },
                            _ => new[] { HardwareAccel.None },
                        }
                    };
                }
                else
                {
                    HardwareAccels = ProgramSettings.IS_SOFTWARE_ONLY_ON switch
                    {
                        true => new[] { HardwareAccel.None },
                        _ => gpu switch
                        {
                            GpuType.Nvidia => new[] { HardwareAccel.Cuda, HardwareAccel.Vulkan, HardwareAccel.None },
                            GpuType.Intel => new[] { HardwareAccel.QSV, HardwareAccel.VAAPI, HardwareAccel.None },
                            _ => new[] { HardwareAccel.None },
                        }
                    };
                }
            }
            else if (ProgramSettings.CURRENT_OS == OS.Windows)
            {

                if (ProgramSettings.IS_DECODE_ONLY_ON)
                {
                    HardwareAccels = ProgramSettings.IS_SOFTWARE_ONLY_ON switch
                    {
                        true => new[] { HardwareAccel.None },
                       
                        _ => gpu switch
                        {
                            GpuType.Nvidia => [HardwareAccel.Cuda,
                            HardwareAccel.D3D11VA,
                            HardwareAccel.Vulkan,
                            HardwareAccel.None,
                            HardwareAccel.D3D12VA],
                            GpuType.Intel => [HardwareAccel.QSV, HardwareAccel.D3D11VA, HardwareAccel.Vulkan, HardwareAccel.D3D12VA, HardwareAccel.None],
                            GpuType.Unknown => [HardwareAccel.None],
                            _ => [HardwareAccel.None],
                        }
                    };

                }
                else
                {
                    HardwareAccels = ProgramSettings.IS_SOFTWARE_ONLY_ON switch
                    {
                        true => new[] { HardwareAccel.None },
                        
                        _ => gpu switch
                        {
                            GpuType.Nvidia => [HardwareAccel.Cuda, HardwareAccel.None],
                            GpuType.Intel => [HardwareAccel.QSV, HardwareAccel.None],
                            GpuType.Unknown => [HardwareAccel.None],
                            _ => [HardwareAccel.None],
                        }
                    };
                }

            }
            else HardwareAccels = [HardwareAccel.None];

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
        D3D12VA = 8
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
