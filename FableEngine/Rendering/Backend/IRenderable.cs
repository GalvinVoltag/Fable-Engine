using FableEngine.Rendering;

namespace FableEngine.Rendering.Backend;

public interface IRenderable
{
    Shader shader { get; }
    void Render();
}