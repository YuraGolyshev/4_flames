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
    // Весь парсинг/валидация вынесены во FlameConfigLoader, Program теперь только точка входа
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
                try
                {
                    var config = Flames.FlameConfigLoader.LoadFromArgsOrJson(
    parseResult,
    widthOption,
    heightOption,
    seedOption,
    iterOption,
    threadsOption,
    outOption,
    affOption,
    funOption,
    configOption,
    gammaCorrOption,
    gammaOption,
    symmetryOption
);
                    Flames.FlameConfigLoader.Validate(config);
                    Logger.Instance.Info($"Parameters loaded. width={config.Width}, height={config.Height}, iters={config.IterationCount}, threads={config.Threads}");

                    byte[] rgb;
                    IFlameRenderer renderer = (config.Threads > 1)
                        ? new MultiThreadedFlameRenderer(config)
                        : new SingleThreadedFlameRenderer(config);
                    rgb = renderer.Render();

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
    /// <summary>Парсит строку с affine-параметрами (по 6 чисел через /)</summary>
    /// <summary>Парсит функции трансформации вида "name:weight,name:weight..."</summary>
    /// <summary>Проверяет консистентность/валидность всех параметров.</summary>
}
