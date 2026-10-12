namespace FableEngine.Rendering.Backend;

public struct PipelineDescription(ShaderHandle shader, VertexLayout layout)
{
    public ShaderHandle shader = shader;
    public VertexLayout layout = layout;
    public BlendMode blendMode = BlendMode.Alpha;
    public CullMode cullMode = CullMode.None;
    public Topology topology = Topology.Triangles;
}