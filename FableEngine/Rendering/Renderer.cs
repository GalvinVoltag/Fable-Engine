using FableEngine.Math;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace FableEngine.Rendering;

public interface IRenderable
{
    Shader shader { get; }
    void Render();
}

public class Renderer
{
    // current renderer to render
    public static Renderer current;

    // todo: remove these two
    // public static GL currentGL;
    // public static IWindow currentWindow;
    // list of all renderers that are connected to this renderer
    private static Dictionary<Shader, List<IRenderable>> Renderables = new();
    
    
    public Renderer(GL gl, IWindow window)
    {
        this.gl = gl;
        this.window = window;
    }
    
    public GL gl;
    public IWindow window { get; set; }
    public Matrix4x4 matrix = Matrix4x4.identity;

    public void RegisterRenderable(IRenderable renderable)
    {
        if (!Renderables.ContainsKey(renderable.shader)) Renderables.Add(renderable.shader, new List<IRenderable>());
        Renderables[renderable.shader].Add(renderable);
    }

    public void UnregisterRenderable(IRenderable renderable)
    {
        if (Renderables.ContainsKey(renderable.shader)) Renderables[renderable.shader].Remove(renderable);
        if (Renderables[renderable.shader].Count == 0) Renderables.Remove(renderable.shader);
    }

    public void RenderAll()
    {
        
    }
}