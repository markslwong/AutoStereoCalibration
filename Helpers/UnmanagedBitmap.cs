using System;
using System.Drawing;
using System.Drawing.Imaging;


namespace AutoStereoCalibration
{
    public unsafe class UnmanagedBitmap : IDisposable
    {
        private readonly Bitmap  _bitmap;

        private int              _stride;
        private BitmapData       _bitmapData;
        private Byte*            _bitmapDataBase;
        
        public UnmanagedBitmap(int width, int height)
        {
            _bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            Initialize();
            Lock();
        }
        
        public UnmanagedBitmap(Bitmap bitmap)
        {
            _bitmap = new Bitmap(bitmap);

            Initialize();
            Lock();
        }

        private void Initialize()
        {
            _stride = _bitmap.Width * sizeof(PixelData);

            // Enforce that stride is a 4-byte aligned
            if (_stride % 4 != 0)
            {
                _stride = 4 * (_stride / 4 + 1);
            }
        }

        private void Lock()
        {
            _bitmapData = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            _bitmapDataBase = (Byte*)_bitmapData.Scan0.ToPointer();
        }

        private void Unlock()
        {
            _bitmap.UnlockBits(_bitmapData);
            _bitmapData = null;
            _bitmapDataBase = null;
        }
        
        public void Dispose()
        {
            Unlock();

            _bitmap.Dispose();
        }

        public Bitmap SnapShot()
        {
            Unlock();

            var snapshot = new Bitmap(_bitmap);

            Lock();

            return snapshot;
        }
        
        public PixelData GetPixel(int x, int y)
        {
            return *GetPixelInternal(x, y);
        }

        public void SetPixel(int x, int y, PixelData colour)
        {
            var pixel = GetPixelInternal(x, y);

            *pixel = colour;
        }

        private PixelData* GetPixelInternal(int x, int y)
        {
            return (PixelData*)(_bitmapDataBase + y * _stride + x * sizeof(PixelData));
        }
    }
}
