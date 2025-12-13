using System;

namespace Flames.Utils;

public static class Logger
{
    public static void Info(string msg) => WriteColored("[INFO] ", ConsoleColor.Cyan, msg);
    public static void Warn(string msg) => WriteColored("[WARN] ", ConsoleColor.Yellow, msg);
    public static void Error(string msg) => WriteColored("[ERROR] ", ConsoleColor.Red, msg);

    private static void WriteColored(string prefix, ConsoleColor color, string msg)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(prefix);
        Console.ForegroundColor = old;
        Console.WriteLine(msg);
    }

    public static void Progress(int current, int total)
    {
        int percent = (int)(current * 100.0 / total);
        Console.CursorLeft = 0;
        Console.Write($"[PROGRESS] {percent,3}% ({current}/{total})");
    }
}

