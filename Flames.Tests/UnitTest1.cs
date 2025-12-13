using Flames;
using Flames.Models;
using Xunit;

namespace Flames.Tests;

public class ConfigTests
{
    [Fact]
    public void ParseAffineParams_ValidStr_Works()
    {
        var str = "1,2,3,4,5,6/0.1,0.2,0.3,0.4,0.5,0.6";
        var list = Program.ParseAffineParams(str);
        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0].A);
        Assert.Equal(0.4, list[1].D);
    }

    [Fact]
    public void ParseAffineParams_InvalidStr_Fails()
    {
        Assert.Throws<FormatException>(() => Program.ParseAffineParams("1,2,3,4,5"));
    }

    [Fact]
    public void ParseFunctions_ValidStr_Works()
    {
        var f = Program.ParseFunctions("swirl:1.0,horseshoe:0.8");
        Assert.Equal(2, f.Count);
        Assert.Equal("swirl", f[0].Name);
        Assert.Equal(0.8, f[1].Weight);
    }

    [Fact]
    public void ParseFunctions_InvalidStr_Fails()
    {
        Assert.Throws<FormatException>(() => Program.ParseFunctions("swirl"));
    }

    [Fact]
    public void DeserializeJsonConfig_Works()
    {
        var json = @"{
  ""width"": 800,
  ""height"": 600,
  ""iteration_count"": 1111,
  ""output_path"": ""t.png"",
  ""threads"": 2,
  ""seed"": 32,
  ""functions"": [{""name"": ""swirl"", ""weight"": 0.5}],
  ""affine_params"": [{""a"": 1, ""b"": 2, ""c"": 3, ""d"": 4, ""e"": 5, ""f"": 6}]
}";
        var config = System.Text.Json.JsonSerializer.Deserialize<FlameConfig>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(config);
        Assert.Equal(800, config.Width);
        Assert.Equal(600, config.Height);
        Assert.Equal(1111, config.IterationCount);
        Assert.Single(config.Functions);
        Assert.Single(config.AffineParams);
    }

    [Fact]
    public void Validation_Works_Ok()
    {
        var c = new FlameConfig
        {
            Width = 600,
            Height = 600,
            IterationCount = 1000,
            Threads = 1,
            Functions = new() { new TransformationFunction("swirl", 1) },
            AffineParams = new() { new AffineParams(1, 2, 3, 4, 5, 6) },
            SymmetryLevel = 1
        };
        Program.ValidateConfig(c);
    }

    [Fact]
    public void Validation_BadParams_Throws()
    {
        var bad = new FlameConfig();
        Assert.Throws<ArgumentException>(() => Program.ValidateConfig(bad));
        bad.Width = 100; bad.Height = 100; bad.IterationCount = 10; bad.Threads = 1;
        bad.Functions = new(); bad.AffineParams = new(); bad.SymmetryLevel = 1;
        Assert.Throws<ArgumentException>(() => Program.ValidateConfig(bad));
        bad.Functions = new() { new TransformationFunction("swirl", 1) };
        Assert.Throws<ArgumentException>(() => Program.ValidateConfig(bad));
        bad.AffineParams = new() { new AffineParams(1, 1, 1, 1, 1, 1) };
        bad.Threads = 0;
        Assert.Throws<ArgumentException>(() => Program.ValidateConfig(bad));
        bad.Threads = 1; bad.SymmetryLevel = 0;
        Assert.Throws<ArgumentException>(() => Program.ValidateConfig(bad));
    }
}

