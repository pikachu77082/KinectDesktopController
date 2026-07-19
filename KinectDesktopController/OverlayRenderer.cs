using Microsoft.Kinect;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectDesktopController
{
    public class OverlayRenderer
    {
        private WriteableBitmap _bitmap;
        private uint[] _pixels;
        private readonly int _width;
        private readonly int _height;

        // BGRA format colors
        private const uint ColorTransparent = 0x00000000;
        private const uint ColorShadow = 0x80FFFFFF; // 50% transparent white

        public WriteableBitmap Bitmap => _bitmap;

        public OverlayRenderer(int width, int height)
        {
            _width = width;
            _height = height;
            _bitmap = new WriteableBitmap(width, height, 96.0, 96.0, PixelFormats.Bgra32, null);
            _pixels = new uint[width * height];
        }

        public unsafe void RenderBodyIndex(BodyIndexFrame frame, int masterUserIndex)
        {
            if (frame == null) return;

            using (KinectBuffer buffer = frame.LockImageBuffer())
            {
                byte* pData = (byte*)buffer.UnderlyingBuffer;
                int length = (int)buffer.Size;

                for (int i = 0; i < length; i++)
                {
                    byte player = pData[i];
                    if (player != 255 && player == masterUserIndex)
                    {
                        _pixels[i] = ColorShadow;
                    }
                    else
                    {
                        _pixels[i] = ColorTransparent;
                    }
                }
            }

            _bitmap.Lock();
            IntPtr pBackBuffer = _bitmap.BackBuffer;
            
            fixed (uint* pPixels = _pixels)
            {
                Buffer.MemoryCopy(pPixels, pBackBuffer.ToPointer(), _bitmap.BackBufferStride * _height, _pixels.Length * sizeof(uint));
            }
            
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
            _bitmap.Unlock();
        }
    }
}
