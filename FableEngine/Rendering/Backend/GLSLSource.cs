namespace FableEngine.Rendering.Backend;

public record struct GLSLSource(string fragmentSource, string vertexSource) : IShaderSource
{
    public string fragmentSource = fragmentSource;
    public string vertexSource = vertexSource;
    public override string ToString()
    {
        return fragmentSource + "\n" + vertexSource;
    }
}