using System.Text.Json.Serialization;

namespace Flames.Models;

/// <summary>
/// Описание одной нелинейной функции/вариации для chaos game: имя и вес
/// </summary>
public record TransformationFunction(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("weight")] double Weight
);

