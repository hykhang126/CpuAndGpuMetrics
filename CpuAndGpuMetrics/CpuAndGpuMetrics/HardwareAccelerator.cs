﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CpuAndGpuMetrics
{
    /// <summary>
    /// Represents a hardware accelerator.
    /// </summary>
    public class HardwareAccelerator
    {
        /// <summary>Gets or sets the GPU type.</summary>
        public GpuType? Gpu { get; set; }

        /// <summary>Gets or sets the hardware acceleration type.</summary>
        public HardwareAccel HardwareAccel { get; set; }

        /// <summary>Container for video and performance metric pairings relating to the specific hardware accelerator.</summary>
        private Dictionary<Video, PerformanceMetricsContainer> map;

        /// <summary>
        /// Initializes a HardwareAccelerator object.
        /// </summary>
        /// <param name="hardwareAccel"></param>
        /// <param name="gpu"></param>
        public HardwareAccelerator(HardwareAccel hardwareAccel, GpuType gpu)
        {
            this.HardwareAccel = hardwareAccel;
            this.Gpu = gpu;
        }

        /// <summary>
        /// Chooses the appropriate hardware acceleration types based on the given GPU type.
        /// </summary>
        /// <param name="gpu">The GPU type, which can be null.</param>
        /// <returns>An array representing the compatible hardware acceleration types of the given Gpu type.</returns>
        public static HardwareAccel[] HardwareAcceleratorChooser(GpuType? gpu)
        {
            HardwareAccel[] HardwareAccels = gpu switch
            {
                GpuType.Nvidia => new[] { HardwareAccel.Cuda, HardwareAccel.D3D11VA, HardwareAccel.Vulkan, HardwareAccel.None },
                GpuType.Intel => new[] { HardwareAccel.QSV, HardwareAccel.D3D11VA, HardwareAccel.Vulkan, HardwareAccel.VAAPI, HardwareAccel.None },
                _ => new[] { HardwareAccel.None },
            };
            return HardwareAccels;
        }

        public void AddPair(Video video, PerformanceMetricsContainer counter)
        {
            map.Add(video, counter);
        }

        public void ToExcel()
        {
            //
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
