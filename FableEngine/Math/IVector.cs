using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;

namespace FableEngine.Math;

public interface IVector
{
    public float x => this[0];
    public float y => this[1];
    public float z => this[2];
    public float w => this[3];
    
    public float this[int index] { get; set; }
    public int Dimensions { get; }

    public IVector New();

    public string ToString()
    {
        string r = "<";
        for (int i = 0; i < Dimensions; i++)
            r += this[i] + ", ";
        return r + ">";
    }

    public IVector Parse(string s, IFormatProvider? provider = null)
    {
        IVector r = New();
        string ins = s.TrimStart("<").TrimEnd(">").ToString();
        ins = s.Replace(",", "");
        string[] ss = ins.Split(" ");
        for (int i = 0; i < r.Dimensions; i++)
            if (float.TryParse(ss[i], out float f))
                r[i] = f;
        return r;
    }

    public IVector Add(IVector b)
    {
        IVector r = New();
        for (int i = 0; i < Dimensions; i++)
            r[i] += b[i];
        return r;
    }
    public IVector Multiply(IVector b)
    {
        IVector r = New();
        for (int i = 0; i < Dimensions; i++)
            r[i] *= b[i];
        return r;
    }
    public IVector Divide(IVector b)
    {
        IVector r = New();
        for (int i = 0; i < Dimensions; i++)
            r[i] /= b[i];
        return r;
    }
    
    public IVector Negative()
    {
        IVector r = New();
        for (int i = 0; i < Dimensions; i++)
            r[i] = -this[i];
        return r;
    }
    
    public IVector Multiply(float b)
    {
        IVector r = New();
        for (int i = 0; i < Dimensions; i++)
            r[i] *= b;
        return r;
    }
    public IVector Divide(float b)
    {
        if (b == 0) return New();
        IVector r = New();
        for (int i = 0; i < Dimensions; i++)
            r[i] /= b;
        return r;
    }
    public IVector Add(float b)
    {
        IVector r = New();
        for (int i = 0; i < Dimensions; i++)
            r[i] += b;
        return r;
    }

    public IVector Clamp(IVector min, IVector max)
    {
        IVector r = New();
        for (int i = 0; i < Dimensions; i++)
            r[i] = this[i] < min[i] ? min[i] : this[i] > max[i] ? max[i] : this[i];
        return r;
    }
    public IVector ClampLength(float f)
    {
        IVector r = New();
        if (f == 0)
        {
            for (int i = 0; i < Dimensions; i++)
                r[i] = 0;
            return r;
        }
        float length = 0;
        for (int i = 0; i < Dimensions; i++)
            length += this[i] * this[i];
        if (length == 0) return ClampLength(0);
        length = MathF.Sqrt(length);
        for (int i = 0; i < Dimensions; i++)
            r[i] = this[i] / length * f;
        return r;
    }
    public IVector Set(float f)
    {
        IVector r = New();
        for (int i = 0; i < Dimensions; i++)
            r[i] = f;
        return r;
    }
}