using Xunit;
using Flames.Render;

namespace Flames.Tests;

public class TransformTests
{
    [Fact]
    public void Linear_IsIdentity()
    {
        var (x, y) = FlameTransforms.Linear(1.0, 2.0);
        Assert.Equal(1.0, x, 5);
        Assert.Equal(2.0, y, 5);
    }
    [Fact]
    public void Swirl_ChangesCoordinates()
    {
        var (x, y) = FlameTransforms.Swirl(1.0, 0.0);
        Assert.NotEqual(1.0, x, 5);
        Assert.NotEqual(0.0, y, 5);
    }
    [Fact]
    public void Horseshoe_Works()
    {
        var (x, y) = FlameTransforms.Horseshoe(1.0, 2.0);
        Assert.False(double.IsNaN(x));
        Assert.False(double.IsNaN(y));
    }
    [Fact]
    public void Spherical_Works()
    {
        var (x, y) = FlameTransforms.Spherical(1.0, 2.0);
        Assert.False(double.IsInfinity(x));
        Assert.False(double.IsInfinity(y));
    }
    [Fact]
    public void Sinusoidal_Works()
    {
        var (x, y) = FlameTransforms.Sinusoidal(1.0, 2.0);
        Assert.Equal(System.Math.Sin(1.0), x, 5);
        Assert.Equal(System.Math.Sin(2.0), y, 5);
    }
    [Fact]
    public void Polar_Works()
    {
        var (x, y) = FlameTransforms.Polar(1.0, 2.0);
        Assert.False(double.IsNaN(x));
        Assert.False(double.IsNaN(y));
    }
}

