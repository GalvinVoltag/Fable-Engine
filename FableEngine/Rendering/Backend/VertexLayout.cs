using System.Runtime.CompilerServices;

namespace FableEngine.Rendering.Backend;

public readonly struct VertexLayout : IEquatable<VertexLayout>
{
    public readonly (int size, ParameterType type, bool normalized)[] layout;
    public readonly int hash = 0;
    public readonly int Length;
    public readonly int Size;
    public (int size, ParameterType type, bool normalized) this[int index] => layout[index];

    public VertexLayout((int size, ParameterType type, bool normalized)[] Layout)
    {
        layout = ((int, ParameterType, bool)[])Layout.Clone();
        Length = layout.Length;
        Size = layout.Sum(v => v.size * SizeOf(v.type));
        foreach (var v in layout) {
            hash = HashCode.Combine(hash, v.size);
            hash = HashCode.Combine(hash, v.type);
            hash = HashCode.Combine(hash, v.normalized);
        }
    }
    
    public static int SizeOf(ParameterType type)
    {
        return type switch
        {
            ParameterType.Float => sizeof(float),
            ParameterType.Byte => sizeof(byte),
            ParameterType.Short => sizeof(short),
            ParameterType.Int => sizeof(int),
            ParameterType.Half => Unsafe.SizeOf<Half>(),
            ParameterType.Double => sizeof(double),
            ParameterType.UnsignedByte => sizeof(byte), //Todo: dunno what to do here either
            ParameterType.UnsignedShort => sizeof(ushort),
            ParameterType.UnsignedInt => sizeof(uint),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    bool IEquatable<VertexLayout>.Equals(VertexLayout other)
    {
        if (Length != other.Length) return false;
        for (int i = 0; i < Length; i++)
        {
            if (this[i].size != other[i].size) return false;
            if (this[i].type != other[i].type) return false;
            if (this[i].normalized != other[i].normalized) return false;
        }
        return true;
    }

    public override int GetHashCode() => hash;
}