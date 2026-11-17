using UnityEngine;

namespace Zmywak.Core.Graphics
{
    /// <summary>
    /// The graphics tier a phone starts on, from what it can tell about itself: its memory, its GPU's memory and shader
    /// model, its processor count and how many pixels its screen has to fill. A budget phone starts on Low and a mid-range
    /// one on Medium instead of everyone starting on High and stuttering until the governor steps down. Pure - the device's
    /// numbers go in - so it is tested without one. Anything that is not a phone or tablet (the editor, a desktop) is High.
    /// </summary>
    public static class DeviceProfile
    {
        /// <summary>The tier to start on.</summary>
        public static QualityTier StartingTier(bool handheld, int memoryMb, int graphicsMemoryMb, int shaderLevel, int processorCount,
            int screenPixels)
        {
            if (!handheld)
            {
                return QualityTier.High;
            }

            // Old or budget hardware: under 3 GB, an old shader model, or four cores or fewer.
            if (memoryMb > 0 && memoryMb < 3000 || shaderLevel > 0 && shaderLevel < 45 || processorCount > 0 && processorCount <= 4)
            {
                return QualityTier.Low;
            }

            // Mid-range: under 6 GB, a small GPU memory pool, or a very large screen on less than 8 GB.
            if (memoryMb < 5500 || graphicsMemoryMb > 0 && graphicsMemoryMb < 1024 || screenPixels > 3200000 && memoryMb < 8000)
            {
                return QualityTier.Medium;
            }

            return QualityTier.High;
        }

        /// <summary>The tier for this device.</summary>
        public static QualityTier ForThisDevice() =>
            StartingTier(Application.isMobilePlatform, SystemInfo.systemMemorySize, SystemInfo.graphicsMemorySize,
                SystemInfo.graphicsShaderLevel, SystemInfo.processorCount, Screen.width * Screen.height);

        /// <summary>How sharp the dirt on a dish is drawn (texels per side): Medium three quarters, Low half as many each way.</summary>
        public static int DirtResolution(QualityTier tier) => tier == QualityTier.Low ? 128 : tier == QualityTier.Medium ? 192 : 256;
    }
}
