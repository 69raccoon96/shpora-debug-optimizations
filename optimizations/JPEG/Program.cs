using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JPEG.Images;

namespace JPEG
{
    public static class Program
    {
        const int CompressionQuality = 70;

        public static void Main(string[] args)
        {
            //args = new[] {"earth.bmp"};
                
            DCT.PreCulc(DCTSize, DCTSize);
            DCT.PreCulc2(DCTSize, DCTSize);
            try
            {
                Console.WriteLine(IntPtr.Size == 8 ? "64-bit version" : "32-bit version");
                var sw = Stopwatch.StartNew();
                var fileName = args[0];
//				var fileName = "Big_Black_River_Railroad_Bridge.bmp";
                var compressedFileName = fileName + ".compressed." + CompressionQuality;
                var uncompressedFileName = fileName + ".uncompressed." + CompressionQuality + ".bmp";

                using (var fileStream = File.OpenRead(fileName))
                using (var bmp = (Bitmap) Image.FromStream(fileStream, false, false))
                {
                    
                    var imageMatrix = Matrix.ConvertToMatrix(bmp);

                    sw.Stop();
                    Console.WriteLine($"{bmp.Width}x{bmp.Height} - {fileStream.Length / (1024.0 * 1024):F2} MB");
                    sw.Start();

                    var compressionResult = Compress(imageMatrix, CompressionQuality);
                    compressionResult.Save(compressedFileName);
                }

                sw.Stop();
                Console.WriteLine("Compression: " + sw.Elapsed);
                sw.Restart();
                var compressedImage = CompressedImage.Load(compressedFileName);
                var uncompressedImage = Uncompress(compressedImage);
                var resultBmp = Matrix.ConvertToBmp(uncompressedImage);
                resultBmp.Save(uncompressedFileName, ImageFormat.Bmp);
                Console.WriteLine("Decompression: " + sw.Elapsed);
                Console.WriteLine($"Peak commit size: {MemoryMeter.PeakPrivateBytes() / (1024.0 * 1024):F2} MB");
                Console.WriteLine($"Peak working set: {MemoryMeter.PeakWorkingSet() / (1024.0 * 1024):F2} MB");
            }

            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            //Console.ReadKey();
        }

        private static CompressedImage Compress(Matrix matrix, int quality = 50)
        {
            var b = new List<double[,]> {matrix.YPixels, matrix.CbPixels, matrix.CrPixels};
            var preQuantizeMatrix = GetQuantizationMatrix(quality);

            var allQuantizedBytes = b
                .AsParallel()
                .AsOrdered()
                .Select(selector =>
                {
                    var current = new List<byte>();
                    for (var y = 0; y < matrix.Height; y += DCTSize)
                    {
                        for (var x = 0; x < matrix.Width; x += DCTSize)
                        {
                            var subMatrix = GetSubMatrix(y, DCTSize, x, DCTSize, selector);
                            var channelFreqs = DCT.DCT2D(subMatrix);
                            var quantizedFreqs = new byte[DCTSize, DCTSize];
                            for (var y2 = 0; y2 < DCTSize; y2++)
                            {
                                for (var x2 = 0; x2 < DCTSize; x2++)
                                {
                                    quantizedFreqs[y2, x2] = (byte) (channelFreqs[y2, x2] / preQuantizeMatrix[y2, x2]);
                                }
                            }

                            current.AddRange(ZigZagScan(quantizedFreqs));
                        }
                    }

                    return current;
                })
                .AsSequential()
                .SelectMany(z => z)
                .ToList();


            var compressedBytes = HuffmanCodec.Encode(allQuantizedBytes, out var decodeTable, out var bitsCount);

            return new CompressedImage
            {
                Quality = quality, CompressedBytes = compressedBytes, BitsCount = bitsCount, DecodeTable = decodeTable,
                Height = matrix.Height, Width = matrix.Width
            };
        }

        


        private static Matrix Uncompress(CompressedImage image)
        {
            var result = new Matrix(image.Height, image.Width);
            var quantizationMatrix = GetQuantizationMatrix(image.Quality);
            var decode = HuffmanCodec.Decode(image.CompressedBytes, image.DecodeTable, image.BitsCount);
            var list = new[]
            {
                result.YPixels, result.CbPixels, result.CrPixels
            };

            Parallel.For((long) 0, 3, z =>
            {
                var channel = list[z];
                var allQuantizedBytes = new MemoryStream(decode) {Position = z * result.Height * result.Width};
                for (var y = 0; y < image.Height; y += DCTSize)
                {
                    for (var x = 0; x < image.Width; x += DCTSize)
                    {
                        {
                            var input = new double[DCTSize, DCTSize];
                            var quantizedBytes = new byte[DCTSize * DCTSize];
                            allQuantizedBytes.Read(quantizedBytes, 0, quantizedBytes.Length);
                            var quantizedFreqs = ZigZagUnScan(quantizedBytes);
                            var channelFreqs = new double[DCTSize, DCTSize];
                            for (var i = 0; i < DCTSize; i++)
                            {
                                for (var j = 0; j < DCTSize; j++)
                                    channelFreqs[i, j] = ((sbyte) quantizedFreqs[i, j]) *
                                                         quantizationMatrix[i, j];
                            }

                            DCT.IDCT2D(channelFreqs, input);
                            for (var i = 0; i < DCTSize; i++)
                            {
                                for (var j = 0; j < DCTSize; j++)
                                    channel[y + i, x + j] = input[i, j];
                            }
                        }
                    }
                }

                allQuantizedBytes.Dispose();
            });


            return result;
        }

