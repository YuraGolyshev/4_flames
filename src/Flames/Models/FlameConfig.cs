using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Flames.Models;

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
}

