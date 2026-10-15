using FableEngine.Rendering.Backend;
using Silk.NET.Windowing;

namespace FableEngine.Rendering;

public class Renderer
{
    // current renderer to render
    public static Renderer current;
    
    public Renderer(IRenderBackend backend, IWindow window)
    {
        this.backend = backend;
        this.window = window;
    }
    
    public IRenderBackend backend;
    public IWindow window { get; set; }
}