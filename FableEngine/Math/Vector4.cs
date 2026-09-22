using FableEngine.Math;
using Silk.NET.Maths;

namespace FableEngine.Math;

public record struct Vector4 : IVector
{
    public float x, y, z, w = 0;
    
    public static readonly Vector4 Zero = new(0, 0, 0, 0);
    public static readonly Vector4 One = new(1, 1, 1, 1);
    public static readonly Vector4 UnitX = new(1, 0, 0, 0);
    public static readonly Vector4 UnitY = new(0, 1, 0, 0);
    public static readonly Vector4 UnitZ = new(0, 0, 1, 0);
    public static readonly Vector4 UnitW = new(0, 0, 0, 1);
    
    public float magnitude => MathF.Sqrt(x * x + y * y  + z * z + w * w);
    public float sqrMagnitude => x * x + y * y + z * z + w * w;
    public Vector4 normalized => new(x / magnitude, y / magnitude, z / magnitude, w / magnitude);
    // public Vector4 reversed => new(y, x);

    public IVector New() => new Vector4(x, y, z, w);
    
    public int Dimensions => 4;
    public float this[int index]
    {
        get => index switch
        {
            0 => x,
            1 => y,
            2 => z,
            3 => w,
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
                case 2:
                    z = value;
                    break;
                case 3:
                    w = value;
                    break;
            }
        }
    }
    
    public override string ToString() { return $"<{x}, {y}, {z}, {w}>"; }

    // public Vector4 Rotated(float angle) {  Todo: make axis angle, and euler angles version, as well as their static counterparts
    //     return new Vector4(
    //         MathF.Cos(angle) * x - MathF.Sin(angle) * y,
    //         MathF.Sin(angle) * x + MathF.Cos(angle) * y
    //     ); 
    // }

    public Vector4(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    public Vector4(float v)
    {
        x = v;
        y = v;
        z = v;
        w = v;
    }

    public static Vector4 Scale(Vector4 a, Vector4 b) => a * b;
    public static float Dot(Vector4 a, Vector4 b) => a.x * b.x + a.y * b.y;
    public static float Distance(Vector4 a, Vector4 b) => (a - b).magnitude;
    public static Vector4 Lerp(Vector4 a, Vector4 b, float t) => a + (b - a) * t;
    
    public static explicit operator Vector4(Vector4D<int> Vector4D)
    {
        return new Vector4(Vector4D.X, Vector4D.Y, Vector4D.Z, Vector4D.W);
    }
    
    public static Vector4 operator +(Vector4 a, Vector4 b) => new(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
    public static Vector4 operator -(Vector4 a, Vector4 b) => new(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
    public static Vector4 operator -(Vector4 a) => new(-a.x, -a.y, -a.z, -a.w);
    
    public static Vector4 operator +(Vector4 a, IVector b) => new(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
    public static Vector4 operator -(Vector4 a, IVector b) => new(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
    
    public static Vector4 operator +(IVector a, Vector4 b) => new(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
    public static Vector4 operator -(IVector a, Vector4 b) => new(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
    
    public static Vector4 operator *(Vector4 a, float b) => new(a.x * b, a.y * b, a.z * b, a.w *b);
    public static Vector4 operator *(float a, Vector4 b) => new(a * b.x, a * b.y, a * b.z, a * b.w);
    
    public static Vector4 operator *(Vector4 a, Vector4 b) => new(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
    public static Vector4 operator *(IVector a, Vector4 b) => new(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
    public static Vector4 operator *(Vector4 a, IVector b) => new(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
    
    public static Vector4 operator /(Vector4 a, float b) => new(a.x / b, a.y / b, a.z / b, a.w / b);
    public static Vector4 operator /(float a, Vector4 b) => new(a / b.x, a / b.y, a / b.z, a / b.w);
    public static Vector4 operator /(Vector4 a, Vector4 b) => new(a.x / b.x, a.y / b.y, a.z / b.z, a.w / b.w);
    public static Vector4 operator /(Vector4 a, IVector b) => new(a.x / b.x, a.y / b.y, a.z / b.z, a.w / b.w);
    public static Vector4 operator /(IVector a, Vector4 b) => new(a.x / b.x, a.y / b.y, a.z / b.z, a.w / b.w);
    
    public static bool operator ==(Vector4? a, Vector4? b) => Equals(a?.x, b?.x) && Equals(a?.y, b?.y) && Equals(a?.z, b?.z) && Equals(a?.w, b?.w);
    public static bool operator !=(Vector4? a, Vector4? b) => !(a == b);
    public static bool operator ==(Vector4? a, IVector? b) => Equals(a?.x, b?.x) && Equals(a?.y, b?.y) && Equals(a?.z, b?.z) && Equals(a?.w, b?.w);
    public static bool operator !=(Vector4? a, IVector? b) => !(a == b);
    public static bool operator ==(IVector? a, Vector4? b) => Equals(a?.x, b?.x) && Equals(a?.y, b?.y) && Equals(a?.z, b?.z) && Equals(a?.w, b?.w);
    public static bool operator !=(IVector? a, Vector4? b) => !(a == b);
}