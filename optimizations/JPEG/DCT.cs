using System;

namespace JPEG
{
    public static class DCT
    {
        private static double[,,,] _preCulcTable;
        private static double[,,,] _preCulcTable2;

        public static void PreCulc(int width, int height)
        {
            _preCulcTable = new double[width, height, width, height];
            var widthPi = Math.PI / (2 * width);
            var heightPi = Math.PI / (2 * height);
            for (var u = 0; u < width; u++)
            {
                for (var v = 0; v < height; v++)
                {
                    var calcU = u * widthPi;
                    var calcV = v * heightPi;
                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            _preCulcTable[u, v, y, x] =
                                Math.Cos((2d * y + 1d) * calcV) * Math.Cos((2d * x + 1d) * calcU);
                        }
                    }
                }
            }
        }

        public static void PreCulc2(int width, int height)
        {
            _preCulcTable2 = new double[width, height, width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var u = 0; u < height; u++)
                    {
                        for (var v = 0; v < width; v++)
                        {
                            var b = Math.Cos((2d * x + 1d) * u * Math.PI / (2 * width * Alpha(u) * Alpha(v)));
                            var c = Math.Cos((2d * y + 1d) * v * Math.PI / (2 * height));

                            _preCulcTable2[x, y, u, v] = b * c * Alpha(u) * Alpha(v);
                        }
                    }
                }
            }
        }

        public static double[,] DCT2D(double[,] input)
        {
            var height = input.GetLength(0);
            var width = input.GetLength(1);
            var coeffs = new double[width, height];
            var beta = 1d / width + 1d / height;
            for (var u = 0; u < width; u++)
            {
                for (var v = 0; v < height; v++)
                {
                    var alphaU = u != 0 ? 1 : 1 / Math.Sqrt(2);
                    var alphaV = v != 0 ? 1 : 1 / Math.Sqrt(2);
                    var sum = 0.0;
                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            sum += input[x, y] * _preCulcTable[u, v, y, x];
                        }
                    }

                    coeffs[u, v] = sum * beta * alphaU * alphaV;
                }
            }

            return coeffs;
        }


        public static void IDCT2D(double[,] coeffs, double[,] output)
        {
            var height = coeffs.GetLength(0);
            var width = coeffs.GetLength(1);
            var beta = 1d / width + 1d / height;
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var sum = 0.0;
                    for (var u = 0; u < height; u++)
                    {
                        for (var v = 0; v < width; v++)
                        {
                            sum += coeffs[u, v] * _preCulcTable2[x, y, u, v];
                        }
                    }

                    output[x, y] = sum * beta + 128;
                }
            }
        }

        private static double Alpha(int u)
        {
            if (u == 0)
                return 1 / Math.Sqrt(2);
            return 1;
        }
    }
}