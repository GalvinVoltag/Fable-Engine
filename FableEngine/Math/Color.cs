using System.Diagnostics.CodeAnalysis;
using FableEngine.Debugging;
using FableEngine.Math;
using Debug = System.Diagnostics.Debug;

namespace FableEngine.Math;


public record struct Color(float r = 1, float g = 1, float b = 1, float a = 1) : IVector, IParsable<Color>
{
    public bool Equals(Color other) {
        return r.Equals(other.r) && g.Equals(other.g) && b.Equals(other.b) && a.Equals(other.a);
    }
    public string ToHex() => 
        $"#{((int)(r * 255)):X2}{((int)(g * 255)):X2}{((int)(b * 255)):X2}{((int)(a * 255)):X2}";

    public IVector New() => new Color(1, 1, 1, 1);
    public int Dimensions => 4;
    public float this[int index]
    {
        get => index switch
        {
            0 => r,
            1 => g,
            2 => b,
            3 => a,
            _ => 0
        };
        set
        {
            switch (index)
            {
                case 0:
                    r = value;
                    break;
                case 1:
                    g = value;
                    break;
                case 2:
                    b = value;
                    break;
                case 3:
                    a = value;
                    break;
            }
        }
    }

    public override string ToString() => $"Color({r}, {g}, {b}, {a})";


    public static readonly Color transparent = new(0, 0, 0, 0);
    public static readonly Color white = new(1, 1, 1, 1);
    public static readonly Color gray = new(0.5f, 0.5f, 0.5f, 1);
    public static readonly Color black = new(0, 0, 0, 1);
    public static readonly Color softBlue = new(0.2f, 0.3f, 0.8f, 1);
    public static readonly Color softRed = new(0.8f, 0.3f, 0.2f, 1);
    public static readonly Color softGreen = new(0.3f, 0.8f, 0.2f, 1);
    public static readonly Color red = new(1, 0, 0, 1);
    public static readonly Color pink = new(1, 0.5f, 0.5f, 1);
    public static readonly Color orange = new(1, 0.5f, 0, 1);
    public static readonly Color yellow = new(1, 1, 0, 1);
    public static readonly Color green = new(0, 1, 0, 1);
    public static readonly Color blue = new(0, 0, 1, 1);
    
    public float r = r, g = g, b = b, a = a;
    public float R => r;
    public float G => g;
    public float B => b;
    public float A => a;
    
    public static Color operator +(Color a, Color b) => new(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);
    public static Color operator -(Color a, Color b) => new(a.r - b.r, a.g - b.g,  a.b - b.b, a.a - b.a);
    public static Color operator -(Color a) => new(-a.r, -a.g, -a.b, a.a);
    
    public static Color operator *(Color a, float b) => new(a.r + b, a.g + b, a.b + b, a.a);
    public static Color operator *(float b, Color a) => new(a.r + b, a.g + b, a.b + b, a.a);
    
    public static Color operator *(Color a, Color b) => new(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
    
    public static Color operator /(Color a, float b) => new(a.r / b, a.g / b, a.b / b, a.a);
    
    public static Color operator /(Color a, Color b) => new(a.r / b.r, a.g / b.g, a.b / b.b, a.a / b.a);
    
    // public static bool operator ==(Color a, Color b) => (a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a);
    // public static bool operator !=(Color a, Color b) => (a.r != b.r || a.g != b.g || a.b != b.b || a.a != b.a);
    public static Color Parse(string s, IFormatProvider? provider = null)
    {
        string[] ss = s.TrimStart("Color(").TrimEnd(")").ToString().Replace(",", "").Split(" ");
        if (ss.Length != 4) return white;
        if (float.TryParse(ss[0], out float r) && float.TryParse(ss[1], out float g) &&
            float.TryParse(ss[2], out float b) && float.TryParse(ss[3], out float a))
            return new Color(r, g, b, a);
        else
            return white;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Color result)
    {
        throw new NotImplementedException();
    }
}
