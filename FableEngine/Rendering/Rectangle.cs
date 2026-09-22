using System.Diagnostics.CodeAnalysis;
using FableEngine.Math;
using FableEngine.Transforms;

namespace FableEngine.Rendering;

public record struct Rectangle : Shape, IParsable<Rectangle>
{
    public Vector2 position = new();
    public Vector2 size = new(1, 1);

    public Rectangle shapeRect => this;

    public Rectangle(float left, float bottom, float right, float top) {
        Left = left; Top = top; Right = right; Bottom = bottom;
    }

    public Rectangle(Vector2 position, Vector2 size, bool centered = false) {
        this.position = centered ? position - size / 2 : position;
        this.size = size;
    }
    
    public Rectangle(float position, float size, bool centered = false) {
        this.position = centered ? new Vector2(position) - new Vector2(size) / 2 : new Vector2(position);
        this.size = new Vector2(size);
    }

    public Rectangle(float allSides, bool centered = false) {
        Left = allSides; Right = allSides; Top = allSides; Bottom = allSides;
        if (centered) position -= size / 2;
    }
    
    public float width { get => size.x; set => size.x = value; }
    public float height { get => size.y; set => size.y = value; }
    public float Right { get => position.x + size.x;
        set => size.x = value - position.x; }
    public float Top { get => position.y + size.y;
        set => size.y = value - position.y; }
    public float Bottom { get => position.y;
        set { size.y = Top - value; position.y = value; }
    }
    public float Left { get => position.x;
        set { size.x = Right - value; position.x = value; }
    }

    public float LeftRight => Left + Right; 
    public float TopBottom => Top + Bottom; 

    public void Rescale(Vector2 size, Vector2 origin)
    {
        this.size = size;
        this.position = size * -origin;
    }
    public void Rescale(Vector2 size, bool centered = false)
    {
        this.size = size;
        if (centered) position = -size / 2;
    }
    public void Rescale(float x, float y , bool centered) => Rescale(new Vector2(x, y), centered);
    public void RescaleX(float x, float origin = 0.5f)
    {
        size.x = x;
        position.x = x * -origin;
    }
    public void RescaleY(float y, float origin = 0.5f)
    {
        size.y = y;
        position.y = y * -origin;
    }
    

    public bool PointIntersection(Vector2 point)
    {
        return (point.x > Left && point.x < Right && point.y > Bottom &&  point.y < Top);
    }

    public void Draw()
    {
        // if (Shader.current == null) return;
        Shader shader = Shader.current;
        shader.SetMatrix4("transform", Shader.matrix * Transform2D.TransformToMatrix4( position, 0, size));
        ShapeRenderer.DrawPolygon(Polygon.GetPrimitiveShape(Polygon.PrimitiveShape.square));
    }
    
    public static bool operator ==(Rectangle? a, Rectangle? b) => a?.position == b?.position && a?.size == b?.size;
    public static bool operator !=(Rectangle? a, Rectangle? b) => !(a == b);
    
    public override string ToString()
    {
        return "[" + Left + " " +  Bottom + " " + Right + " " + Top + "]";
    }
    public static Rectangle Parse(string s, IFormatProvider? provider = null)
    {
        string[] ss = s.TrimStart("[").TrimEnd("]").ToString().Replace(",", "").Split(" ");
        if (ss.Length != 4) return new();
        if (float.TryParse(ss[0], out float r) && float.TryParse(ss[1], out float g) &&
            float.TryParse(ss[2], out float b) && float.TryParse(ss[3], out float a))
            return new Rectangle(r, g, b, a);
        else
            return new();
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Rectangle result)
    {
        throw new NotImplementedException();
    }
}