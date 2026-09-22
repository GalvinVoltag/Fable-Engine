namespace FableEngine.Math;

public interface IMatrix
{
    static int width { get; }
    static int height { get; }
    string ToString();
    float this[int row, int col] { get; set; }
}

public struct Matrix4x4 : IMatrix
{
    public static readonly Matrix4x4 identity = new Matrix4x4();
    public static int width => 4;
    public static int height => 4;
    
    private float[] _data = [
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
    ];

    public float[] data
    {
        get => _data;
        set
        {
            if (value.Length != 16) throw new Exception("Matrix4x4 data can only be set to array of 16 length!");
            _data = value;
        }
    }
    
    public float this[int row, int col]
    {
        get => _data[col * 4 + row];
        set => _data[col * 4 + row] = value;
    }
    
    public float this[int slot] => _data[slot];

    public Matrix4x4() { }
    public Matrix4x4(float[] array) { _data = array.ToArray(); }

    public override string ToString()
    {
        string result = "";
        for (int i = 0; i < 4; i++)
        {
            result += "[ ";
            for (int j = 0; j < 4; j++)
            {
                result += this[i,j] + (j < 3 ? ", " : " ");
            }
            result += "]\n";
        }
        return result;
    }
    
    public Matrix4x4 Inverse()
    {
        float[] m = _data;
        float[] inv = new float[16];

        inv[0]  =  m[5]*m[10]*m[15] - m[5]*m[11]*m[14] - m[9]*m[6]*m[15] + m[9]*m[7]*m[14] + m[13]*m[6]*m[11] - m[13]*m[7]*m[10];
        inv[4]  = -m[4]*m[10]*m[15] + m[4]*m[11]*m[14] + m[8]*m[6]*m[15] - m[8]*m[7]*m[14] - m[12]*m[6]*m[11] + m[12]*m[7]*m[10];
        inv[8]  =  m[4]*m[9] *m[15] - m[4]*m[11]*m[13] - m[8]*m[5]*m[15] + m[8]*m[7]*m[13] + m[12]*m[5]*m[11] - m[12]*m[7]*m[9];
        inv[12] = -m[4]*m[9] *m[14] + m[4]*m[10]*m[13] + m[8]*m[5]*m[14] - m[8]*m[6]*m[13] - m[12]*m[5]*m[10] + m[12]*m[6]*m[9];

        inv[1]  = -m[1]*m[10]*m[15] + m[1]*m[11]*m[14] + m[9]*m[2]*m[15] - m[9]*m[3]*m[14] - m[13]*m[2]*m[11] + m[13]*m[3]*m[10];
        inv[5]  =  m[0]*m[10]*m[15] - m[0]*m[11]*m[14] - m[8]*m[2]*m[15] + m[8]*m[3]*m[14] + m[12]*m[2]*m[11] - m[12]*m[3]*m[10];
        inv[9]  = -m[0]*m[9] *m[15] + m[0]*m[11]*m[13] + m[8]*m[1]*m[15] - m[8]*m[3]*m[13] - m[12]*m[1]*m[11] + m[12]*m[3]*m[9];
        inv[13] =  m[0]*m[9] *m[14] - m[0]*m[10]*m[13] - m[8]*m[1]*m[14] + m[8]*m[2]*m[13] + m[12]*m[1]*m[10] - m[12]*m[2]*m[9];

        inv[2]  =  m[1]*m[6] *m[15] - m[1]*m[7] *m[14] - m[5]*m[2]*m[15] + m[5]*m[3]*m[14] + m[13]*m[2]*m[7]  - m[13]*m[3]*m[6];
        inv[6]  = -m[0]*m[6] *m[15] + m[0]*m[7] *m[14] + m[4]*m[2]*m[15] - m[4]*m[3]*m[14] - m[12]*m[2]*m[7]  + m[12]*m[3]*m[6];
        inv[10] =  m[0]*m[5] *m[15] - m[0]*m[7] *m[13] - m[4]*m[1]*m[15] + m[4]*m[3]*m[13] + m[12]*m[1]*m[7]  - m[12]*m[3]*m[5];
        inv[14] = -m[0]*m[5] *m[14] + m[0]*m[6] *m[13] + m[4]*m[1]*m[14] - m[4]*m[2]*m[13] - m[12]*m[1]*m[6]  + m[12]*m[2]*m[5];

        inv[3]  = -m[1]*m[6] *m[11] + m[1]*m[7] *m[10] + m[5]*m[2]*m[11] - m[5]*m[3]*m[10] - m[9]*m[2]*m[7]   + m[9]*m[3]*m[6];
        inv[7]  =  m[0]*m[6] *m[11] - m[0]*m[7] *m[10] - m[4]*m[2]*m[11] + m[4]*m[3]*m[10] + m[8]*m[2]*m[7]   - m[8]*m[3]*m[6];
        inv[11] = -m[0]*m[5] *m[11] + m[0]*m[7] *m[9]  + m[4]*m[1]*m[11] - m[4]*m[3]*m[9]  - m[8]*m[1]*m[7]   + m[8]*m[3]*m[5];
        inv[15] =  m[0]*m[5] *m[10] - m[0]*m[6] *m[9]  - m[4]*m[1]*m[10] + m[4]*m[2]*m[9]  + m[8]*m[1]*m[6]   - m[8]*m[2]*m[5];

        float det = m[0]*inv[0] + m[1]*inv[4] + m[2]*inv[8] + m[3]*inv[12];

        if (MathF.Abs(det) < 1e-6f) throw new Exception("Matrix4x4 is not invertible (determinant is zero)!");

        float invDet = 1.0f / det;
        for (int i = 0; i < 16; i++) inv[i] *= invDet;

        return new Matrix4x4(inv);
    }

    public static Matrix4x4 MultiplyMatrix(Matrix4x4 a, Matrix4x4 b)
    {
        if (a._data.Length != b._data.Length && a._data.Length != 16) throw new Exception("matrix4x4 multiplication sizes don't match!");
        float[] result = new float[16];
        for (int col = 0; col < 4; col++)
        for (int row = 0; row < 4; row++)
        for (int k = 0; k < 4; k++)
            result[col * 4 + row] += a._data[k * 4 + row] * b._data[col * 4 + k];
        return new Matrix4x4(result);
    }

    // 
    
    public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b) => MultiplyMatrix(a, b);
    public static Matrix4x4 operator !(Matrix4x4 a) => a.Inverse();

    public static Vector2 operator *(Matrix4x4 a, Vector2 b)
    {
        float x = b.x, y = b.y;
        return new Vector2(
            x * a[0] + y * a[4] + a[12],
            x * a[1] + y * a[5] + a[13]
        );
    }
    public static Vector3 operator *(Matrix4x4 a, Vector3 b)
    {
        float x = b.x, y = b.y, z = b.z;
        return new Vector3(
            x * a[0] + y * a[4] + z * a[8]  + a[12],
            x * a[1] + y * a[5] + z * a[9]  + a[13],
            x * a[2] + y * a[6] + z * a[10] + a[14]
        );
    }
    public static Vector4 operator *(Matrix4x4 a, Vector4 b)
    {
        float x = b.x, y = b.y, z = b.z;
        return new Vector4(
            x * a[0] + y * a[4] + z * a[8]  + a[12],
            x * a[1] + y * a[5] + z * a[9]  + a[13],
            x * a[2] + y * a[6] + z * a[10] + a[14],
            x * a[3] + y * a[7] + z * a[11] + a[15]
        );
    }
}