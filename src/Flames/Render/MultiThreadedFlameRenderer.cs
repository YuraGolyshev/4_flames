using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flames.Models;
using Flames.Utils;

namespace Flames.Render
{
    public class MultiThreadedFlameRenderer : FlameRendererBase
    {
        public MultiThreadedFlameRenderer(FlameConfig config) : base(config) { }
        public override byte[] Render()
        {
            int w = config.Width, h = config.Height, n = config.IterationCount;
            int threads = Math.Max(1, config.Threads);
            var (weights, sum) = PrepareWeights();
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
                    RenderCore(localBuf, weights, sum, localRand, startIter, endIter, (cur, total) =>
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
    }
}
