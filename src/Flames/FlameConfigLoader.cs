using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Flames.Models;

namespace Flames
{
    /// <summary>
    /// Сервис загрузки и валидации конфигурации генератора (CLI, JSON, дефолты)
    /// </summary>
    public class FlameConfigLoader
    {
        public static FlameConfig LoadFromArgsOrJson(ParseResult pr,
            Option<int> widthOption,
            Option<int> heightOption,
            Option<double> seedOption,
            Option<int> iterOption,
            Option<int> threadsOption,
            Option<string> outputOption,
            Option<string> affineOption,
            Option<string> functionsOption,
            Option<string> configOption,
            Option<bool> gammaCorrOption,
            Option<double> gammaOption,
            Option<int> symmetryOption)
        {
            string configPath = pr.GetValueForOption(configOption);
            FlameConfig config = new FlameConfig();
            if (!string.IsNullOrWhiteSpace(configPath) && File.Exists(configPath))
            {
                var fileJson = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<FlameConfig>(fileJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            }

            // Перезаписываем CLI-значениями, если они явно есть
            if (pr.FindResultFor(widthOption) != null)
            {
                config.Width = pr.GetValueForOption(widthOption);
            }

            if (pr.FindResultFor(heightOption) != null)
            {
                config.Height = pr.GetValueForOption(heightOption);
            }

            if (pr.FindResultFor(seedOption) != null)
            {
                config.Seed = pr.GetValueForOption(seedOption);
            }

            if (pr.FindResultFor(iterOption) != null)
            {
                config.IterationCount = pr.GetValueForOption(iterOption);
            }

            if (pr.FindResultFor(threadsOption) != null)
            {
                config.Threads = pr.GetValueForOption(threadsOption);
            }

            if (pr.FindResultFor(outputOption) != null && !string.IsNullOrWhiteSpace(pr.GetValueForOption(outputOption)))
            {
                config.OutputPath = pr.GetValueForOption(outputOption);
            }

            if (pr.FindResultFor(affineOption) != null && !string.IsNullOrWhiteSpace(pr.GetValueForOption(affineOption)))
            {
                config.AffineParams = AffineParamsParser.Parse(pr.GetValueForOption(affineOption));
            }

            if (pr.FindResultFor(functionsOption) != null && !string.IsNullOrWhiteSpace(pr.GetValueForOption(functionsOption)))
            {
                config.Functions = TransformationFunctionParser.Parse(pr.GetValueForOption(functionsOption));
            }

            if (pr.FindResultFor(gammaCorrOption) != null)
            {
                config.GammaCorrection = pr.GetValueForOption(gammaCorrOption);
            }

            if (pr.FindResultFor(gammaOption) != null)
            {
                config.Gamma = pr.GetValueForOption(gammaOption);
            }

            if (pr.FindResultFor(symmetryOption) != null)
            {
                config.SymmetryLevel = pr.GetValueForOption(symmetryOption);
            }

            if (config.Functions.Count == 0)
            {
                config.Functions.Add(new TransformationFunction("linear", 1.0));
            }

            if (config.AffineParams.Count == 0)
            {
                config.AffineParams.Add(FlameConfig.DEFAULT_AFFINE);
            }

            return config;
        }

        public static void Validate(FlameConfig conf)
        {
            if (conf.Width <= 0 || conf.Height <= 0)
            {
                throw new ArgumentException("Размер изображения должен быть больше 0");
            }

            if (conf.IterationCount <= 0)
            {
                throw new ArgumentException("Число итераций должно быть больше 0");
            }

            if (conf.Threads <= 0)
            {
                throw new ArgumentException("Число потоков должно быть больше 0");
            }

            if (conf.SymmetryLevel < 1)
            {
                throw new ArgumentException("SymmetryLevel >= 1");
            }

            if (conf.Functions.Count == 0)
            {
                throw new ArgumentException("Должна быть указана хотя бы одна функция трансформации");
            }

            if (conf.AffineParams.Count == 0)
            {
                throw new ArgumentException("Должна быть указана хотя бы одна аффинная матрица");
            }
        }
    }

    public static class AffineParamsParser
    {
        public static System.Collections.Generic.List<AffineParams> Parse(string s)
        {
            var list = new System.Collections.Generic.List<AffineParams>();
            var parts = s.Split('/');
            foreach (var p in parts)
            {
                var arr = p.Split(',');
                if (arr.Length != 6)
                {
                    throw new FormatException("Ошибка формата affine-параметров: требуется 6 чисел через запятую");
                }

                list.Add(new AffineParams(
                    double.Parse(arr[0], CultureInfo.InvariantCulture),
                    double.Parse(arr[1], CultureInfo.InvariantCulture),
                    double.Parse(arr[2], CultureInfo.InvariantCulture),
                    double.Parse(arr[3], CultureInfo.InvariantCulture),
                    double.Parse(arr[4], CultureInfo.InvariantCulture),
                    double.Parse(arr[5], CultureInfo.InvariantCulture)
                ));
            }
            return list;
        }
    }

    public static class TransformationFunctionParser
    {
        public static System.Collections.Generic.List<TransformationFunction> Parse(string s)
        {
            var res = new System.Collections.Generic.List<TransformationFunction>();
            var fns = s.Split(',');
            foreach (var fn in fns)
            {
                var split = fn.Split(':');
                if (split.Length != 2)
                {
                    throw new FormatException("Неверный формат функции (ожидалось <name>:<weight>)");
                }

                res.Add(new TransformationFunction(split[0], double.Parse(split[1], CultureInfo.InvariantCulture)));
            }
            return res;
        }
    }
}


