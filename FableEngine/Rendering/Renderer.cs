using System.Collections;
using System.IO.Pipelines;
using FableEngine.Rendering.Backend;
using FableEngine.Math;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace FableEngine.Rendering;

// public class Vulkan : IRenderBackend todo later
// {
//     
// }

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