namespace FableEngine.Rendering.Backend;

public readonly struct GLSLSource(string vertex, string fragment) : IShaderSource
{
    public readonly string vertexSource = vertex;
    public readonly string fragmentSource = fragment;
    public override string ToString()
    {
        return vertexSource + "\n" + fragmentSource;
    }
}

public readonly struct NullShaderSource : IShaderSource;