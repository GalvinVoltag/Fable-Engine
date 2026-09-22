using FableEngine.Core;
using FableEngine.Transforms;
using FableEngine.Math;

namespace FableEngine.Rendering;

public abstract class RenderRoutine : Routine, IRenderable
{
    public static readonly Dictionary<Shader, Dictionary<Transform, HashSet<RenderRoutine>>> All = new();
    
    [ShowDeclared]
    public Shader shader { get; protected set; }

    public RenderRoutine(Shader? sourceShader = null)
    {
        shader = sourceShader?? GameWindow.defaultShader;
        GameWindow.GetWindowOf(transform)?.renderer.RegisterRenderable(this);
        if (!All.ContainsKey(shader)) 
            All.Add(shader, new Dictionary<Transform, HashSet<RenderRoutine>>());
        if (!All[shader].ContainsKey(transform))
            All[shader].Add(transform, new HashSet<RenderRoutine>());
        All[shader][transform].Add(this);
    }

    public override void Destroy()
    {
        All[shader][transform].Remove(this);
        if (All[shader][transform].Count == 0) All[shader].Remove(transform);
        if (All[shader].Count == 0) All.Remove(shader);
    }
    
    public virtual void Render()
    {
        // shader.Use();
    }
    
    public virtual void PostRender()
    {
        
    }
}
