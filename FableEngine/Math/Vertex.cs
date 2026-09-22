namespace FableEngine.Math;

public struct Vertex 
{
    public Vector2 pos;
    public Vector2 UV;
    public Color color;
    public static int Length => 8;
    public static int Size => sizeof(float) * Length;

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

    public float[] ToData()
    {
        return [pos.x, pos.y,  UV.x, UV.y,  color.r, color.g, color.b, color.a];
    }

    public static float[] ToData(IEnumerable<Vertex> vertices)
    {
        Vertex[] vs = vertices as Vertex[] ?? vertices.ToArray();
        return vs.SelectMany(v => v.ToData()).ToArray();
    }

    public static Vertex[] ToVertices(IEnumerable<float> Data)
    {
        float[] data = Data as float[] ?? Data.ToArray();
        if (data.Length % Length != 0) throw new Exception("Cannot convert data stream into vertices since length of the stream is not a floor of vertex size");
        Vertex[] vs = new Vertex[data.Length / Length];
        for (int i=0; i<data.Length; i+=Length)
            vs[i/Length] = new Vertex(data[i], data[i+1], data[i+2], data[i+3], data[i+4], data[i+5], data[i+6], data[i+7]);
        return vs;
    }
}
