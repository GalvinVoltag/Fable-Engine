namespace FableEngine.Rendering.Backend;

public enum Topology
{
    Triangles, // draw vertices 1-2-3, then 4-5-6 
    TriangleStrip,  // draw vertices 1-2-3, then 2-3-4
    Lines, 
    LineStrip, 
    Points 
    
}