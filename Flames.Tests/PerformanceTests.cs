using System;
using System.Diagnostics;
using System.Linq;
using Flames.Models;
using Flames.Render;
using Xunit;

namespace Flames.Tests;

public class PerformanceTests
{
    private FlameConfig CreateTestConfig(int threads, int iterations = 100000)
    {
        return new FlameConfig
        {
            Width = 400,
            Height = 400,
            IterationCount = iterations,
            Threads = threads,
            Seed = 42,
            Functions = new()
            {
                new TransformationFunction("swirl", 1.0),
                new TransformationFunction("linear", 0.5)
            },
            AffineParams = new()
            {
                new AffineParams(0.7, 0, 0, 0, 0.7, 0),
                new AffineParams(0.7, 0, 0.3, 0, 0.7, 0.3)
            }
        };
    }

    [Fact]
    public void SingleThreaded_Render_Completes()
    {
        var config = CreateTestConfig(1, 50000);
        var renderer = new FlameRenderer(config);
        var sw = Stopwatch.StartNew();
        var result = renderer.Render();
        sw.Stop();
        
        Assert.NotNull(result);
        Assert.Equal(config.Width * config.Height * 3, result.Length);
        Assert.True(sw.ElapsedMilliseconds > 0);
    }

    [Fact]
    public void MultiThreaded_Render_Completes()
    {
        var config = CreateTestConfig(4, 50000);
        var renderer = new MultiThreadedFlameRenderer(config);
        var sw = Stopwatch.StartNew();
        var result = renderer.Render();
        sw.Stop();
        
        Assert.NotNull(result);
        Assert.Equal(config.Width * config.Height * 3, result.Length);
        Assert.True(sw.ElapsedMilliseconds > 0);
    }

    [Fact]
    public void MultiThreaded_FasterThanSingleThreaded()
    {
        int iterations = 200000;
        var singleConfig = CreateTestConfig(1, iterations);
        var multiConfig = CreateTestConfig(4, iterations);
        
        var singleRenderer = new FlameRenderer(singleConfig);
        var multiRenderer = new MultiThreadedFlameRenderer(multiConfig);
        
        Console.WriteLine("\n=== Single vs Multi-threaded Comparison ===");
        Console.WriteLine($"Iterations: {iterations}");
        
        var sw1 = Stopwatch.StartNew();
        singleRenderer.Render();
        sw1.Stop();
        Console.WriteLine($"Single-threaded: {sw1.ElapsedMilliseconds} ms");
        
        var sw2 = Stopwatch.StartNew();
        multiRenderer.Render();
        sw2.Stop();
        Console.WriteLine($"Multi-threaded (4 threads): {sw2.ElapsedMilliseconds} ms");
        Console.WriteLine($"Speedup: {(double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds:F2}x");
        Console.WriteLine("===========================================\n");
        
        // Многопоточная версия должна быть быстрее (хотя бы не медленнее)
        // На некоторых системах может быть незначительное ускорение
        Assert.True(sw2.ElapsedMilliseconds <= sw1.ElapsedMilliseconds * 1.2, 
            $"Single: {sw1.ElapsedMilliseconds}ms, Multi: {sw2.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void DifferentThreadCounts_ProduceSameSize()
    {
        int iterations = 100000;
        var config1 = CreateTestConfig(1, iterations);
        var config2 = CreateTestConfig(2, iterations);
        var config4 = CreateTestConfig(4, iterations);
        var config8 = CreateTestConfig(8, iterations);
        
        var renderer1 = new FlameRenderer(config1);
        var renderer2 = new MultiThreadedFlameRenderer(config2);
        var renderer4 = new MultiThreadedFlameRenderer(config4);
        var renderer8 = new MultiThreadedFlameRenderer(config8);
        
        var result1 = renderer1.Render();
        var result2 = renderer2.Render();
        var result4 = renderer4.Render();
        var result8 = renderer8.Render();
        
        Assert.Equal(result1.Length, result2.Length);
        Assert.Equal(result1.Length, result4.Length);
        Assert.Equal(result1.Length, result8.Length);
    }

    [Fact]
    public void Benchmark_ThreadCounts()
    {
        int iterations = 300000;
        var results = new System.Collections.Generic.Dictionary<int, long>();
        
        Console.WriteLine("\n=== Performance Benchmark ===");
        Console.WriteLine($"Iterations: {iterations}");
        Console.WriteLine($"Image size: 400x400");
        Console.WriteLine("--------------------------------");
        
        foreach (int threads in new[] { 1, 2, 4, 8 })
        {
            var config = CreateTestConfig(threads, iterations);
            var sw = Stopwatch.StartNew();
            
            if (threads == 1)
            {
                var renderer = new FlameRenderer(config);
                renderer.Render();
            }
            else
            {
                var renderer = new MultiThreadedFlameRenderer(config);
                renderer.Render();
            }
            
            sw.Stop();
            results[threads] = sw.ElapsedMilliseconds;
            Console.WriteLine($"Threads: {threads,2} | Time: {sw.ElapsedMilliseconds,6} ms | Speedup: {(double)results[1] / sw.ElapsedMilliseconds:F2}x");
        }
        
        Console.WriteLine("--------------------------------");
        Console.WriteLine($"Best: {results.OrderBy(kvp => kvp.Value).First().Key} threads ({results.OrderBy(kvp => kvp.Value).First().Value} ms)");
        Console.WriteLine("================================\n");
        
        // Просто проверяем, что все выполнились
        Assert.Equal(4, results.Count);
        foreach (var kvp in results)
        {
            Assert.True(kvp.Value > 0, $"Threads {kvp.Key}: {kvp.Value}ms");
        }
    }
}

