// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flames.Models;
using Flames.Render;
using Flames.Utils;

namespace Flames;

/// <summary>
/// Входная точка и парсер параметров генератора фрактальных пламён
/// </summary>
public static class Program
{
    /// <summary>Главная точка входа. Парсит параметры CLI/JSON, управляет рендером и PNG-выходом.</summary>
    public static int Main(string[] args)
    {
        try
        {
            var rootCommand = new RootCommand("Генерация фрактального пламени");
            var widthOption = new Option<int>(new[] { "--width", "-w" }, () => 1920, "Ширина изображения");
            var heightOption = new Option<int>(new[] { "--height", "-h" }, () => 1080, "Высота изображения");
            var seedOption = new Option<double>("--seed", () => 5, "Seed генератора");
            var iterOption = new Option<int>(new[] { "--iteration-count", "-i" }, () => 2500, "Число итераций");
            var threadsOption = new Option<int>(new[] { "--threads", "-t" }, () => 1, "Кол-во потоков");
            var outOption = new Option<string>(new[] { "--output-path", "-o" }, () => "result.png", "Путь PNG");
            var affOption = new Option<string>(new[] { "--affine-params", "-ap" }, "Список аффинных преобразований");
            var funOption = new Option<string>(new[] { "--functions", "-f" }, "Список трансформаций");
            var configOption = new Option<string>("--config", "Путь к JSON-конфигу");
            var gammaCorrOption = new Option<bool>(new[] { "--gamma-correction", "-g" }, "Включить гамма-коррекцию");
            var gammaOption = new Option<double>("--gamma", () => 2.2, "Значение гаммы");
            var symmetryOption = new Option<int>(new[] { "--symmetry-level", "-s" }, () => 1, "Кол-во симметрий");

            rootCommand.AddOption(widthOption);
            rootCommand.AddOption(heightOption);
            rootCommand.AddOption(seedOption);
            rootCommand.AddOption(iterOption);
            rootCommand.AddOption(threadsOption);
            rootCommand.AddOption(outOption);
            rootCommand.AddOption(affOption);
            rootCommand.AddOption(funOption);
            rootCommand.AddOption(configOption);
            rootCommand.AddOption(gammaCorrOption);
            rootCommand.AddOption(gammaOption);
            rootCommand.AddOption(symmetryOption);

            rootCommand.SetHandler((context) =>
            {
                var parseResult = context.ParseResult;
                // Получаем значения и сравниваем с дефолтами, чтобы определить, были ли они указаны явно
                int widthVal = parseResult.GetValueForOption(widthOption);
                int heightVal = parseResult.GetValueForOption(heightOption);
                double seedVal = parseResult.GetValueForOption(seedOption);
                int iterVal = parseResult.GetValueForOption(iterOption);
                int threadsVal = parseResult.GetValueForOption(threadsOption);
                string outputVal = parseResult.GetValueForOption(outOption);
                string affineParamsVal = parseResult.GetValueForOption(affOption);
                string functionsVal = parseResult.GetValueForOption(funOption);
                string configPath = parseResult.GetValueForOption(configOption);
                bool gammaCorrVal = parseResult.GetValueForOption(gammaCorrOption);
                double gammaVal = parseResult.GetValueForOption(gammaOption);
                int symmetryVal = parseResult.GetValueForOption(symmetryOption);

                // Проверяем, был ли указан --gamma-correction в командной строке
                bool gammaCorrectionSpecified = parseResult.Tokens.Any(t => t.Value == "--gamma-correction" || t.Value == "-g");

                // Определяем, какие параметры были указаны явно (отличаются от дефолтов или не null для строк)
                int? width = (widthVal != 1920) ? widthVal : null;
                int? height = (heightVal != 1080) ? heightVal : null;
                double? seed = (seedVal != 5.0) ? seedVal : null;
                int? iterationCount = (iterVal != 2500) ? iterVal : null;
                int? threads = (threadsVal != 1) ? threadsVal : null;
                string output = (!string.IsNullOrWhiteSpace(outputVal) && outputVal != "result.png") ? outputVal : null;
                string affineParams = !string.IsNullOrWhiteSpace(affineParamsVal) ? affineParamsVal : null;
                string functions = !string.IsNullOrWhiteSpace(functionsVal) ? functionsVal : null;
                bool? gammaCorrection = gammaCorrectionSpecified ? (bool?)gammaCorrVal : null;
                double? gamma = (gammaVal != 2.2) ? gammaVal : null;
                int? symmetryLevel = (symmetryVal != 1) ? symmetryVal : null;
                try
                {
                    var config = LoadConfig(width, height, seed, iterationCount, threads, output, affineParams, functions, configPath, gammaCorrection, gamma, symmetryLevel);
                    ValidateConfig(config);
                    Logger.Instance.Info($"Parameters loaded. width={config.Width}, height={config.Height}, iters={config.IterationCount}, threads={config.Threads}");

                    byte[] rgb;
                    if (config.Threads > 1)
                    {
                        var multiRenderer = new MultiThreadedFlameRenderer(config);
                        rgb = multiRenderer.Render();
                    }
                    else
                    {
                        var renderer = new FlameRenderer(config);
                        rgb = renderer.Render();
                    }

                    PngWriter.SaveRgbImage(config.OutputPath, config.Width, config.Height, rgb);
                    Logger.Instance.Info($"PNG saved to {config.OutputPath}");
                }
                catch (Exception ex) { Logger.Instance.Error(ex.Message); }
            });
            return rootCommand.Invoke(args);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex.Message + "\n" + ex.StackTrace);
            return 1;
        }
    }
    /// <summary>
    /// Собирает итоговый конфиг из параметров CLI, JSON или дефолтов
    /// </summary>
    public static FlameConfig LoadConfig(int? width, int? height, double? seed, int? iter, int? threads,
    string output, string affStr, string funStr, string configPath, bool? gammaCorr,
    double? gamma, int? symmetry)
    {
        FlameConfig config = new FlameConfig();
        if (!string.IsNullOrWhiteSpace(configPath) && File.Exists(configPath))
        {
            var fileJson = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<FlameConfig>(fileJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        // CLI > JSON > дефолты (перезаписываем только если параметр был явно указан в CLI)
        if (width.HasValue)
        {
            config.Width = width.Value;
        }

        if (height.HasValue)
        {
            config.Height = height.Value;
        }

        if (seed.HasValue)
        {
            config.Seed = seed.Value;
        }

        if (iter.HasValue)
        {
            config.IterationCount = iter.Value;
        }

        if (threads.HasValue)
        {
            config.Threads = threads.Value;
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            config.OutputPath = output;
        }

        if (gammaCorr.HasValue)
        {
            config.GammaCorrection = gammaCorr.Value;
        }

        if (gamma.HasValue)
        {
            config.Gamma = gamma.Value;
        }

        if (symmetry.HasValue)
        {
            config.SymmetryLevel = symmetry.Value;
        }

        if (!string.IsNullOrWhiteSpace(affStr))
        {
            config.AffineParams = ParseAffineParams(affStr);
        }

        if (!string.IsNullOrWhiteSpace(funStr))
        {
            config.Functions = ParseFunctions(funStr);
        }

        // Добавляем дефолтные значения, если списки пустые
        if (config.Functions.Count == 0)
        {
            config.Functions.Add(new TransformationFunction("linear", 1.0));
        }

        if (config.AffineParams.Count == 0)
        {
            // Дефолтное аффинное преобразование (единичная матрица со смещением)
            config.AffineParams.Add(new AffineParams(
                0.8, -0.2, 0.1,
                0.2, 0.8, -0.1
            ));
        }

        return config;
    }
    /// <summary>Парсит строку с affine-параметрами (по 6 чисел через /)</summary>
    public static List<AffineParams> ParseAffineParams(string s)
    {
        var list = new List<AffineParams>();
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
    /// <summary>Парсит функции трансформации вида "name:weight,name:weight..."</summary>
    public static List<TransformationFunction> ParseFunctions(string s)
    {
        var res = new List<TransformationFunction>();
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
    /// <summary>Проверяет консистентность/валидность всех параметров.</summary>
    public static void ValidateConfig(FlameConfig conf)
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
