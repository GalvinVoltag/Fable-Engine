namespace FableEngine.Math;

public struct Triangle
{
    public Vector2 a = new();
    public Vector2 b = new();
    public Vector2 c = new();

    public Triangle(Vector2 a, Vector2 b, Vector2 c)
    {
        this.a = a;
        this.b = b;
        this.c = c;
    }
}