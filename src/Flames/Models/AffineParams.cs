using System.Text.Json.Serialization;

namespace Flames.Models;

/// <summary>
/// Параметры одной аффинной матрицы для chaos game: X'=AX+BY+C, Y'=DX+EY+F
/// </summary>
public record AffineParams(
    [property: JsonPropertyName("a")] double A,
    [property: JsonPropertyName("b")] double B,
    [property: JsonPropertyName("c")] double C,
    [property: JsonPropertyName("d")] double D,
    [property: JsonPropertyName("e")] double E,
    [property: JsonPropertyName("f")] double F
);

