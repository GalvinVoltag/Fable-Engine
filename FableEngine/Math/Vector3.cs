using FableEngine.Math;
using Silk.NET.Maths;

namespace FableEngine.Math;

public record struct Vector3 : IVector
{
    public float x, y, z = 0;
    
    public static readonly Vector3 Zero = new(0, 0, 0);
    public static readonly Vector3 One = new(1, 1, 1);
    public static readonly Vector3 UnitX = new(1, 0, 0);
    public static readonly Vector3 UnitY = new(0, 1, 0);
    public static readonly Vector3 UnitZ = new(0, 0, 1);
    public static readonly Vector3 UnitXY = new(1, 1, 0);
    public static readonly Vector3 UnitXZ = new(1, 0, 1);
    public static readonly Vector3 UnitYZ = new(0, 1, 1);
    
    public float magnitude => MathF.Sqrt(x * x + y * y + z * z);
    public float sqrMagnitude => x * x + y * y +  z * z;
    public Vector3 normalized => new(x / magnitude, y / magnitude, z / magnitude);
    // public Vector3 reversed => new(y, x);

    public IVector New() => new Vector3(x, y, z);
    public int Dimensions => 3;
    public float this[int index]
    {
        get => index switch
        {
            0 => x,
            1 => y,
            2 => z,
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
            }
        }
    }

    public override string ToString() { return $"<{x}, {y}, {z}>"; }

    // public Vector3 Rotated(float angle) {  Todo: make axis angle, and euler angles version, as well as their static counterparts
    //     return new Vector3(
    //         MathF.Cos(angle) * x - MathF.Sin(angle) * y,
    //         MathF.Sin(angle) * x + MathF.Cos(angle) * y
    //     ); 
    // }

    public Vector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3(float v)
    {
        x = v;
        y = v;
        z = v;
    }

    public static Vector3 Scale(Vector3 a, Vector3 b) => a * b;
    public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y;
    public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
    public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * t;
    
    public static explicit operator Vector3(Vector3D<int> vector3D)
    {
        return new Vector3(vector3D.X, vector3D.Y, vector3D.Z);
    }
    
    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
    public static Vector3 operator -(Vector3 a) => new(-a.x, -a.y, -a.z);
    
    public static Vector3 operator +(IVector a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    public static Vector3 operator -(IVector a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
    
    public static Vector3 operator +(Vector3 a, IVector b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    public static Vector3 operator -(Vector3 a, IVector b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
    
    public static Vector3 operator *(Vector3 a, float b) => new(a.x * b, a.y * b, a.z * b);
    public static Vector3 operator *(float a, Vector3 b) => new(a * b.x, a * b.y, a * b.z);
    
    public static Vector3 operator *(Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);
    public static Vector3 operator *(Vector3 a, IVector b) => new(a.x * b.x, a.y * b.y, a.z * b.z);
    
    public static Vector3 operator /(Vector3 a, float b) => new(a.x / b, a.y / b, a.z / b);
    public static Vector3 operator /(float a, Vector3 b) => new(a / b.x, a / b.y, a / b.z);
    public static Vector3 operator /(Vector3 a, Vector3 b) => new(a.x / b.x, a.y / b.y, a.z / b.z);
    public static Vector3 operator /(IVector a, Vector3 b) => new(a.x / b.x, a.y / b.y, a.z / b.z);
    public static Vector3 operator /(Vector3 a, IVector b) => new(a.x / b.x, a.y / b.y, a.z / b.z);
    
    public static bool operator ==(Vector3? a, Vector3? b) => Equals(a?.x, b?.x) && Equals(a?.y, b?.y) && Equals(a?.z, b?.z);
    public static bool operator !=(Vector3? a, Vector3? b) => !(a == b);
    
    public static bool operator ==(Vector3? a, IVector? b) => Equals(a?.x, b?.x) && Equals(a?.y, b?.y) && Equals(a?.z, b?.z);
    public static bool operator !=(Vector3? a, IVector? b) => !(a == b);
    
    public static bool operator ==(IVector? a, Vector3? b) => Equals(a?.x, b?.x) && Equals(a?.y, b?.y) && Equals(a?.z, b?.z);
    public static bool operator !=(IVector? a, Vector3? b) => !(a == b);
}