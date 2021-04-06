using BenchmarkDotNet.Attributes;

namespace Benchmarks.Benchmarks
{
    [DisassemblyDiagnoser]
    public class JpegBenchmark
    {
        [Benchmark]
        public void Test1MbImage()
        {
            JPEG.Program.Main(new []{"sample.bmp"});
        }
        
        [Benchmark]
        public void Test3MbImage()
        {
            JPEG.Program.Main(new []{"marbles.bmp"});
        }
        
        /*[Benchmark]
        public void Test300MbImage()
        {
            JPEG.Program.Main(new []{"earth.bmp"});
        }*/
    }
}