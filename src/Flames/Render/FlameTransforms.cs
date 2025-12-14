namespace Flames.Render;

/// <summary>
/// Реализация всех поддерживаемых нелинейных трансформаций вариаций для фрактального пламени
/// </summary>
public static class FlameTransforms
{
    /// <summary>Линейное (identity) преобразование</summary>
    public static (double, double) Linear(double x, double y) => (x, y);
    /// <summary>Swirl: завихрение вокруг центра, sin(r^2) и cos(r^2)</summary>
    public static (double, double) Swirl(double x, double y)
    {
        double r2 = x * x + y * y;
        return (
            x * System.Math.Sin(r2) - y * System.Math.Cos(r2),
            x * System.Math.Cos(r2) + y * System.Math.Sin(r2)
        );
    }
    /// <summary>Horseshoe: классическая нелинейная деформация в стиле подковы</summary>
    public static (double, double) Horseshoe(double x, double y)
    {
        double r = System.Math.Sqrt(x * x + y * y);
        if (r < 1e-10)
        {
            return (0, 0);
        }

        return (
            ((x - y) * (x + y) / r) * 0.5,
            (2 * x * y / r) * 0.5
        );
    }
    /// <summary>Spherical: инверсия относительно окружности</summary>
    public static (double, double) Spherical(double x, double y)
    {
        double r2 = x * x + y * y;
        if (r2 < 1e-10)
        {
            return (0, 0);
        }

        return (x / r2, y / r2);
    }
    /// <summary>Sinusoidal: поэлементный sin(x), sin(y)</summary>
    public static (double, double) Sinusoidal(double x, double y)
        => (System.Math.Sin(x), System.Math.Sin(y));
    /// <summary>Polar: переходит к полярным координатам, theta/pi, r-1</summary>
    public static (double, double) Polar(double x, double y)
    {
        double theta = System.Math.Atan2(y, x);
        double r = System.Math.Sqrt(x * x + y * y);
        return (theta / System.Math.PI, r - 1.0);
    }
}
