using System;
using System.Collections.Generic;
using Flames.Models;
using Flames.Utils;

namespace Flames.Render
{
    public abstract class FlameRendererBase : IFlameRenderer
    {
        protected readonly FlameConfig config;

        protected FlameRendererBase(FlameConfig config)
        {
            this.config = config;
        }

        public abstract byte[] Render();

        protected (List<double> weights, double sum) PrepareWeights()
        {
            var weights = new List<double>();
            double sum = 0;
            foreach (var f in config.Functions)
            {
                sum += f.Weight;
                weights.Add(sum);
            }
            return (weights, sum);
        }

        protected byte[] NormalizeToRgb(double[] buf, int w, int h)
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

        protected (double r, double g, double b) FunctionColor(int i, int total)
        {
            const double COLOR_RING = 6.0;
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
    }
}