        private static double[,] GetSubMatrix(int yOffset, int yLength, int xOffset, int xLength,
            double[,] componentSelector)
        {
            var result = new double[yLength, xLength];
            for (var j = 0; j < yLength; j++)
            for (var i = 0; i < xLength; i++)
                result[j, i] = componentSelector[yOffset + j, xOffset + i] - 128;
            return result;
        }

        private static IEnumerable<byte> ZigZagScan(byte[,] channelFreqs)
        {
            return new[]
            {
                channelFreqs[0, 0], channelFreqs[0, 1], channelFreqs[1, 0], channelFreqs[2, 0], channelFreqs[1, 1],
                channelFreqs[0, 2], channelFreqs[0, 3], channelFreqs[1, 2],
                channelFreqs[2, 1], channelFreqs[3, 0], channelFreqs[4, 0], channelFreqs[3, 1], channelFreqs[2, 2],
                channelFreqs[1, 3], channelFreqs[0, 4], channelFreqs[0, 5],
                channelFreqs[1, 4], channelFreqs[2, 3], channelFreqs[3, 2], channelFreqs[4, 1], channelFreqs[5, 0],
                channelFreqs[6, 0], channelFreqs[5, 1], channelFreqs[4, 2],
                channelFreqs[3, 3], channelFreqs[2, 4], channelFreqs[1, 5], channelFreqs[0, 6], channelFreqs[0, 7],
                channelFreqs[1, 6], channelFreqs[2, 5], channelFreqs[3, 4],
                channelFreqs[4, 3], channelFreqs[5, 2], channelFreqs[6, 1], channelFreqs[7, 0], channelFreqs[7, 1],
                channelFreqs[6, 2], channelFreqs[5, 3], channelFreqs[4, 4],
                channelFreqs[3, 5], channelFreqs[2, 6], channelFreqs[1, 7], channelFreqs[2, 7], channelFreqs[3, 6],
                channelFreqs[4, 5], channelFreqs[5, 4], channelFreqs[6, 3],
                channelFreqs[7, 2], channelFreqs[7, 3], channelFreqs[6, 4], channelFreqs[5, 5], channelFreqs[4, 6],
                channelFreqs[3, 7], channelFreqs[4, 7], channelFreqs[5, 6],
                channelFreqs[6, 5], channelFreqs[7, 4], channelFreqs[7, 5], channelFreqs[6, 6], channelFreqs[5, 7],
                channelFreqs[6, 7], channelFreqs[7, 6], channelFreqs[7, 7]
            };
        }

        private static byte[,] ZigZagUnScan(IReadOnlyList<byte> quantizedBytes)
        {
            return new[,]
            {
                {
                    quantizedBytes[0], quantizedBytes[1], quantizedBytes[5], quantizedBytes[6], quantizedBytes[14],
                    quantizedBytes[15], quantizedBytes[27], quantizedBytes[28]
                },
                {
                    quantizedBytes[2], quantizedBytes[4], quantizedBytes[7], quantizedBytes[13], quantizedBytes[16],
                    quantizedBytes[26], quantizedBytes[29], quantizedBytes[42]
                },
                {
                    quantizedBytes[3], quantizedBytes[8], quantizedBytes[12], quantizedBytes[17], quantizedBytes[25],
                    quantizedBytes[30], quantizedBytes[41], quantizedBytes[43]
                },
                {
                    quantizedBytes[9], quantizedBytes[11], quantizedBytes[18], quantizedBytes[24], quantizedBytes[31],
                    quantizedBytes[40], quantizedBytes[44], quantizedBytes[53]
                },
                {
                    quantizedBytes[10], quantizedBytes[19], quantizedBytes[23], quantizedBytes[32], quantizedBytes[39],
                    quantizedBytes[45], quantizedBytes[52], quantizedBytes[54]
                },
                {
                    quantizedBytes[20], quantizedBytes[22], quantizedBytes[33], quantizedBytes[38], quantizedBytes[46],
                    quantizedBytes[51], quantizedBytes[55], quantizedBytes[60]
                },
                {
                    quantizedBytes[21], quantizedBytes[34], quantizedBytes[37], quantizedBytes[47], quantizedBytes[50],
                    quantizedBytes[56], quantizedBytes[59], quantizedBytes[61]
                },
                {
                    quantizedBytes[35], quantizedBytes[36], quantizedBytes[48], quantizedBytes[49], quantizedBytes[57],
                    quantizedBytes[58], quantizedBytes[62], quantizedBytes[63]
                }
            };
        }


        private static int[,] GetQuantizationMatrix(int quality)
        {
            if (quality < 1 || quality > 99)
                throw new ArgumentException("quality must be in [1,99] interval");

            var multiplier = quality < 50 ? 5000 / quality : 200 - 2 * quality;

            var result = new[,]
            {
                {16, 11, 10, 16, 24, 40, 51, 61},
                {12, 12, 14, 19, 26, 58, 60, 55},
                {14, 13, 16, 24, 40, 57, 69, 56},
                {14, 17, 22, 29, 51, 87, 80, 62},
                {18, 22, 37, 56, 68, 109, 103, 77},
                {24, 35, 55, 64, 81, 104, 113, 92},
                {49, 64, 78, 87, 103, 121, 120, 101},
                {72, 92, 95, 98, 112, 100, 103, 99}
            };

            for (var y = 0; y < result.GetLength(0); y++)
            {
                for (var x = 0; x < result.GetLength(1); x++)
                {
                    result[y, x] = (multiplier * result[y, x] + 50) / 100;
                }
            }

            return result;
        }

        const int DCTSize = 8;
    }
}