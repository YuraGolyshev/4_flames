using System;
using Flames.Models;
using Flames.Utils;
using System.Collections.Generic;

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
            RenderCore(buf, config, weights, sum, new Random((int)config.Seed), 0, n, (cur, total) => Logger.Instance.Progress(cur, n));
            Logger.Instance.Progress(n, n);
            Console.WriteLine();
            return NormalizeToRgb(buf, w, h);
        }

        private void RenderCore(double[] buf, FlameConfig config, List<double> weights, double sumWeights, Random rand,
            int startIter, int endIter, Action<int, int>? progressCb = null)
        {
            const double XMIN = -4.0, XMAX = 4.0, YMIN = -4.0, YMAX = 4.0, COORD_LIMIT = 10.0;
            int w = config.Width, h = config.Height;
            double x = 0, y = 0;
            int n = endIter - startIter;
            for (int iter = 0; iter < n; iter++)
            {
                int idx = PickFunction(weights, sumWeights, rand);
                int affIdx = rand.Next(config.AffineParams.Count);
                var aff = config.AffineParams[affIdx];
                (x, y) = ApplyAffine(x, y, aff);
                (x, y) = ApplyTransform(x, y, config.Functions[idx].Name);
                if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
                {
                    x = 0; y = 0;
                    continue;
                }
                x = Math.Max(-COORD_LIMIT, Math.Min(COORD_LIMIT, x));
                y = Math.Max(-COORD_LIMIT, Math.Min(COORD_LIMIT, y));
                int symLevels = Math.Max(1, config.SymmetryLevel);
                for (int s = 0; s < symLevels; s++)
                {
                    double angle = 2 * Math.PI * s / symLevels;
                    double xx = x * Math.Cos(angle) - y * Math.Sin(angle);
                    double yy = x * Math.Sin(angle) + y * Math.Cos(angle);
                    int px = (int)((xx - XMIN) / (XMAX - XMIN) * (w - 1));
                    int py = (int)((YMAX - yy) / (YMAX - YMIN) * (h - 1));
                    if (px >= 0 && px < w && py >= 0 && py < h)
                    {
                        var color = FunctionColor(idx, config.Functions.Count);
                        int idxBuf = (py * w + px) * 3;
                        buf[idxBuf + 0] += color.r;
                        buf[idxBuf + 1] += color.g;
                        buf[idxBuf + 2] += color.b;
                    }
                }
                if (progressCb != null && (iter + 1) % Math.Max(1, n / 20) == 0)
                {
                    progressCb(startIter + iter + 1, startIter + n);
                }
            }
            progressCb?.Invoke(startIter + n, startIter + n);
        }

        private int PickFunction(List<double> acc, double sum, Random rand)
        {
            double r = rand.NextDouble() * sum;
            for (int i = 0; i < acc.Count; i++)
            {
                if (r < acc[i])
                {
                    return i;
                }
            }

            return acc.Count - 1;
        }

        private (double, double) ApplyAffine(double x, double y, AffineParams t)
            => (t.A * x + t.B * y + t.C, t.D * x + t.E * y + t.F);

        private (double, double) ApplyTransform(double x, double y, string name)
            => name.ToLower() switch
            {
                "linear" => FlameTransforms.Linear(x, y),
                "swirl" => FlameTransforms.Swirl(x, y),
                "horseshoe" => FlameTransforms.Horseshoe(x, y),
                "spherical" => FlameTransforms.Spherical(x, y),
                "sinusoidal" => FlameTransforms.Sinusoidal(x, y),
                "polar" => FlameTransforms.Polar(x, y),
                _ => FlameTransforms.Linear(x, y)
            };
    }
}


