using System;
using System.Collections.Generic;
using Flames.Models;

namespace Flames.Render;

/// <summary>
/// Общая core-реализация вычисления набора итераций фрактального пламени: может использоваться и для single-thread, и для multi-thread
/// </summary>
public static class FlameRenderCore
{
    /// <summary>
    /// Вычисляет точки и накапливает значения цвета в переданный буфер buf
    /// </summary>
    public static void RenderCore(
        double[] buf,
        FlameConfig config,
        List<double> weights,
        double sumWeights,
        Random rand,
        double xmin,
        double xmax,
        double ymin,
        double ymax,
        int startIter,
        int endIter,
        Action<int, int>? progressCb = null)
    {
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
            // Защита от NaN и Infinity
            if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
            {
                x = 0; y = 0;
                continue;
            }
            x = Math.Max(-10, Math.Min(10, x));
            y = Math.Max(-10, Math.Min(10, y));
            int symLevels = Math.Max(1, config.SymmetryLevel);
            for (int s = 0; s < symLevels; s++)
            {
                double angle = 2 * Math.PI * s / symLevels;
                double xx = x * Math.Cos(angle) - y * Math.Sin(angle);
                double yy = x * Math.Sin(angle) + y * Math.Cos(angle);
                int px = (int)((xx - xmin) / (xmax - xmin) * (w - 1));
                int py = (int)((ymax - yy) / (ymax - ymin) * (h - 1));
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
        if (progressCb != null)
        {
            progressCb(startIter + n, startIter + n);
        }
    }

    private static int PickFunction(List<double> acc, double sum, Random rand)
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

    private static (double, double) ApplyAffine(double x, double y, AffineParams t)
        => (t.A * x + t.B * y + t.C, t.D * x + t.E * y + t.F);

    private static (double, double) ApplyTransform(double x, double y, string name)
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

    private static (double r, double g, double b) FunctionColor(int i, int total)
    {
        double hue = (double)i / total * 6.0;
        int sector = (int)hue;
        double frac = hue - sector;
        return sector switch
        {
            0 => (1.0, frac, 0.0),
            1 => (1.0 - frac, 1.0, 0.0),
            2 => (0.0, 1.0, frac),
            3 => (0.0, 1.0 - frac, 1.0),
            4 => (frac, 0.0, 1.0),
            _ => (1.0, 0.0, 1.0 - frac)
        };
    }
}

