using System;
using System.IO;

namespace Flames.Utils;

/// <summary>
/// Гибкий логгер для записи в консоль и/или файл, не статический.
/// Выбор output меняется через конструктор (или статический Logger.Instance для простого глобального доступа).
/// </summary>
public class Logger
{
    private readonly TextWriter outWriter;
    private readonly bool useColor;
    private readonly object sync = new();

    /// <summary>Стандартный консольный логгер с цветом</summary>
    public static Logger Instance { get; set; } = new Logger(Console.Out, useColor: true);

    /// <summary>Создать логгер, пишущий в поток (например, файл или консоль)</summary>
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
        lock (sync) // важен для многопоточности + корректного цвета
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

    /// <summary>Логгирует прогресс выполнения (только для консоли)</summary>
    public void Progress(int current, int total)
    {
        try
        {
            int percent = (int)(current * 100.0 / total);
            // Проверяем, можно ли писать в консоль
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
        catch { /* Игнорируем для тестов/файлов */ }
    }
}
