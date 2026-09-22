using System.Runtime.CompilerServices;
using FableEngine.Core;
using FableEngine.Debugging;
using FableEngine.Math;
using Silk.NET.OpenGL;

namespace FableEngine.Rendering;
public class Shader : INameable
{
    /// <summary>
    /// current shader that is being used to draw
    /// </summary>
    public static Shader current { get; private set; }
    private static GL currentGl { get; set; }
    private static Dictionary<string, Shader> all = [];
    public static Matrix4x4 matrix = new Matrix4x4();

    public static void ClearGL(GL gl)
    {
        foreach (Shader shader in all.Values)
        {
            shader.shaderPrograms.Remove(gl);
        }
    }
    
    
    private readonly Dictionary<GL, uint> shaderPrograms = [];
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
            foreach (var sp in shaderPrograms)
                sp.Key?.DeleteProgram(sp.Value);
        }
        all.Remove(name);
    }

    public static Shader Find(string name)
    {
        return all[name];
    }
    
    

    public static string[] GetAllShaderNames()
    {
        return all.Keys.ToArray();
    }

    public uint GetShaderProgram(GL gl)
    {
        if (!shaderPrograms.ContainsKey(gl))
            Compile(gl);
        return shaderPrograms[gl];
    }
    public void Use(GL? gl = null)
    {
        gl ??= Renderer.current.gl;
        if (current == this && currentGl == gl) return;
        if (!shaderPrograms.ContainsKey(gl))
            Compile(gl);
        if (compiled && shaderPrograms[gl] != 0)
        {
            gl.UseProgram(shaderPrograms[gl]);
            current = this;
            currentGl = gl;
        }
    }
    public bool Compile(GL? gl = null)
    {
        if (gl == null) return false;
        if (vertexShaderSource == "" ||  fragmentShaderSource == "")
        {
            Debug.Log("compilation failed due to empty shader source!", DebugLogLevel.Error);
            return false;
        }
        if (compiled) // delete shader before recompiling it
        {
            if (shaderPrograms.TryGetValue(gl, out var program))
                gl.DeleteProgram(program);
            else
                foreach (var sp in shaderPrograms)
                    sp.Key.DeleteProgram(sp.Value);
        }

        // if (gl != null)
            if (!shaderPrograms.ContainsKey(gl))
                shaderPrograms.Add(gl, ShaderUtility.CreateShaderProgram(gl, vertexShaderSource, fragmentShaderSource, name));
        // else
        //     foreach (var sp in shaderPrograms)
        //         shaderPrograms[sp.Key] = ShaderUtility.CreateShaderProgram(sp.Key, vertexShaderSource, fragmentShaderSource, name);
        compiled = shaderPrograms.ElementAt(0).Value != 0;
        return compiled;
    }

    public void Delete(GL? gl)
    {
        if (gl == null) return;
        if (!shaderPrograms.ContainsKey(gl)) return;
        gl.DeleteProgram(shaderPrograms[gl]);
        shaderPrograms.Remove(gl);
    }

    public void Delete()
    {
        foreach (var sp in shaderPrograms)
            sp.Key.DeleteProgram(sp.Value);
        shaderPrograms.Clear();
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

    public void SetMatrix4(string variableName, Matrix4x4 matrix4, GL gl)
    {
        if (variableName == "transform") matrix = matrix4;
        int loc = gl.GetUniformLocation(GetShaderProgram(gl), variableName);
        gl.UniformMatrix4(loc, 1, false, matrix4.data);
    }
    
    public void SetMatrix4(string variableName, Matrix4x4 matrix4)
    {
        if (variableName == "transform") matrix = matrix4;
        int loc = Renderer.current.gl.GetUniformLocation(GetShaderProgram(Renderer.current.gl), variableName);
        Renderer.current.gl.UniformMatrix4(loc, 1, false, matrix4.data);
    }

    public void SetVector4(string variableName, float x, float y, float z, float w, GL gl)
    {
        int loc = gl.GetUniformLocation(GetShaderProgram(gl), variableName);
        gl.Uniform4(loc, x, y, z, w);
    }
    public void SetVector4(string variableName, float x, float y, float z, float w)
    {
        int loc = Renderer.current.gl.GetUniformLocation(GetShaderProgram(Renderer.current.gl), variableName);
        Renderer.current.gl.Uniform4(loc, x, y, z, w);
    }
    
    public void SetColor(string variableName, Color color, GL gl)
    {
        int loc = gl.GetUniformLocation(GetShaderProgram(gl), variableName);
        gl.Uniform4(loc, color.r, color.g, color.b, color.a);
    }
    public void SetColor(string variableName, Color color)
    {
        int loc = Renderer.current.gl.GetUniformLocation(GetShaderProgram(Renderer.current.gl), variableName);
        Renderer.current.gl.Uniform4(loc, color.r, color.g, color.b, color.a);
    }
    
    public void SetFloat(string variableName, float f, GL gl)
    {
        int loc = gl.GetUniformLocation(GetShaderProgram(gl), variableName);
        gl.Uniform1(loc, f);
    }
    public void SetFloat(string variableName, float f)
    {
        int loc = Renderer.current.gl.GetUniformLocation(GetShaderProgram(Renderer.current.gl), variableName);
        Renderer.current.gl.Uniform1(loc, f);
    }

    public void SetTexture(string variableName, Texture texture, GL gl, TextureUnit unit = TextureUnit.Texture0)
    {
        int loc = gl.GetUniformLocation(GetShaderProgram(gl), variableName);
        gl.Uniform1(loc, 0);
        gl.ActiveTexture(unit);
        gl.BindTexture(TextureTarget.Texture2D, texture.GetID());
    }
    public void SetTexture(string variableName, Texture texture, TextureUnit unit = TextureUnit.Texture0)
    {
        int loc = Renderer.current.gl.GetUniformLocation(GetShaderProgram(Renderer.current.gl), variableName);
        Renderer.current.gl.Uniform1(loc, 0);
        Renderer.current.gl.ActiveTexture(unit);
        Renderer.current.gl.BindTexture(TextureTarget.Texture2D, texture.GetID());
    }

}
