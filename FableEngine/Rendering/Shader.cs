using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FableEngine.Core;
using FableEngine.Debugging;
using FableEngine.Math;
using FableEngine.Rendering.Backend;

namespace FableEngine.Rendering;

public class Shader : INameable, IParsable<Shader>
{
    /// <summary>
    /// current shader that is being used to draw
    /// </summary>
    public static Shader current { get; private set; }
    private static IRenderBackend currentBackend { get; set; }
    private static Dictionary<string, Shader> all = [];
    public static Matrix4x4 matrix = new Matrix4x4();

    public static void ClearGL(IRenderBackend backend)
    {
        foreach (Shader shader in all.Values)
        {
            shader.hosts.Remove(backend);
        }
    }
    
    
    private readonly List<IRenderBackend> hosts = [];
    private readonly ShaderHandle handle = new();
    private PipelineHandle pipeline = new PipelineHandle();
    private string vertexShaderSource = "";
    private string fragmentShaderSource = "";
    private bool compiled = false;
    
    public string name { get; set; }
    public bool autoCompile = true;

    public Shader(string name)
    {
        // gl = shaderGL;
        string newName = name;
        int i = 0;
        while (all.ContainsKey(newName)) {
            newName = name + "_" + i;
            i++;
        }
        this.name = newName;
        all[newName] = this;
    }
    public Shader(string name, string vertex, string fragment)
    {
        // gl = shaderGL;
        string newName = name;
        int i = 0;
        while (all.ContainsKey(newName)) {
            newName = name + "_" + i;
            i++;
        }
        this.name = newName;
        vertexShaderSource = vertex;
        fragmentShaderSource = fragment;
        all[newName] = this;
    }
    ~Shader() { 
        if (compiled)
        {
            foreach (var sp in hosts)
                sp.DeleteShader(handle);
        }
        all.Remove(name);
    }

    public static Shader Find(string name)
    {
        return all[name];
    }
    
    

    public static string[] GetAllShaderNames() => [.. all.Keys];
    
    public void Use(IRenderBackend? backend = null)
    {
        backend ??= Renderer.current.backend;
        if (current == this && currentBackend == backend) return;
        if (!hosts.Contains(backend) || !compiled)
            Compile(backend);
        if (!compiled) return;
        backend.CreatePipeline(pipeline, new PipelineDescription(handle, Vertex.Layout));
        backend.SetPipeline(pipeline);
        current = this;
        currentBackend = backend;
    }
    public bool Compile(IRenderBackend? backend = null)
    {
        if (backend == null) return false;
        if (vertexShaderSource == "" ||  fragmentShaderSource == "")
        {
            Debug.Log("compilation failed due to empty shader source!", DebugLogLevel.Error);
            return false;
        }
        if (compiled) // delete shader before recompiling it
        {
            backend.DeleteShader(handle);
        }

        if (!hosts.Contains(backend))
            hosts.Add(backend);
        compiled = backend.CreateShader(handle, new GLSLSource(fragmentShaderSource, vertexShaderSource));
        Debug.Log(DebugLogLevel.Success, ConsoleColor.Green, "Shader program ", ConsoleColor.Yellow, name, ConsoleColor.Green," compiled successfully.");
        return compiled;
    }

    public void Delete(IRenderBackend? backend)
    {
        if (backend == null) return;
        if (!hosts.Contains(backend)) return;
        backend.DeleteShader(handle);
        hosts.Remove(backend);
    }

    public void Delete()
    {
        foreach (var sp in hosts)
            sp.DeleteShader(handle);
        hosts.Clear();
        all.Remove(name);
    }

    public string GetVertexSource() { return vertexShaderSource; }
    public string GetFragmentSource() { return fragmentShaderSource; }
    
    public void SetVertexSource(string source)
    {
        vertexShaderSource = source;
        compiled = false;
        if (autoCompile) Compile();
    }
    public void SetFragmentSource(string source)
    {
        fragmentShaderSource = source;
        compiled = false;
        if (autoCompile) Compile();
    }
    public void SetSource(string vertex, string fragment)
    {
        vertexShaderSource = vertex;
        fragmentShaderSource = fragment;
        compiled = false;
        if (autoCompile) Compile();
    }

    public void SetMatrix4(string variableName, Matrix4x4 matrix4, IRenderBackend backend)
    {
        if (variableName == "transform") matrix = matrix4;
        // int loc = backend.GetUniformLocation(GetShaderProgram(backend), variableName);
        backend.SetUniformData(pipeline, variableName, MemoryMarshal.AsBytes(matrix4.data));
    }
    
    public void SetMatrix4(string variableName, Matrix4x4 matrix4)
    {
        SetMatrix4(variableName, matrix4, Renderer.current.backend);        
    }

    public void SetVector4(string variableName, float x, float y, float z, float w, IRenderBackend backend)
    {
        backend.SetUniformData(pipeline, variableName, MemoryMarshal.AsBytes([x, y, z, w]));
        // int loc = gl.GetUniformLocation(GetShaderProgram(gl), variableName);
        // gl.Uniform4(loc, x, y, z, w);
    }
    public void SetVector4(string variableName, float x, float y, float z, float w)
    {
        SetVector4(variableName, x, y, z, w, Renderer.current.backend);
    }
    
    public void SetColor(string variableName, Color color, IRenderBackend backend)
    {
        backend.SetUniformData(pipeline, variableName, MemoryMarshal.AsBytes([color.r, color.g, color.b, color.a]));
        // int loc = gl.GetUniformLocation(GetShaderProgram(gl), variableName);
        // gl.Uniform4(loc, color.r, color.g, color.b, color.a);
    }
    public void SetColor(string variableName, Color color)
    {
        SetColor(variableName, color, Renderer.current.backend);
    }
    
    public void SetFloat(string variableName, float f, IRenderBackend backend)
    {
        backend.SetUniformData(pipeline, variableName, MemoryMarshal.AsBytes([f]));
        // int loc = gl.GetUniformLocation(GetShaderProgram(gl), variableName);
        // gl.Uniform1(loc, f);
    }
    public void SetFloat(string variableName, float f)
    {
        SetFloat(variableName, f, Renderer.current.backend);
    }

    public void SetTexture(string variableName, Texture texture, IRenderBackend backend, int slot = 0)
    {
        // OpenGL ogl = (backend as OpenGL ??
        //               throw new Exception("setting textures on backends other than OpenGL are not supported yet."));
        // GL gl = ogl.gl;
        // int loc = gl.GetUniformLocation(ogl.GetShaderInfo(handle).id, variableName);
        backend.SetUniformData(pipeline, variableName, MemoryMarshal.AsBytes([slot]));
        backend.SetTexture(texture.handle, 0);
        // ogl.UseShader(handle);
        // gl.Uniform1(loc, 0);
        // gl.ActiveTexture(unit);
        // gl.BindTexture(TextureTarget.Texture2D, texture.GetID());
    }
    public void SetTexture(string variableName, Texture texture, int slot = 0)
    {
        SetTexture(variableName, texture, Renderer.current.backend, slot);
    }
    
    public static void Clear(IRenderBackend backend)
    {
        foreach (Shader shader in all.Values)
        {
            shader.hosts.Remove(backend);
        }
    }

    public override string ToString()
    {
        return name;
    }

    public static Shader Parse(string s, IFormatProvider? provider = null)
    {
        if (!all.TryGetValue(s, out Shader? shader))
            return all.FirstOrDefault().Value;
        return shader;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Shader result)
    {
        if (!all.TryGetValue(s??"", out Shader? shader)) {
            result = null;
            return false;
        }
        result = shader;
        return true;
    }
}