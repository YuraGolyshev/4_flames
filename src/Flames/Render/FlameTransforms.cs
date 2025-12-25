namespace Flames.Render;

public static class FlameTransforms
{
    private const double EPSILON = 1e-10; // Малое число для защиты от деления на ноль
    private const double HORSESHOE_K = 0.5; // Коэффициенты масштабирования в Horseshoe
    public static (double, double) Linear(double x, double y) => (x, y);
    public static (double, double) Swirl(double x, double y)
    {
        double r2 = x * x + y * y;
        return (
            x * System.Math.Sin(r2) - y * System.Math.Cos(r2),
            x * System.Math.Cos(r2) + y * System.Math.Sin(r2)
        );
    }
    public static (double, double) Horseshoe(double x, double y)
    {
        double r = System.Math.Sqrt(x * x + y * y);
        if (r < EPSILON) { return (0, 0); }
        return (
            ((x - y) * (x + y) / r) * HORSESHOE_K,
            (2 * x * y / r) * HORSESHOE_K
        );
    }
    public static (double, double) Spherical(double x, double y)
    {
        double r2 = x * x + y * y;
        if (r2 < EPSILON) { return (0, 0); }
        return (x / r2, y / r2);
    }
    public static (double, double) Sinusoidal(double x, double y)
        => (System.Math.Sin(x), System.Math.Sin(y));
    public static (double, double) Polar(double x, double y)
    {
        double theta = System.Math.Atan2(y, x);
        double r = System.Math.Sqrt(x * x + y * y);
        return (theta / System.Math.PI, r - 1.0);
    }
}
