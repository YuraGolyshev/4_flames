using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flames.Models;
using Flames.Utils;

namespace Flames.Render;

/// <summary>
/// Многопоточный рендерер фрактального пламени, равномерно делит работу между потоками
/// </summary>
public class MultiThreadedFlameRenderer
{
    private readonly FlameConfig config;
    // Координаты/границы вынесены в константы
    private const double XMIN = FlameRenderCore.DEFAULT_XMIN;
    private const double XMAX = FlameRenderCore.DEFAULT_XMAX;
    private const double YMIN = FlameRenderCore.DEFAULT_YMIN;
    private const double YMAX = FlameRenderCore.DEFAULT_YMAX;

    public MultiThreadedFlameRenderer(FlameConfig cfg)
    {
        config = cfg;
    }
    public byte[] Render()
    {
        int w = config.Width, h = config.Height, n = config.IterationCount;
        int threads = Math.Max(1, config.Threads);
        var (weights, sum) = FlameRenderCore.PrepareWeights(config.Functions);
        var globalBuf = new double[w * h * 3];
        var lockObj = new object();
        int completed = 0;
        Logger.Instance.Info($"Starting multithreaded generation with {threads} threads");
        var tasks = new Task[threads];
        int iterationsPerThread = n / threads;
        int remainder = n % threads;
        for (int t = 0; t < threads; t++)
        {
            int threadId = t;
            int startIter = t * iterationsPerThread;
            int endIter = startIter + iterationsPerThread + (threadId == threads - 1 ? remainder : 0);
            int seed = (int)(config.Seed + threadId);
            tasks[t] = Task.Run(() =>
            {
                var localBuf = new double[w * h * 3];
                var localRand = new Random(seed);
                FlameRenderCore.RenderCore(localBuf, config, weights, sum, localRand, XMIN, XMAX, YMIN, YMAX, startIter, endIter, (cur, total) =>
                {
                    int prog = Interlocked.Increment(ref completed);
                    if (prog % (n / 100) == 0)
                    {
                        Logger.Instance.Progress(prog, n);
                    }
                });
                lock (lockObj)
                {
                    for (int i = 0; i < localBuf.Length; i++)
                    {
                        globalBuf[i] += localBuf[i];
                    }
                }
            });
        }
        Task.WaitAll(tasks);
        Logger.Instance.Progress(n, n);
        Console.WriteLine();
        Logger.Instance.Info($"Multithreaded generation completed");
        return NormalizeToRgb(globalBuf, w, h);
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
