using System.Collections;
using FableEngine.Core;

namespace FableEngine.Debugging;


public readonly struct DebugLogLevel(int level) : IEquatable<int>, IEquatable<DebugLogLevel>
{
    private readonly int level = level;
    
    public const int Success = -1;
    public const int Debug = 0;
    public const int Info = 1;
    public const int Warning = 2;
    public const int Error = 3;

    public static implicit operator int(DebugLogLevel level) => level.level;
    public static implicit operator DebugLogLevel(int i) => new(i);

    public bool Equals(DebugLogLevel other)
    {
        return level == other.level;
    }

    public bool Equals(int other)
    {
        return level == other;
    }

    public override bool Equals(object? obj)
    {
        return obj is DebugLogLevel other && Equals(other);
    }

    public override int GetHashCode()
    {
        return level;
    }
}

// todo: implement instance based debug additionally, for source distinction capability.
public class Debug : INameable
{
    /// <summary>
    /// Any Debug.Log will check, and won't log anything if its log level is below this threshold.
    /// </summary>
    public static DebugLogLevel LogLevel = -2;
    /// <summary>
    /// Any debug.Log will check, and won't log anything if its log level is below this threshold.
    /// </summary>
    public DebugLogLevel logLevel = -2;
    /// <summary>
    /// Name of this debug source.
    /// </summary>
    public string name { get; set; } = "";
    
    /// <summary>
    /// A method to do logging with custom colors or log severity level.
    /// </summary>
    /// <param name="msg">object to log</param>
    /// <param name="color">text color of the log</param>
    /// <param name="backgroundColor">background color of the log</param>
    /// <param name="debugLogLevel">log level of the log for filtering it based on current debug filters</param>
    /// <returns>"true" all the time, letting you put this method into logical operations.</returns>
    public static bool Log(object? msg, ConsoleColor color, ConsoleColor backgroundColor, DebugLogLevel debugLogLevel)
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
    
    /// <summary>
    /// Log objects with a DebugLogLevel. This will color the log according to the level.
    /// </summary>
    /// <param name="msg">object to be logged</param>
    /// <param name="debugLogLevel">log level to filter it against current debug filter</param>
    /// <returns> "true" all the time, letting you put this method into logical operations.</returns>
    public static bool Log(object? msg, DebugLogLevel? debugLogLevel = null)
    {
        debugLogLevel ??= DebugLogLevel.Debug;
        if (debugLogLevel < LogLevel ) return true;
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = 
            (int)debugLogLevel switch
            {
                <DebugLogLevel.Success => ConsoleColor.Cyan,
                 DebugLogLevel.Success => ConsoleColor.Green,
                 DebugLogLevel.Debug => ConsoleColor.Gray,
                 DebugLogLevel.Info => oldColor,
                 DebugLogLevel.Warning => ConsoleColor.Yellow,
                 DebugLogLevel.Error => ConsoleColor.DarkRed,
                >DebugLogLevel.Error => ConsoleColor.Red,
            };
        Console.WriteLine(msg);
        Console.ForegroundColor = oldColor;
        return true;
    }

    /// <summary>
    /// Log many objects in one go. Pass in ConsoleColor as a parameter to change the text color of following parameters.
    /// </summary>
    /// <param name="level">level of the log, used for ignoring low level logs</param>
    /// <param name="objects">object to be logged, or ConsoleColor</param>
    /// <returns> "true" all the time, letting you put this method into logical operations</returns>
    
    public static bool Log(DebugLogLevel level ,params object[] objects)
    {
        if (level < LogLevel ) return true;
        ConsoleColor oldColor = Console.ForegroundColor;
        foreach (var obj in objects)
        {
            if (obj is ConsoleColor color) Console.ForegroundColor = color;
            else Console.Write(obj);
        }
        Console.Write("\n");
        Console.ForegroundColor = oldColor;
        return true;
    }
}