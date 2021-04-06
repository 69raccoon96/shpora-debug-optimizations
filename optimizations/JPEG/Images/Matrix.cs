using System.Drawing;
using System.Drawing.Imaging;

namespace JPEG.Images
{
    class Matrix
    {
        public readonly int Height;
        public readonly int Width;
        public readonly double[,] YPixels;
        public readonly double[,] CbPixels;
        public readonly double[,] CrPixels;

        public Matrix(int height, int width)
        {
            Height = height;
            Width = width;
            YPixels = new double[height, width];
            CbPixels = new double[height, width];
            CrPixels = new double[height, width];
        }

        public static Bitmap ConvertToBmp(Matrix matrix)
        {
            var bmp = new Bitmap(matrix.Width, matrix.Height);
            var data = bmp.LockBits(new Rectangle(0, 0, matrix.Width, matrix.Height), ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);
            unsafe
            {
                var ptr = (byte*) data.Scan0;
                for (var y = 0; y < bmp.Height; y++)
                {
                    var ptr2 = ptr;
                    for (var x = 0; x < bmp.Width; x++)
                    {
                        var y1 = matrix.YPixels[y, x];
                        var cb = matrix.CbPixels[y, x];
                        var cr = matrix.CrPixels[y, x];
                        var r = (298.082 * y1 + 408.583 * cr) / 256.0 - 222.921;
                        var g = (298.082 * y1 - 100.291 * cb - 208.120 * cr) / 256.0 + 135.576;
                        var b = (298.082 * y1 + 516.412 * cb) / 256.0 - 276.836;
                        *(ptr2++) = (byte) ToByte(b);
                        *(ptr2++) = (byte) ToByte(g);
                        *(ptr2++) = (byte) ToByte(r);
                    }

                    ptr += data.Stride;
                }
            }

            bmp.UnlockBits(data);

            return bmp;
        }

        public static Matrix ConvertToMatrix(Bitmap bmp)
        {
            var height = bmp.Height - bmp.Height % 8;
            var width = bmp.Width - bmp.Width % 8;
            var matrix = new Matrix(height, width);
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);
            unsafe
            {
                var ptr = (byte*) data.Scan0;
                for (var y = 0; y < height; y++)
                {
                    var ptr2 = ptr;
                    for (var x = 0; x < width; x++)
                    {
                        var b = *(ptr2++);
                        var g = *(ptr2++);
                        var r = *(ptr2++);
                        matrix.CbPixels[y, x] = 128.0 + (-37.945 * r - 74.494 * g + 112.439 * b) / 256.0;
                        matrix.CrPixels[y, x] = 128.0 + (112.439 * r - 94.154 * g - 18.285 * b) / 256.0;
                        matrix.YPixels[y, x] = 16.0 + (65.738 * r + 129.057 * g + 24.064 * b) / 256.0;
                    }

                    ptr += data.Stride;
                }
            }

            bmp.UnlockBits(data);


            return matrix;
        }
        
        public static explicit operator Matrix(Bitmap bmp)
        {
            var height = bmp.Height - bmp.Height % 8;
            var width = bmp.Width - bmp.Width % 8;
            var matrix = new Matrix(height, width);
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);
            unsafe
            {
                var ptr = (byte*) data.Scan0;
                for (var y = 0; y < height; y++)
                {
                    var ptr2 = ptr;
                    for (var x = 0; x < width; x++)
                    {
                        var b = *(ptr2++);
                        var g = *(ptr2++);
                        var r = *(ptr2++);
                        matrix.CbPixels[y, x] = 128.0 + (-37.945 * r - 74.494 * g + 112.439 * b) / 256.0;
                        matrix.CrPixels[y, x] = 128.0 + (112.439 * r - 94.154 * g - 18.285 * b) / 256.0;
                        matrix.YPixels[y, x] = 16.0 + (65.738 * r + 129.057 * g + 24.064 * b) / 256.0;
                    }

                    ptr += data.Stride;
                }
            }

            bmp.UnlockBits(data);


            return matrix;
        }


        public static explicit operator Bitmap(Matrix matrix)
        {
            var bmp = new Bitmap(matrix.Width, matrix.Height);
            var data = bmp.LockBits(new Rectangle(0, 0, matrix.Width, matrix.Height), ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);
            unsafe
            {
                var ptr = (byte*) data.Scan0;
                for (var y = 0; y < bmp.Height; y++)
                {
                    var ptr2 = ptr;
                    for (var x = 0; x < bmp.Width; x++)
                    {
                        var y1 = matrix.YPixels[y, x];
                        var cb = matrix.CbPixels[y, x];
                        var cr = matrix.CrPixels[y, x];
                        var r = (298.082 * y1 + 408.583 * cr) / 256.0 - 222.921;
                        var g = (298.082 * y1 - 100.291 * cb - 208.120 * cr) / 256.0 + 135.576;
                        var b = (298.082 * y1 + 516.412 * cb) / 256.0 - 276.836;
                        *(ptr2++) = (byte) ToByte(b);
                        *(ptr2++) = (byte) ToByte(g);
                        *(ptr2++) = (byte) ToByte(r);
                    }

                    ptr += data.Stride;
                }
            }

            bmp.UnlockBits(data);

            return bmp;
        }

        private static int ToByte(double d)
        {
            var val = (int) d;
            if (val > byte.MaxValue)
                return byte.MaxValue;
            return val < byte.MinValue ? byte.MinValue : val;
        }
    }
}