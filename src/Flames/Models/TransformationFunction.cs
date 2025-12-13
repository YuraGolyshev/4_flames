using System.Text.Json.Serialization;

namespace Flames.Models;

public record TransformationFunction(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("weight")] double Weight
);

