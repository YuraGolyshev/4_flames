using System;
using System.Collections.Generic;
using Flames.Models;
using Flames.Utils;

namespace Flames.Render;

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
    /// Генерирует изображение фрактального пламени в byte[] RGB (row-major, 8 бит/канал)
    /// </summary>
    public byte[] Render()
    {
        int w = config.Width, h = config.Height, n = config.IterationCount;
        var buf = new double[w * h * 3]; // Для накопления цвета
        double xmin=-1.5, xmax=1.5, ymin=-1.0, ymax=1.0;

        // Сгенерируем кумулятивный массив весов для трансформаций
        var weights = new List<double>();
        double sum = 0;
        foreach (var f in config.Functions) { sum += f.Weight; weights.Add(sum); }
        // Стартовая точка
        double x = 0, y = 0;
        for (int i = 0; i < n; i++)
        {
            // Выбор функции по весам
            int idx = PickFunction(weights, sum);
            var aff = config.AffineParams[idx % config.AffineParams.Count];
            (x, y) = ApplyAffine(x, y, aff);
            (x, y) = ApplyTransform(x, y, config.Functions[idx].Name);

            // Приводим к экрану
            int px = (int)((x - xmin) / (xmax - xmin) * (w - 1));
            int py = (int)((ymax - y) / (ymax - ymin) * (h - 1)); // важен ymax - y, иначе перевёрнуто
            if (px >= 0 && px < w && py >= 0 && py < h)
            {
                // Цвет: индекс функции задаёт градиент (можно усложнить до colormap)
                var color = FunctionColor(idx, config.Functions.Count);
                int idxBuf = (py * w + px) * 3;
                buf[idxBuf + 0] += color.r;
                buf[idxBuf + 1] += color.g;
                buf[idxBuf + 2] += color.b;
            }
            if ((i+1) % Math.Max(1, n/100) == 0) Logger.Progress(i+1, n);
        }
        Logger.Progress(n, n); Console.WriteLine();
        // Преобразуем буфер double => byte
        return NormalizeToRgb(buf, w, h);
    }

    private int PickFunction(List<double> acc, double sum)
    {
        double r = rand.NextDouble() * sum;
        for (int i = 0; i < acc.Count; i++)
            if (r < acc[i]) return i;
        return acc.Count-1;
    }

    private (double, double) ApplyAffine(double x, double y, AffineParams t)
        => (t.A * x + t.B * y + t.C, t.D * x + t.E * y + t.F);

    private (double, double) ApplyTransform(double x, double y, string name)
    {
        // (Сделаем только linear для второго этапа, остальные трансформации позже.)
        return name switch {
            "linear" => (x, y),
            // для следующих этапов -- "swirl", "horseshoe" и т.д.
            _ => (x, y)
        };
    }
    private (double r, double g, double b) FunctionColor(int i, int total)
    {
        // Простой градиент по id: от красного к синему
        double t = (double)i/(total-1);
        return (1.0-t, 0.5*t, t); // R-G-B градиент
    }
    private byte[] NormalizeToRgb(double[] buf, int w, int h)
    {
        // Скалируем так, чтобы максимальный канал был 255
        double max = 1;
        foreach(var c in buf) if(c>max) max=c;
        var arr = new byte[w*h*3];
        for (int i=0;i<buf.Length;i++)
            arr[i]=(byte)Math.Min(255,(int)(buf[i]/max*255));
        return arr;
    }
}

