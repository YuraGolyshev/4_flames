using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Flames.Models;

/// <summary>
/// Полная конфигурация генератора фрактальных пламён, поддерживаемая через CLI или JSON
/// </summary>
public class FlameConfig
{
    /// <summary>Ширина итогового изображения</summary>
    [JsonPropertyName("width")]
    public int Width { get; set; } = 1920;
    /// <summary>Высота итогового изображения</summary>
    [JsonPropertyName("height")]
    public int Height { get; set; } = 1080;
    /// <summary>Seed генератора случайных чисел</summary>
    [JsonPropertyName("seed")]
    public double Seed { get; set; } = 5;
    /// <summary>Число итераций</summary>
    [JsonPropertyName("iteration_count")]
    public int IterationCount { get; set; } = 2500;
    /// <summary>Путь сохранения PNG</summary>
    [JsonPropertyName("output_path")]
    public string OutputPath { get; set; } = "result.png";
    /// <summary>Количество потоков (1 = однопоточный режим)</summary>
    [JsonPropertyName("threads")]
    public int Threads { get; set; } = 1;
    /// <summary>Список параметров для affine-преобразований</summary>
    [JsonPropertyName("affine_params")]
    public List<AffineParams> AffineParams { get; set; } = new();
    /// <summary>Список нелинейных функций (name + weight)</summary>
    [JsonPropertyName("functions")]
    public List<TransformationFunction> Functions { get; set; } = new();
    /// <summary>Включить гамма-коррекцию (log+gamma)</summary>
    [JsonPropertyName("gamma_correction")]
    public bool GammaCorrection { get; set; } = false;
    /// <summary>Значение гаммы</summary>
    [JsonPropertyName("gamma")]
    public double Gamma { get; set; } = 2.2;
    /// <summary>Число осей симметрии, >=1</summary>
    [JsonPropertyName("symmetry_level")]
    public int SymmetryLevel { get; set; } = 1;
}

