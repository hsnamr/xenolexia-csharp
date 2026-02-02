using System.Runtime.InteropServices;

namespace Xenolexia.Core.Services;

/// <summary>
/// P/Invoke to xenolexia-shared-c SM-2 for identical behaviour with Obj-C.
/// Requires libxenolexia_sm2.so (Linux), libxenolexia_sm2.dylib (macOS), or xenolexia_sm2.dll (Windows) in path or next to the app.
/// </summary>
internal static class Sm2Native
{
    private const string LibName = "xenolexia_sm2";

    [StructLayout(LayoutKind.Sequential)]
    internal struct XenolexiaSm2State
    {
        public double ease_factor;
        public int interval;
        public int review_count;
        public int status; // xenolexia_sm2_status_t
    }

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern void xenolexia_sm2_step(int quality, ref XenolexiaSm2State state);

    /// <summary>
    /// Returns true if the native library is available and the step was performed.
    /// </summary>
    public static bool TryStep(int quality, ref double easeFactor, ref int interval, ref int reviewCount, ref int status)
    {
        try
        {
            var state = new XenolexiaSm2State
            {
                ease_factor = easeFactor,
                interval = interval,
                review_count = reviewCount,
                status = status
            };
            xenolexia_sm2_step(quality, ref state);
            easeFactor = state.ease_factor;
            interval = state.interval;
            reviewCount = state.review_count;
            status = state.status;
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }
}
