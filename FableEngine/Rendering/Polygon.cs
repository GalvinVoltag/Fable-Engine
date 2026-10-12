
using FableEngine.Math;
using FableEngine.Rendering.Backend;
using FableEngine.Transforms;
using Silk.NET.OpenGL;

namespace FableEngine.Rendering;

public class Polygon : Shape
{
    public enum PrimitiveShape { triangle, square, square_centered }
    private static Dictionary<PrimitiveShape, Polygon> primitiveShapes = new()
    {
        { PrimitiveShape.triangle, new Polygon([
            new Vertex(     0,   .57735f,  0.5f, 1.0f,  Color.white ),
            new Vertex(-.500f, -.288675f,  0.0f, 0.25f, Color.white ),
            new Vertex( .500f, -.288675f,  1.0f, 0.25f, Color.white )
        ]) },
        { PrimitiveShape.square, new Polygon([
            new Vertex(0, 0, 0, 0, Color.white ),
            new Vertex(0, 1, 0, 1, Color.white ),
            new Vertex(1, 0, 1, 0, Color.white ),
            new Vertex(0, 1, 0, 1, Color.white ),
            new Vertex(1, 0, 1, 0, Color.white ),
            new Vertex(1, 1, 1, 1, Color.white )
        ]) },
        { PrimitiveShape.square_centered, new Polygon([
            new Vertex(-.5f, -.5f, 0, 0, Color.white ),
            new Vertex(-.5f,  .5f, 0, 1, Color.white ),
            new Vertex( .5f, -.5f, 1, 0, Color.white ),
            new Vertex(-.5f,  .5f, 0, 1, Color.white ),
            new Vertex( .5f, -.5f, 1, 0, Color.white ),
            new Vertex( .5f,  .5f, 1, 1, Color.white )
        ]) }
    };
    private List<IRenderBackend> hosts = [];
    private BufferHandle _handle = new();
    public BufferHandle handle
    {
        get
        {
            var backend = Renderer.current.backend;
            if (!compiled || !hosts.Contains(backend)) CompileShape(data);
            return _handle;
        }
    }
    
    private bool compiled = false;
    private byte[] _data;
    public byte[] data
    {
        get => _data;
        set {
            compiled = false;
            _data = value;
        }
    }

    public Rectangle shapeRect { get; private set; } = new Rectangle(0, 0, 1, 1);

    public void Draw()
    {
        ShapeRenderer.DrawPolygon(this);
    }

    public static bool PointVTriangle(Vector2 point, Triangle triangle)
    {
        Vector2 a = triangle.a;
        Vector2 b = triangle.b;
        Vector2 c = triangle.c;
        float u = ((b.y - c.y) * (point.x - c.x) + (c.x - b.x) * (point.y - c.y)) /
                  ((b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y));
        float v = ((c.y - a.y) * (point.x - c.x) + (a.x - c.x) * (point.y - c.y)) /
                  ((b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y));
        float w = 1 - u - v;

        return (u > 0 && v > 0 && w > 0);
    }
    
    public bool PointIntersection(Vector2 point)
    {
        Triangle[] ts = GetTriangles();
        return ts.Any(t => PointVTriangle(point, t));
    }

    private Vertex[] _vertices = [];
    private Vertex[] vertices
    {
        get
        {
            if (data.Length != 0 && _vertices.Length == 0)
                _vertices = Vertex.ToVertices(data);
            return _vertices;
        }
        set
        {
            _vertices = value;
            data = Vertex.ToData(value);
        }
    }

    ~Polygon()
    {
        foreach (var b in hosts)
        {
            var (backend, h) = (b, _handle);
            backend.DeleteBuffer(h);
        }
    }
    
    public byte[] GetRawData() { return data; }

    public Vector2[] GetPoints()
    {
        Vector2[] points = new Vector2[vertices.Length];
        for (int i=0; i<points.Length; i++)
            points[i] = vertices[i].pos;
        return points;
    }

    public Triangle[] GetTriangles()
    {
        Triangle[] triangles = new Triangle[vertices.Length/3];
        for (int i = 0; i < triangles.Length; i++)
            triangles[i] = new Triangle(vertices[i*3].pos, vertices[i*3+1].pos, vertices[i*3+2].pos);
        return triangles;
    }

    public static Polygon GetPrimitiveShape(PrimitiveShape shapeName)
    {
        return primitiveShapes[shapeName];
    }

    public Polygon(IEnumerable<byte> data, bool unreadable = false)
    {
        _data = data as byte[] ?? [.. data];
    }
    public Polygon(IEnumerable<Vertex> Vertices)
    {
        Vertex[] vertexArray = Vertices as Vertex[] ?? Vertices.ToArray();
        _data =  Vertex.ToData(vertexArray);
    }

    private void CompileShape(IEnumerable<byte> Data, bool unreadable = false)
    {
        IRenderBackend backend = Renderer.current.backend;

        if (!compiled) // data changed: drop stale copies everywhere
        {
            foreach (var host in hosts) host.DeleteBuffer(_handle);
            hosts.Clear();
        }

        if (!backend.CreateBuffer(_handle, new BufferDescription(BufferType.Vertex, _data.Length), _data))
            return;

        if (!hosts.Contains(backend)) hosts.Add(backend);
        compiled = true;

        Rectangle boundingRect = new Rectangle(0, 0, 0,0);
        foreach (var vertex in vertices)
        {
            if (vertex.pos.x < boundingRect.Left) boundingRect.Left = vertex.pos.x;
            if (vertex.pos.x > boundingRect.Right) boundingRect.Right = vertex.pos.x;
            if (vertex.pos.y < boundingRect.Bottom) boundingRect.Bottom = vertex.pos.y;
            if (vertex.pos.y > boundingRect.Top) boundingRect.Top = vertex.pos.y;
        }
        shapeRect = boundingRect;
        
        // Program.Log($"CompileShape: Creating VAO {vao} for GL context {gl.GetHashCode()}", ConsoleColor.Cyan);
    
        // gl.BindVertexArray(vao);
        // gl.BindBuffer(BufferTargetARB.ArrayBuffer, bufferID);
        // gl.BufferData<float>(BufferTargetARB.ArrayBuffer, dataArray, BufferUsageARB.StaticDraw);
        //
        // gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)Vertex.Size, 0);
        // gl.EnableVertexAttribArray(0);
        //
        // gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)Vertex.Size, (IntPtr)(2 * sizeof(float)));
        // gl.EnableVertexAttribArray(1);
        //
        // gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint)Vertex.Size, (IntPtr)(4 * sizeof(float)));
        // gl.EnableVertexAttribArray(2);
        //
        // gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        // gl.BindVertexArray(0);

        // ID[gl] = vao;
        // if (!hosts.Contains(Renderer.current.backend)) hosts.Add(Renderer.current.backend);
        // _data = dataArray;
        // compiled = true;
    }

}