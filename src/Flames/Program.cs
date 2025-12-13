// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flames.Models;
using Flames.Render;
using Flames.Utils;

namespace Flames;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var rootCommand = new RootCommand("Генерация фрактального пламени");
            var widthOption = new Option<int>(new[] {"--width", "-w"}, ()=>1920, "Ширина изображения");
            var heightOption = new Option<int>(new[] {"--height", "-h"}, ()=>1080, "Высота изображения");
            var seedOption = new Option<double>("--seed", ()=>5, "Seed генератора");
            var iterOption = new Option<int>(new[] {"--iteration-count", "-i"}, ()=>2500,"Число итераций");
            var threadsOption = new Option<int>(new[] {"--threads", "-t"}, ()=>1, "Кол-во потоков");
            var outOption = new Option<string>(new[] {"--output-path", "-o"}, ()=>"result.png", "Путь PNG");
            var affOption = new Option<string>(new[]{"--affine-params","-ap"},"Список аффинных преобразований");
            var funOption = new Option<string>(new[]{"--functions","-f"},"Список трансформаций");
            var configOption = new Option<string>("--config","Путь к JSON-конфигу");
            var gammaCorrOption = new Option<bool>(new[]{"--gamma-correction","-g"},"Включить гамма-коррекцию");
            var gammaOption = new Option<double>("--gamma",()=>2.2, "Значение гаммы");
            var symmetryOption = new Option<int>(new[]{"--symmetry-level","-s"},()=>1,"Кол-во симметрий");

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
                int width = context.ParseResult.GetValueForOption(widthOption);
                int height = context.ParseResult.GetValueForOption(heightOption);
                double seed = context.ParseResult.GetValueForOption(seedOption);
                int iterationCount = context.ParseResult.GetValueForOption(iterOption);
                int threads = context.ParseResult.GetValueForOption(threadsOption);
                string output = context.ParseResult.GetValueForOption(outOption);
                string affineParams = context.ParseResult.GetValueForOption(affOption);
                string functions = context.ParseResult.GetValueForOption(funOption);
                string configPath = context.ParseResult.GetValueForOption(configOption);
                bool gammaCorrection = context.ParseResult.GetValueForOption(gammaCorrOption);
                double gamma = context.ParseResult.GetValueForOption(gammaOption);
                int symmetryLevel = context.ParseResult.GetValueForOption(symmetryOption);
                try
                {
                    var config = LoadConfig(width, height, seed, iterationCount, threads, output, affineParams, functions, configPath, gammaCorrection, gamma, symmetryLevel);
                    ValidateConfig(config);
                    Logger.Info($"Параметры загружены. width={config.Width}, height={config.Height}, iters={config.IterationCount}");
                    var renderer = new FlameRenderer(config);
                    var rgb = renderer.Render();
                    PngWriter.SaveRgbImage(config.OutputPath, config.Width, config.Height, rgb);
                    Logger.Info($"PNG сохранён в {config.OutputPath}");
                } catch (Exception ex) { Logger.Error(ex.Message); }
            });
            return rootCommand.Invoke(args);
        }
        catch (Exception ex)
        {
            Logger.Error(ex.Message+"\n"+ex.StackTrace);
            return 1;
        }
    }
    public static FlameConfig LoadConfig(int width, int height, double seed, int iter, int threads, string output, string affStr, string funStr, string configPath, bool gammaCorr, double gamma, int symmetry)
    {
        FlameConfig config = new FlameConfig();
        if (!string.IsNullOrWhiteSpace(configPath) && File.Exists(configPath))
        {
            var fileJson = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<FlameConfig>(fileJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
        // CLI > JSON > дефолты
        config.Width = width;
        config.Height = height;
        config.Seed = seed;
        config.IterationCount = iter;
        config.Threads = threads;
        config.OutputPath = output;
        config.GammaCorrection = gammaCorr;
        config.Gamma = gamma;
        config.SymmetryLevel = symmetry;
        if (!string.IsNullOrWhiteSpace(affStr))
            config.AffineParams = ParseAffineParams(affStr);
        if (!string.IsNullOrWhiteSpace(funStr))
            config.Functions = ParseFunctions(funStr);
        return config;
    }
    public static List<AffineParams> ParseAffineParams(string s)
    {
        var list = new List<AffineParams>();
        var parts = s.Split('/');
        foreach (var p in parts)
        {
            var arr = p.Split(',');
            if (arr.Length != 6) throw new FormatException("Ошибка формата affine-параметров: требуется 6 чисел через запятую");
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
    public static List<TransformationFunction> ParseFunctions(string s)
    {
        var res = new List<TransformationFunction>();
        var fns = s.Split(',');
        foreach (var fn in fns)
        {
            var split = fn.Split(':');
            if (split.Length != 2) throw new FormatException("Неверный формат функции (ожидалось <name>:<weight>)");
            res.Add(new TransformationFunction(split[0], double.Parse(split[1], CultureInfo.InvariantCulture)));
        }
        return res;
    }
    public static void ValidateConfig(FlameConfig conf)
    {
        if (conf.Width <= 0 || conf.Height <= 0) throw new ArgumentException("Размер изображения должен быть больше 0");
        if (conf.IterationCount <= 0) throw new ArgumentException("Число итераций должно быть больше 0");
        if (conf.Threads <= 0) throw new ArgumentException("Число потоков должно быть больше 0");
        if (conf.SymmetryLevel < 1) throw new ArgumentException("SymmetryLevel >= 1");
        if (conf.Functions.Count == 0) throw new ArgumentException("Должна быть указана хотя бы одна функция трансформации");
        if (conf.AffineParams.Count == 0) throw new ArgumentException("Должна быть указана хотя бы одна аффинная матрица");
    }
}
