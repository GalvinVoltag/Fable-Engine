using FableEngine.Debugging;
using Silk.NET.OpenGL;

namespace FableEngine.Rendering;

public static class ShaderUtility
{
    // Returns a compiled shader program handle
    public static uint CreateShaderProgram(GL gl, string vertexSource, string fragmentSource, string name)
    {
        // Compile vertex shader
        uint vertexShader = CompileShader(gl, ShaderType.VertexShader, vertexSource, name + "_vertex");
        if (vertexShader == 0) return 0;
        
        // Compile fragment shader
        uint fragmentShader = CompileShader(gl, ShaderType.FragmentShader, fragmentSource, name + "_fragment");
        if (fragmentShader == 0) return 0;
        
        // Link into program
        uint program = gl.CreateProgram();
        gl.AttachShader(program, vertexShader);
        gl.AttachShader(program, fragmentShader);
        gl.LinkProgram(program);
        
        // Check linking
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int success);
        if (success == 0)
        {
            string log = gl.GetProgramInfoLog(program);
            Debug.Log($"Shader program linking failed for {name}: {log}", DebugLogLevel.Error);
            gl.DeleteProgram(program);
            return 0;
        }
        
        // Clean up individual shaders (we don't need them after linking)
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
        
        Debug.Log($"Shader program '{name}' compiled successfully!", DebugLogLevel.Success);
        return program;
    }
    
    private static uint CompileShader(GL gl, ShaderType type, string source, string name)
    {
        uint shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out int success);
        if (success == 0)
        {
            string log = gl.GetShaderInfoLog(shader);
            Debug.Log($"Shader compilation failed for {name}: {log}", DebugLogLevel.Error);
            gl.DeleteShader(shader);
            return 0;
        }
        
        return shader;
    }
}
