using System;
using System.Collections.Generic;
using Flames.Models;
using Flames.Utils;

namespace Flames.Render;

/// <summary>
/// Основной однопоточный движок генерации фрактального пламени (chaos game + nonlinear transforms)
/// </summary>
public class FlameRenderer
{
    private readonly FlameConfig config;
    private readonly Random rand;
    public FlameRenderer(FlameConfig cfg)
    {
        config = cfg;
        rand = new Random((int)config.Seed);
    }

    /// <summary>
    /// Генерирует финальное RGB-изображение в виде byte[] по config.
    /// </summary>
    public byte[] Render()
    {
        int w = config.Width, h = config.Height, n = config.IterationCount;
        var buf = new double[w * h * 3]; // Для накопления цвета
        double xmin = -4.0, xmax = 4.0, ymin = -4.0, ymax = 4.0;
        var weights = new List<double>();
        double sum = 0;
        foreach (var f in config.Functions) { sum += f.Weight; weights.Add(sum); }
        // Используем общий core-рендер
        FlameRenderCore.RenderCore(buf, config, weights, sum, rand, xmin, xmax, ymin, ymax, 0, n, (cur, total) => Logger.Progress(cur, n));
        Logger.Progress(n, n); Console.WriteLine();
        return NormalizeToRgb(buf, w, h);
    }

    private int PickFunction(List<double> acc, double sum)
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

    private (double r, double g, double b) FunctionColor(int i, int total)
    {
        // Яркие насыщенные цвета для каждой функции - полный спектр радуги
        double hue = (double)i / total * 6.0; // 0-6 для полного спектра
        int sector = (int)hue;
        double frac = hue - sector;
        return sector switch
        {
            0 => (1.0, frac, 0.0),           // Красный -> Жёлтый
            1 => (1.0 - frac, 1.0, 0.0),     // Жёлтый -> Зелёный
            2 => (0.0, 1.0, frac),            // Зелёный -> Голубой
            3 => (0.0, 1.0 - frac, 1.0),      // Голубой -> Синий
            4 => (frac, 0.0, 1.0),            // Синий -> Фиолетовый
            _ => (1.0, 0.0, 1.0 - frac)       // Фиолетовый -> Красный
        };
    }
    private byte[] NormalizeToRgb(double[] buf, int w, int h)
    {
        double max = 1;
        foreach (var c in buf)
        {
            if (c > max)
            {
                max = c;
            }
        }

        double logMax = Math.Log(max + 1);
        var arr = new byte[w * h * 3];
        for (int i = 0; i < buf.Length; i++)
        {
            double normalized = Math.Log(buf[i] + 1) / logMax;
            if (config.GammaCorrection)
            {
                double gamma = config.Gamma > 0 ? config.Gamma : 2.2;
                normalized = Math.Pow(normalized, 1.0 / gamma);
            }
            arr[i] = (byte)Math.Min(255, (int)(normalized * 255));
        }
        return arr;
    }
}

