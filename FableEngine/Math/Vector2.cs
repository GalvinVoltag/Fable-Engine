using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FableEngine.Math;
using Silk.NET.Maths;

namespace FableEngine.Math;

public record struct Vector2 : IVector, IParsable<Vector2>
{
    public float x, y = 0;

    public static readonly Vector2 Zero = new(0, 0);
    public static readonly Vector2 One = new(1, 1);
    public static readonly Vector2 UnitX = new(1, 0);
    public static readonly Vector2 UnitY = new(0, 1);
    
    public float magnitude => MathF.Sqrt(x * x + y * y);
    public float sqrMagnitude => x * x + y * y;
    public Vector2 normalized => new(x / magnitude, y / magnitude);
    public Vector2 reversed => new(y, x);

    public IVector New() => new Vector2(x, y);
    public int Dimensions => 2;
    public float this[int index]
    {
        get => index switch
        {
            0 => x,
            1 => y,
            _ => 0
        };
        set
        {
            switch (index)
            {
                case 0:
                    x = value;
                    break;
                case 1:
                    y = value;
                    break;
            }
        }
    }

    public override string ToString() { return $"<{x}, {y}>"; }

    public void Rotate(float angle)
    {
        float X = x;
        x = MathF.Cos(angle) * X - MathF.Sin(angle) * y;
        y = MathF.Sin(angle) * X + MathF.Cos(angle) * y;
    }
    public Vector2 Rotated(float angle) {
        return new Vector2(
            MathF.Cos(angle) * x - MathF.Sin(angle) * y,
            MathF.Sin(angle) * x + MathF.Cos(angle) * y
        ); }

    public Vector2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2(float v)
    {
        x = v;
        y = v;
    }
    
    public static Vector2 Rotate(Vector2 vector, float angle)
    {
        return new Vector2(
            MathF.Cos(angle) * vector.x - MathF.Sin(angle) * vector.y,
            MathF.Sin(angle) * vector.x + MathF.Cos(angle) * vector.y
        );
    }
    public static Vector2 Scale(Vector2 a, Vector2 b) => a * b;
    public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
    public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * t;
    
    public static explicit operator Vector2(Vector2D<int> vector2D)
    {
        return new Vector2(vector2D.X, vector2D.Y);
    }


    public IVector Negative() => -this;
    public IVector Add(float b) => this + b;
    public IVector Add(IVector b) => this + b;
    public IVector Multiply(float b) => this * b;
    public IVector Multiply(IVector b) => this * b;
    public IVector Divide(float b) => this / b;
    public IVector Divide(IVector b) => this / b;

    public static Vector2 operator +(Vector2 a, float b) => new(a.x + b, a.y + b);
    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x + b.x, a.y + b.y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);
    public static Vector2 operator -(Vector2 a) => new(-a.x, -a.y);
    
    public static Vector2 operator +(Vector2 a, IVector b) => new(a.x + b.x, a.y + b.y);
    public static Vector2 operator -(Vector2 a, IVector b) => new(a.x - b.x, a.y - b.y);
    
    public static Vector2 operator *(Vector2 a, float b) => new(a.x * b, a.y * b);
    public static Vector2 operator *(float a, Vector2 b) => new(a * b.x, a * b.y);
    
    public static Vector2 operator *(Vector2 a, Vector2 b) => new(a.x * b.x, a.y * b.y);
    
    public static Vector2 operator *(Vector2 a, IVector b) => new(a.x * b.x, a.y * b.y);
    
    public static Vector2 operator /(Vector2 a, float b) => new(a.x / b, a.y / b);
    public static Vector2 operator /(Vector2 a, Vector2 b) => new(a.x / b.x, a.y / b.y);
    
    
    public static Vector2 operator /(Vector2 a, IVector b) => new(a.x / b.x, a.y / b.y);
    
    public static bool operator ==(Vector2? a, Vector2? b) => Equals(a?.x, b?.x) && Equals(a?.y, b?.y);
    public static bool operator !=(Vector2? a, Vector2? b) => !(a == b);
    
    public static bool operator ==(Vector2? a, IVector? b) => Equals(a?.x, b?.x) && Equals(a?.y, b?.y);
    public static bool operator !=(Vector2? a, IVector? b) => !(a == b);

    public static Vector2 Parse(string s, IFormatProvider? provider = null)
    {
        string ins = s.TrimStart("<").TrimEnd(">").ToString();
        ins = ins.Replace(",", "");
        string[] ss = ins.Split(" ");
        if (ss.Length != 2) return Zero;
        if (float.TryParse(ss[0], out float x) && float.TryParse(ss[1], out float y))
            return new Vector2(x, y);
        return Zero;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Vector2 result)
    {
        throw new NotImplementedException();
    }
}