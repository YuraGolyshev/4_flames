using System;
using System.Collections.Generic;
using Flames.Models;

namespace Flames.Render;

/// <summary>
/// Общая core-реализация расчёта фрактального пламени
/// </summary>
public static class FlameRenderCore
{
    private const double COORD_LIMIT = 10.0;        // Ограничение области для выбросов x/y
    private const int PROGRESS_LOG_DIV = 20;        // Логировать прогресс каждые 5% итераций (n/20)
    private const double COLOR_RING = 6.0;          // Для расчёта цветов спектра (6 цветов)

    /// <summary>
    /// Основной цикл итераций рендера
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
            x = Math.Max(-COORD_LIMIT, Math.Min(COORD_LIMIT, x));
            y = Math.Max(-COORD_LIMIT, Math.Min(COORD_LIMIT, y));
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
            if (progressCb != null && (iter + 1) % Math.Max(1, n / PROGRESS_LOG_DIV) == 0)
            {
                progressCb(startIter + iter + 1, startIter + n);
            }
        }
        if (progressCb != null)
        {
            progressCb(startIter + n, startIter + n);
        }
    }

    /// <summary>
    /// Готовит кумулятивные веса для трансформационных функций.
    /// Возвращает список кумулятивных весов (для PickFunction) и сумму весов.
    /// </summary>
    public static (List<double> weights, double sum) PrepareWeights(List<TransformationFunction> functions)
    {
        var weights = new List<double>();
        double sum = 0;
        foreach (var f in functions)
        {
            sum += f.Weight;
            weights.Add(sum);
        }
        return (weights, sum);
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
        double hue = (double)i / total * COLOR_RING;
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
    public const double DEFAULT_XMIN = -4.0;
    public const double DEFAULT_XMAX = 4.0;
    public const double DEFAULT_YMIN = -4.0;
    public const double DEFAULT_YMAX = 4.0;


}
