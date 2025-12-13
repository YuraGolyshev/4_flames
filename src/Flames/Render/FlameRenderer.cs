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
        // Расширенный диапазон координат для лучшего заполнения (адаптивный)
        double xmin=-4.0, xmax=4.0, ymin=-4.0, ymax=4.0;

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
            // Выбираем случайное аффинное преобразование (независимо от функции)
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
            // Ограничиваем координаты разумными пределами
            x = Math.Max(-10, Math.Min(10, x));
            y = Math.Max(-10, Math.Min(10, y));

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
        => name.ToLower() switch
        {
            "linear"      => FlameTransforms.Linear(x, y),
            "swirl"       => FlameTransforms.Swirl(x, y),
            "horseshoe"   => FlameTransforms.Horseshoe(x, y),
            "spherical"   => FlameTransforms.Spherical(x, y),
            "sinusoidal"  => FlameTransforms.Sinusoidal(x, y),
            "polar"       => FlameTransforms.Polar(x, y),
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
        // Находим максимум
        double max = 1;
        foreach(var c in buf) if(c>max) max=c;
        
        // Логарифмическая нормализация для лучшего контраста и яркости
        double logMax = Math.Log(max + 1);
        var arr = new byte[w*h*3];
        for (int i=0;i<buf.Length;i++)
        {
            double normalized = Math.Log(buf[i] + 1) / logMax;
            arr[i] = (byte)Math.Min(255, (int)(normalized * 255));
        }
        return arr;
    }
}

