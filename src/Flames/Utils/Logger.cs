using System;
using System.IO;

namespace Flames.Utils;

/// <summary>
/// Гибкий логгер для записи в консоль и/или файл, не статический.
/// </summary>
public class Logger
{
    private const int PROGRESS_PERCENT_TOTAL = 100; // Максимальный процент прогресса
    private readonly TextWriter outWriter;
    private readonly bool useColor;
    private readonly object sync = new();

    /// <summary>Стандартный глобальный логгер (по умолчанию — консоль)</summary>
    public static Logger Instance { get; set; } = new Logger(Console.Out, useColor: true);

    public Logger(TextWriter writer, bool useColor = false)
    {
        outWriter = writer;
        this.useColor = useColor && writer == Console.Out;
    }

    public void Info(string msg) => WriteColored("[INFO] ", ConsoleColor.Cyan, msg);
    public void Warn(string msg) => WriteColored("[WARN] ", ConsoleColor.Yellow, msg);
    public void Error(string msg) => WriteColored("[ERROR] ", ConsoleColor.Red, msg);

    private void WriteColored(string prefix, ConsoleColor color, string msg)
    {
        lock (sync)
        {
            if (useColor)
            {
                var old = Console.ForegroundColor;
                Console.ForegroundColor = color;
                outWriter.Write(prefix);
                Console.ForegroundColor = old;
                outWriter.WriteLine(msg);
            }
            else
            {
                outWriter.Write(prefix);
                outWriter.WriteLine(msg);
            }
            outWriter.Flush();
        }
    }

    public void Progress(int current, int total)
    {
        try
        {
            int percent = (int)(current * PROGRESS_PERCENT_TOTAL / (double)total);
            if (outWriter != Console.Out || Console.IsOutputRedirected)
            {
                return;
            }

            lock (sync)
            {
                Console.CursorLeft = 0;
                Console.Write($"[PROGRESS] {percent,3}% ({current}/{total})");
            }
        }
        catch { }
    }
}
