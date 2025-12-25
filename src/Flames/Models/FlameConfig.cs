using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Flames.Models;

/// <summary>
/// Полная конфигурация генератора фрактальных пламён, поддерживает извлечение параметров из CLI/JSON/дефолтов
/// </summary>
public class FlameConfig
{
    [JsonPropertyName("width")]
    public int Width { get; set; } = 1920;
    [JsonPropertyName("height")]
    public int Height { get; set; } = 1080;
    [JsonPropertyName("seed")]
    public double Seed { get; set; } = 5;
    [JsonPropertyName("iteration_count")]
    public int IterationCount { get; set; } = 2500;
    [JsonPropertyName("output_path")]
    public string OutputPath { get; set; } = "result.png";
    [JsonPropertyName("threads")]
    public int Threads { get; set; } = 1;
    [JsonPropertyName("affine_params")]
    public List<AffineParams> AffineParams { get; set; } = new();
    [JsonPropertyName("functions")]
    public List<TransformationFunction> Functions { get; set; } = new();
    [JsonPropertyName("gamma_correction")]
    public bool GammaCorrection { get; set; } = false;
    [JsonPropertyName("gamma")]
    public double Gamma { get; set; } = 2.2;
    [JsonPropertyName("symmetry_level")]
    public int SymmetryLevel { get; set; } = 1;

    /// <summary>
    /// Создаёт FlameConfig из ParseResult и Option'ов (всё парсинг-детали спрятаны здесь)
    /// </summary>
    public static FlameConfig FromParseResult(
        ParseResult pr,
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
        // CLI > JSON > дефолты
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
            config.AffineParams = ParseAffineParams(pr.GetValueForOption(affineOption));
        }

        if (pr.FindResultFor(functionsOption) != null && !string.IsNullOrWhiteSpace(pr.GetValueForOption(functionsOption)))
        {
            config.Functions = ParseFunctions(pr.GetValueForOption(functionsOption));
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
        // Добавить дефолты если списки пусты
        if (config.Functions.Count == 0)
        {
            config.Functions.Add(new TransformationFunction("linear", 1.0));
        }

        if (config.AffineParams.Count == 0)
        {
            config.AffineParams.Add(new AffineParams(0.8, -0.2, 0.1, 0.2, 0.8, -0.1));
        }

        return config;
    }

    private static List<AffineParams> ParseAffineParams(string s)
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

    private static List<TransformationFunction> ParseFunctions(string s)
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
}
