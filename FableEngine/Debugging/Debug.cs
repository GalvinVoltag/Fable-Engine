using System.Collections;
using FableEngine.Core;
using Microsoft.VisualBasic.FileIO;

namespace FableEngine.Debugging;


public readonly struct DebugLogLevel(int level) : IEquatable<int>, IEquatable<DebugLogLevel>
{
    private readonly int level = level;
    
    public static readonly DebugLogLevel Success = IntSuccess;
    public static readonly DebugLogLevel Debug   = IntDebug  ;
    public static readonly DebugLogLevel Info    = IntInfo   ;
    public static readonly DebugLogLevel Warning = IntWarning;
    public static readonly DebugLogLevel Error   = IntError  ;
    
    public const int IntSuccess = -1;
    public const int IntDebug   = 0;
    public const int IntInfo    = 1;
    public const int IntWarning = 2;
    public const int IntError   = 3;

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

    public static bool stackSameLines = true;
    public static uint stackCount = 0;
    public static object[] lastLine = [];

    public static Func<DebugLogLevel, ConsoleColor> levelColors = (debugLogLevel) =>
    {
        return (int)debugLogLevel switch
        {
            <DebugLogLevel.IntSuccess => ConsoleColor.Cyan,
             DebugLogLevel.IntSuccess => ConsoleColor.Green,
             DebugLogLevel.IntDebug   => ConsoleColor.Gray,
             DebugLogLevel.IntInfo    => Console.ForegroundColor,
             DebugLogLevel.IntWarning => ConsoleColor.Yellow,
             DebugLogLevel.IntError   => ConsoleColor.DarkRed,
            >DebugLogLevel.IntError   => ConsoleColor.Red,
        };
    };
    public static Func<DebugLogLevel, ConsoleColor> levelBackgroundColors = (debugLogLevel) => Console.BackgroundColor;
    
    /// <summary>
    /// Log many objects in one go. Pass in ConsoleColor as a parameter to change the text color of following parameters.
    /// pass in a (ConsoleColor, ConsoleColor) pair as a parameter to change both text and background color of following parameters.
    /// pass in a DebugLogLevel to set the log level of following parameters. Note that if the last parameter is a DebugLogLevel and is
    /// smaller than the set limit, none of the parameters will be logged.
    /// </summary>
    /// <param name="objects">object to be logged, a ConsoleColor, a (ConsoleColor, ConsoleColor) pair, or a DebugLogLevel</param>
    /// <returns> "true" all the time, letting you put this method into logical operations</returns>
    public static bool Log(params object[] objects)
    {
        (ConsoleColor, ConsoleColor) oldColors = (Console.ForegroundColor, Console.BackgroundColor);
        if (objects[^1] is DebugLogLevel lev)
        {
            if (lev < LogLevel) return true;
            Console.ForegroundColor = levelColors(lev);
            Console.BackgroundColor = levelBackgroundColors(lev);
        }

        bool sameLine = false;
        if (objects.Length == lastLine.Length)
            for (int i = 0; i < objects.Length; i++)
            {
                if (!objects[i].Equals(lastLine[i])) break;
                sameLine = true;
                break;
            }

        if (sameLine)
        {
            stackCount++;
            int lineheight = 1;
            foreach (object o in objects)
                if (o is string s)
                    lineheight += s.Split("\n").Length - 1;
            Console.CursorTop -= lineheight;
            Console.Write($"({stackCount}) ");
        }
        else stackCount = 0;
        foreach (object obj in objects)
        {
            switch (obj)
            {
                case ConsoleColor color:
                    Console.ForegroundColor = color;
                    break;
                case (ConsoleColor a, ConsoleColor b):
                    Console.ForegroundColor = a; Console.BackgroundColor = b;
                    break;
                case DebugLogLevel l when l < LogLevel:
                    continue;
                case DebugLogLevel l:
                    Console.ForegroundColor = levelColors(l);
                    Console.BackgroundColor = levelBackgroundColors(l);
                    break;
                default:
                    Console.Write(obj);
                    break;
            }
        }
        Console.Write("\n");
        lastLine = objects;
        (Console.ForegroundColor, Console.BackgroundColor) = oldColors;
        return true;
    }
}