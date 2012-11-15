using System.Runtime.InteropServices;


namespace AutoStereoCalibration
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PixelData
    {
        public byte B;
        public byte G;
        public byte R;
    }
}
