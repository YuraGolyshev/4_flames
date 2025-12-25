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
    // Координаты и пределы вынесены в константы
    private const double XMIN = FlameRenderCore.DEFAULT_XMIN;
    private const double XMAX = FlameRenderCore.DEFAULT_XMAX;
    private const double YMIN = FlameRenderCore.DEFAULT_YMIN;
    private const double YMAX = FlameRenderCore.DEFAULT_YMAX;
    public FlameRenderer(FlameConfig cfg)
    {
        config = cfg;
        rand = new Random((int)config.Seed);
    }
    public byte[] Render()
    {
        int w = config.Width, h = config.Height, n = config.IterationCount;
        var buf = new double[w * h * 3]; // Для накопления цвета
        var (weights, sum) = FlameRenderCore.PrepareWeights(config.Functions);
        FlameRenderCore.RenderCore(buf, config, weights, sum, rand, XMIN, XMAX, YMIN, YMAX, 0, n, (cur, total) => Logger.Instance.Progress(cur, n));
        Logger.Instance.Progress(n, n); Console.WriteLine();
        return NormalizeToRgb(buf, w, h);
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
                double gamma = config.Gamma > 0 ? config.Gamma : FlameConfig.DEFAULT_GAMMA;
                normalized = Math.Pow(normalized, 1.0 / gamma);
            }
            arr[i] = (byte)Math.Min(255, (int)(normalized * 255));
        }
        return arr;
    }
}
