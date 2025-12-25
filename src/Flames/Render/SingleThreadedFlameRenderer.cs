using System;
using Flames.Models;
using Flames.Utils;
namespace Flames.Render
{
    public class SingleThreadedFlameRenderer : FlameRendererBase
    {
        public SingleThreadedFlameRenderer(FlameConfig config) : base(config) { }
        public override byte[] Render()
        {
            int w = config.Width, h = config.Height, n = config.IterationCount;
            var buf = new double[w * h * 3];
            var (weights, sum) = PrepareWeights();
            RenderCore(buf, weights, sum, new Random((int)config.Seed), 0, n, (cur, total) => Logger.Instance.Progress(cur, n));
            Logger.Instance.Progress(n, n);
            Console.WriteLine();
            return NormalizeToRgb(buf, w, h);
        }
    }
}
