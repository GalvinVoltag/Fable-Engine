using FableEngine.Core;

namespace FableEngine.Debugging;


public enum DebugLogLevel
{
    Success = -1,
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

public static class Debug
{
    /// <summary>
    /// Any Debug.Log will check, and won't log anything if its log level is below this threshold.
    /// It can be set using DebugLogLevel enum for ease of use, but can be set to any integer.
    /// </summary>
    public static int LogLevel = -2;

    public static bool LogAll(object?[] msg, DebugLogLevel debugLogLevel = DebugLogLevel.Debug)
    {
        foreach (object? obj in msg)
            Log(obj, debugLogLevel);
        return true;
    }
    
    /// <summary>
    /// A method to do logging with custom colors or log severity level.
    /// </summary>
    /// <param name="msg">object to log</param>
    /// <param name="color">text color of the log</param>
    /// <param name="backgroundColor">background color of the log</param>
    /// <param name="debugLogLevel">log level of the log for filtering it based on current debug filters</param>
    /// <returns>returns "true" all the time, letting you put this method into logical operations.</returns>
    public static bool Log(object? msg, ConsoleColor color, ConsoleColor backgroundColor, int debugLogLevel)
    {
        if (debugLogLevel < LogLevel ) return true;
        ConsoleColor[] oldColors = [
            Console.ForegroundColor,
            Console.BackgroundColor
        ];
        Console.ForegroundColor = color;
        Console.BackgroundColor = backgroundColor;
        Console.WriteLine(msg);
        Console.ForegroundColor = oldColors[0];
        Console.BackgroundColor = oldColors[1];
        return true;
    }

    public static bool Log(object? msg, ConsoleColor color, ConsoleColor? backgroundColor = null,
        DebugLogLevel debugLogLevel = DebugLogLevel.Debug) =>
        Log(msg, color, backgroundColor??Console.ForegroundColor, (int)debugLogLevel);
    
    
    /// <summary>
    /// Log objects with a DebugLogLevel. This will color the log according to the level.
    /// </summary>
    /// <param name="msg">object to be logged</param>
    /// <param name="debugLogLevel">log level to filter it against current debug filter</param>
    /// <returns>returns "true" all the time, letting you put this method into logical operations.</returns>
    public static bool Log(object? msg, int debugLogLevel)
    {
        if (debugLogLevel < LogLevel ) return true;
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = 
            debugLogLevel switch
            {
                <(int)DebugLogLevel.Success => ConsoleColor.Cyan,
                (int)DebugLogLevel.Success => ConsoleColor.Green,
                (int)DebugLogLevel.Debug => ConsoleColor.Gray,
                (int)DebugLogLevel.Info => oldColor,
                (int)DebugLogLevel.Warning => ConsoleColor.Yellow,
                (int)DebugLogLevel.Error => ConsoleColor.DarkRed,
                >(int)DebugLogLevel.Error => ConsoleColor.Red
            };
        Console.WriteLine(msg);
        Console.ForegroundColor = oldColor;
        return true;
    }

    public static bool Log(object? msg, DebugLogLevel debugLogLevel = DebugLogLevel.Debug) => Log(msg, (int)debugLogLevel);

    public static bool Log(object source, object? msg, int debugLogLevel) => Log($"[{ (source is INameable n ? n : source) }] " + msg, debugLogLevel);
    public static bool Log(object source, object? msg, DebugLogLevel debugLogLevel = DebugLogLevel.Debug) => Log($"[{ (source is INameable n ? n : source) }] " + msg, (int)debugLogLevel);
    
}