using System.Xml.Serialization;
using FableEngine.Math;

namespace FableEngine.Rendering;

public interface Shape
{
    bool PointIntersection(Vector2 point);
    void Draw();
    Rectangle shapeRect { get; }
}