using System.Runtime.InteropServices;
using FableEngine.Rendering.Backend;
using FableEngine.Rendering;

namespace FableEngine.Math;

public struct Vertex 
{
    public Vector2 pos;
    public Vector2 UV;
    public Color color;
    public static int Length => 8;
    public static int Size => sizeof(float) * Length;

    public static readonly VertexLayout Layout = new VertexLayout([ 
        (2, type: ParameterType.Float, false), 
        (2, type: ParameterType.Float, false), 
        (4, type: ParameterType.Float, false) ]);

    public Vertex(Vector2 position, Vector2 UV, Color vertexColor)
    {
        pos = position;
        this.UV = UV;
        color = vertexColor;
    }
    public Vertex(float x, float y, float U, float V, Color vertexColor)
    {
        pos = new(x,y);
        this.UV = new(U, V);
        color = vertexColor;
    }
    public Vertex(float x, float y, float U, float V, float r, float g, float b, float a)
    {
        pos = new(x,y);
        this.UV = new(U, V);
        color = new(r, g, b, a);
    }

    public byte[] ToData()
    {
        float[] f = new float[Length];
        f[0] = pos.x;  f[1] = pos.y;
        f[2] = UV.x;   f[3] = UV.y;
        f[4] = color.r; f[5] = color.g; f[6] = color.b; f[7] = color.a;
        return MemoryMarshal.AsBytes(f.AsSpan()).ToArray();
    }

    public static byte[] ToData(IEnumerable<Vertex> vertices)
    {
        Vertex[] vs = vertices as Vertex[] ?? vertices.ToArray();
        return vs.SelectMany(v => v.ToData()).ToArray();
    }

    public static Vertex[] ToVertices(ReadOnlySpan<byte> data)
    {
        // byte[] data = Data as byte[] ?? Data.ToArray();
        if (data.Length % Size != 0) throw new Exception("Cannot convert data stream into vertices since length of the stream is not a multiple of the vertex size");
        ReadOnlySpan<float> f = MemoryMarshal.Cast<byte, float>(data);
        Vertex[] vs = new Vertex[data.Length / Size];
        for (int i = 0; i < vs.Length; i++)
            vs[i] = new Vertex(
                f[i*8], f[i*8+1], f[i*8+2], f[i*8+3],
                f[i*8+4], f[i*8+5], f[i*8+6], f[i*8+7]);
        return vs;
    }
}
