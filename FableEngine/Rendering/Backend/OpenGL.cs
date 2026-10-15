using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FableEngine.Core;
using FableEngine.Debugging;
using FableEngine.Math;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;

namespace FableEngine.Rendering.Backend;

public struct TextureDescription(int width, int height, ColorComponents colorComponents)
{
    public int width = width;
    public int height = height;
    public ColorComponents components = colorComponents;
}

public struct BackendInfo
{
    public string Name;            // "OpenGL", "Vulkan", "Null"
    public string DeviceName;      // glGetString(GL_RENDERER)
    public Version ApiVersion;     // or string if you prefer
    public int MaxTextureUnits;
    public int MaxTextureSize;
    public int MaxTextureAnisotropy;
    public bool SupportsCompute;
    public bool SupportsDepthClamp;
    // one-off oddities go in a string[] or (string, string)[]
    public (string key, string value)[] Extensions;
}

public class OpenGL : IRenderBackend, IDestroyable, IDisposable
{
    public class GLShaderInfo(uint id, IShaderSource source)
    {
        public uint id = id;
        public Dictionary<string, UniformInfo> uniforms = new();
        public IShaderSource source = source;
    }

    public struct UniformInfo
    {
        public int location;
        public UniformType type;
    }

    public BackendInfo Info => new BackendInfo()
    {
        // Commented out lines that I don't understand, don't blame me
        Name = "OpenGL",
        // DeviceName = gl.GetString(),
        ApiVersion = new Version(0, 2, 0),
        // MaxTextureUnits = 8,
        // MaxTextureSize = 64000,
        // MaxTextureAnisotropy = 5096,
        // SupportsCompute = true,
        // SupportsDepthClamp = true,
    };

    public void BeginFrame()
    {
        return;
    }

    public void EndFrame()
    {
        return;
    }

    public void BeginPass(RenderTargetHandle? renderTarget = null, Color? clearColor = null, float? clearDepth = null,
        int? clearStencil = null)
    {
        // todo: set render target
        ClearBufferMask mask = ClearBufferMask.None;
        if (clearColor != null)
        {
            mask |= ClearBufferMask.ColorBufferBit;
            gl.ClearColor(clearColor.Value.r, clearColor.Value.g, clearColor.Value.b, clearColor.Value.a);
        }
        if (clearDepth != null)
        {
            mask |= ClearBufferMask.DepthBufferBit;
            gl.ClearDepth(clearDepth.Value);
        }
        if (clearStencil != null)
        {
            mask |= ClearBufferMask.StencilBufferBit;
            gl.ClearStencil(clearStencil.Value);
        }
        gl.Clear(mask);
    }

    public void EndPass()
    {
        return;
    }

    public void WaitForGPU()
    {
        throw new NotImplementedException();
    }

    public void SetViewport(int x, int y, int width, int height)
    {
        gl.Viewport(x, y, (uint)width, (uint)height);
    }

    public void SetScissor(int x, int y, int width, int height)
    {
        throw new NotImplementedException();
    }

    // private List<OpenGL> All = new();
    private readonly Dictionary<BufferHandle, (uint id, int size)> buffers = new(); // vbo
    private readonly Dictionary<VertexLayout, uint> layouts = new(); // vao
    private readonly Dictionary<ShaderHandle, GLShaderInfo> shaders = new();
    private readonly Dictionary<TextureHandle, uint> textures = new();
    private readonly Dictionary<PipelineHandle, PipelineDescription> pipelines = new();

    private readonly BufferHandle[] currentBuffers = new BufferHandle[8];
    private readonly TextureHandle[] currentTextures = new TextureHandle[8]; // was this 16?
    private PipelineHandle currentPipeline;

    // Todo: consider this:
    public static readonly Dictionary<Type, Func<OpenGL, IShaderSource, uint>> compilers = new Dictionary<Type, Func<OpenGL, IShaderSource, uint>>
    {
        {
            typeof(GLSLSource),
            (ogl, source) =>
            {
                GLSLSource glsl = (GLSLSource)source;
                
                uint vertexShader = CompileShader(ogl, glsl.vertexSource, ShaderType.VertexShader);
                uint fragmentShader = CompileShader(ogl, glsl.fragmentSource, ShaderType.FragmentShader);
                if (vertexShader == 0 || fragmentShader == 0) { 
                    ogl.gl.DeleteShader(vertexShader);
                    ogl.gl.DeleteShader(fragmentShader);
                    return 0;
                }
                
                // Link to program
                uint id = ogl.gl.CreateProgram();
                ogl.gl.AttachShader(id, vertexShader);
                ogl.gl.AttachShader(id, fragmentShader);
                ogl.gl.LinkProgram(id);
        
                // Clean up individual shaders after linking
                ogl.gl.DeleteShader(vertexShader);
                ogl.gl.DeleteShader(fragmentShader);
                
                return id;
            }
        }
    };

    // public Func<IShaderSource, int> compiler = (IShaderSource s) => 0;
    
    public GL gl;

    public string Name => "OpenGL";
    
    public OpenGL(IWindow source)
    {
        // BufferUsageFlags usage = BufferUsageFlags.VertexBufferBit;
        
        gl = source.CreateOpenGL();
        gl.Enable(EnableCap.Blend);
        gl.Enable(EnableCap.StencilTest);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        // All.Add(this);
    }

    public GLShaderInfo GetShaderInfo(ShaderHandle shader) => shaders[shader];

    public uint GetVertexBufferID(BufferHandle b)
    {
        return buffers[b].id;
    }
    
    public bool CreateBuffer(BufferHandle gb, BufferDescription description, ReadOnlySpan<byte> Data)
    {
        uint buffer = gl.GenBuffer();
        
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
        gl.BufferData<byte>(BufferTargetARB.ArrayBuffer, Data, 
            description.dynamic ? BufferUsageARB.DynamicDraw : BufferUsageARB.StaticDraw);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

        DeleteBuffer(gb);
        buffers.Add(gb, (buffer, description.size));
        return true;
    }
    public void DeleteBuffer(BufferHandle go)
    {
        if (!buffers.TryGetValue(go, out var d)) return;
        gl.DeleteBuffer(d.id);
        buffers.Remove(go);
    }

    public bool CreateShader(ShaderHandle s, IShaderSource source)
    {
        uint shaderid = 0;
        if (compilers.TryGetValue(source.GetType(), out Func<OpenGL, IShaderSource, uint>? compile))
        {
            shaderid = compile(this, source);
        }
        else
        {
            throw new NotSupportedException("this type of shader source is not supported by OpenGL backend.");
        }
        
        if (shaderid == 0) return false;
        GLShaderInfo shaderInfo = new GLShaderInfo(shaderid, source);
        
        // Check linking
        gl.GetProgram(shaderInfo.id, ProgramPropertyARB.LinkStatus, out int success);
        if (success == 0)
        {
            string log = gl.GetProgramInfoLog(shaderInfo.id);
            Debug.Log($"Shader program linking failed: {log}", DebugLogLevel.Error);
            gl.DeleteProgram(shaderInfo.id);
            return false;
        }
        gl.GetProgram(shaderInfo.id, ProgramPropertyARB.ActiveUniforms, out int uniforms);
        for (int i = 0; i < uniforms; i++)
        {
            UniformInfo info = new UniformInfo();
            // gl.GetActiveUniform(shaderInfo.id, (uint)i, out _, out info.type);
            gl.GetActiveUniform(shaderInfo.id, (uint)i, 128, out _, out _, out info.type, out string name);
            info.location = gl.GetUniformLocation(shaderInfo.id, name);
            if (info.location < 0) continue;

            // shaderInfo.uniforms ??= new Dictionary<string, UniformInfo>();
            shaderInfo.uniforms[name] = info;
        }
        
        // Debug.Log($"Shader program compiled successfully!", DebugLogLevel.Success);
        
        if (shaders.TryAdd(s, shaderInfo)) return true;
        gl.DeleteProgram(shaders[s].id);
        shaders[s] = shaderInfo;
        return true;
    }
    public void DeleteShader(ShaderHandle s)
    {
        if (!shaders.TryGetValue(s, out GLShaderInfo? program)) return;
        gl.DeleteProgram(program.id);
        shaders.Remove(s);
    }

    public bool CreateTexture(TextureHandle t, TextureDescription desc, ReadOnlySpan<byte> data)
    {
        if (textures.ContainsKey(t))
        {
            gl.DeleteTexture(textures[t]);
            textures.Remove(t);
        }
        textures[t] = gl.GenTexture();
        
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, textures[t]);
        
        (InternalFormat internalFormat, PixelFormat pixelFormat) = desc.components switch
        {
            ColorComponents.Grey => (InternalFormat.R8, PixelFormat.Red),
            ColorComponents.GreyAlpha => (InternalFormat.RG8, PixelFormat.RG),
            ColorComponents.RedGreenBlue => (InternalFormat.Rgb, PixelFormat.Rgb),
            ColorComponents.RedGreenBlueAlpha => (InternalFormat.Rgba, PixelFormat.Rgba),
            _ => (InternalFormat.Rgba, PixelFormat.Rgba) // fallback
        };
        
        gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        gl.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, (uint)desc.width,
            (uint)desc.height, 0, pixelFormat, PixelType.UnsignedByte, data);
        
        if (desc.components == ColorComponents.Grey)
        {
            gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureSwizzleG, (int)GLEnum.Red);
            gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureSwizzleB, (int)GLEnum.Red);
        }
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.Repeat);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.Repeat);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.BindTexture(TextureTarget.Texture2D, 0);
        
        return true;
    }
    public void DeleteTexture(TextureHandle t)
    {
        if (!textures.TryGetValue(t, out uint texture)) return;
        gl.DeleteTexture(texture);
        textures.Remove(t);
    }

    public bool CreatePipeline(PipelineHandle p, PipelineDescription description)
    {
        if (!shaders.ContainsKey(description.shader))
            throw new InvalidOperationException("Pipeline references a shader that hasn't been created.");
        pipelines[p] = description;
        return true;
    }
    public void DeletePipeline(PipelineHandle p)
    {
        pipelines.Remove(p);
    }

    
    public void SetUniformData(PipelineHandle p, string name, ReadOnlySpan<byte> data)
    {
        GLShaderInfo shader = shaders[pipelines[p].shader];
        if (!shader.uniforms.TryGetValue(name, out var slot)) return;
        bool changeBack = false;
        if (currentlyUsedShader != pipelines[p].shader) {
            gl.UseProgram(shaders[pipelines[p].shader].id); changeBack = true;
        }
        if (slot.location < 0) return;
        
        ReadOnlySpan<float> f = MemoryMarshal.Cast<byte, float>(data);
        ReadOnlySpan<int> i = MemoryMarshal.Cast<byte, int>(data);
        ReadOnlySpan<uint> u = MemoryMarshal.Cast<byte, uint>(data);

        switch (slot.type)
        {
            case UniformType.Float:     gl.Uniform1(slot.location, MemoryMarshal.Read<float>(data)); break;
            case UniformType.FloatVec2: gl.Uniform2(slot.location, f); break;
            case UniformType.FloatVec3: gl.Uniform3(slot.location, f); break;
            case UniformType.FloatVec4: gl.Uniform4(slot.location, f); break;
            case UniformType.FloatMat4: gl.UniformMatrix4(slot.location, 1, false, f); break;
            case UniformType.Int:
            case UniformType.Sampler2D: gl.Uniform1(slot.location, i); break;
            default: throw new NotSupportedException($"Unhandled GL type {slot.type}");
        }
        
        if (changeBack && shaders.TryGetValue(currentlyUsedShader, out var shader1))
            gl.UseProgram(shader1.id);
    }

    public void SetBuffer(BufferHandle gb, int slot = 0)
    {
        currentBuffers[slot] = gb;
    }

    public void SetTexture(TextureHandle t, int slot = 0)
    {
        if (currentTextures[slot] == t) return;
        gl.ActiveTexture((TextureUnit)slot);
        gl.BindTexture(TextureTarget.Texture2D, textures[t]);
        currentTextures[slot] = t;
    }

    public void SetPipeline(PipelineHandle p)
    {
        currentPipeline = p;
    }

    public void Draw(int count = 1)
    {
        PipelineDescription desc = pipelines[currentPipeline];
        // gl.UseProgram(shaders[desc.shader].id);
        UseShader(desc.shader);
        ApplyPipeline(currentPipeline);
        
        // one VAO per layout, created the first time we see that layout
        if (!layouts.TryGetValue(desc.layout, out uint vao))
            layouts[desc.layout] = vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, buffers[currentBuffers[0]].id);
        
        IntPtr pointer = 0;
        for (int i = 0; i < desc.layout.Length; i++)
        {
            gl.VertexAttribPointer((uint)i, desc.layout[i].size, Map(desc.layout[i].type), desc.layout[i].normalized, (uint)desc.layout.Size, pointer );
            gl.EnableVertexAttribArray((uint)i);
            pointer += desc.layout[i].size * VertexLayout.SizeOf(desc.layout[i].type);
        }
        
        for (int i = 0; i < currentTextures.Length; i++)
        {
            if (!textures.ContainsKey(currentTextures[i]) || textures[currentTextures[i]] == 0) continue;
            SetTexture(currentTextures[i], i);
        }
        
        
        // gl.DrawArrays(Map(desc.topology), 0, (uint)count);
        gl.DrawArrays(Map(desc.topology), 0, (uint)(buffers[currentBuffers[0]].size / desc.layout.Size));
    }

    public static Silk.NET.OpenGL.PrimitiveType Map(Topology topology)
    {
        return (topology) switch
        {
            Topology.Triangles => Silk.NET.OpenGL.PrimitiveType.Triangles,
            Topology.TriangleStrip => Silk.NET.OpenGL.PrimitiveType.TriangleStrip,
            Topology.Lines => Silk.NET.OpenGL.PrimitiveType.Lines,
            Topology.LineStrip => Silk.NET.OpenGL.PrimitiveType.LineStrip,
            Topology.Points => Silk.NET.OpenGL.PrimitiveType.Points,
            _=> throw new NotSupportedException("this topology is not supported!")
        };
    }
    public static BlendingFactor Map(BlendFactor factor)
    {
        return (factor) switch
        {
            BlendFactor.Zero => BlendingFactor.Zero,
            BlendFactor.One => BlendingFactor.One,
            BlendFactor.SrcColor => BlendingFactor.SrcColor,
            BlendFactor.OneMinusSrcColor => BlendingFactor.OneMinusSrcColor,
            BlendFactor.DstColor => BlendingFactor.DstColor,
            BlendFactor.OneMinusDstColor => BlendingFactor.OneMinusDstColor,
            BlendFactor.SrcAlpha => BlendingFactor.SrcAlpha,
            BlendFactor.OneMinusSrcAlpha => BlendingFactor.OneMinusSrcAlpha,
            BlendFactor.DstAlpha => BlendingFactor.DstAlpha,
            BlendFactor.OneMinusDstAlpha => BlendingFactor.OneMinusDstAlpha,
            BlendFactor.ConstantColor => BlendingFactor.ConstantColor,
            BlendFactor.OneMinusConstantColor => BlendingFactor.OneMinusConstantColor,
            BlendFactor.ConstantAlpha => BlendingFactor.ConstantAlpha,
            BlendFactor.OneMinusConstantAlpha => BlendingFactor.OneMinusConstantAlpha,
            _ => throw new NotSupportedException()
        };
    }
    public static BlendEquationModeEXT Map(BlendOp op)
    {
        return (op) switch
        {
            BlendOp.Add => BlendEquationModeEXT.FuncAdd,
            BlendOp.Subtract => BlendEquationModeEXT.FuncSubtract,
            BlendOp.ReverseSubtract => BlendEquationModeEXT.FuncReverseSubtract,
            BlendOp.Min => BlendEquationModeEXT.Min,
            BlendOp.Max => BlendEquationModeEXT.Max,
            _ => throw new NotSupportedException()
        };
    }
    public static VertexAttribPointerType Map(ParameterType type)
    {
        return type switch
        {
            ParameterType.Float => VertexAttribPointerType.Float,
            ParameterType.Byte => VertexAttribPointerType.Byte,
            ParameterType.Short => VertexAttribPointerType.Short,
            ParameterType.Int => VertexAttribPointerType.Int,
            ParameterType.Half => VertexAttribPointerType.HalfFloat,
            ParameterType.Double => VertexAttribPointerType.Double,
            ParameterType.UnsignedByte => VertexAttribPointerType.UnsignedByte,
            ParameterType.UnsignedShort => VertexAttribPointerType.UnsignedShort,
            ParameterType.UnsignedInt => VertexAttribPointerType.UnsignedInt,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void ApplyPipeline(PipelineHandle pipeline)
    {
        PipelineDescription d = pipelines[pipeline];
        if (d.blendMode.Enabled)
        {
            gl.Enable(EnableCap.Blend);
            gl.BlendFuncSeparate(Map(d.blendMode.color.Source), Map(d.blendMode.color.Destination), Map(d.blendMode.alpha.Source), Map(d.blendMode.alpha.Destination));
            gl.BlendEquationSeparate(Map(d.blendMode.color.Operation), Map(d.blendMode.alpha.Operation));
        }
        else gl.Disable(EnableCap.Blend);
        
        if (d.cullMode != CullMode.None)
        {
            gl.Enable(EnableCap.CullFace);
            gl.CullFace(d.cullMode == CullMode.Front ? GLEnum.Front : GLEnum.Back);
        }
        else gl.Disable(EnableCap.CullFace);

        // if (d.Depth.TestEnabled) { gl.Enable(EnableCap.DepthTest); gl.DepthFunc(Map(d.Depth.Compare)); }
        // else gl.Disable(EnableCap.DepthTest);
        // gl.DepthMask(d.Depth.WriteEnabled);
        gl.Disable(EnableCap.DepthTest);
    }

    
    public static uint CompileShader(OpenGL opengl, string source, ShaderType type)
    {
        GL gl = opengl.gl;
        uint shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out int success);
        if (success != 0) return shader;
        
        string log = gl.GetShaderInfoLog(shader);
        Debug.Log($"Shader compilation failed: {log}", DebugLogLevel.Error);
        gl.DeleteShader(shader);
        return 0;
    }

    private ShaderHandle currentlyUsedShader;
    public void UseShader(ShaderHandle shader)
    {
        if (currentlyUsedShader == shader) return;
        gl.UseProgram(shaders[shader].id);
        currentlyUsedShader = shader;
    }

    public void Dispose()
    {
        foreach ((uint vbo, int _) in buffers.Values) gl.DeleteBuffer(vbo);
        foreach (uint vao in layouts.Values) gl.DeleteVertexArray(vao);
        foreach (GLShaderInfo program in shaders.Values) gl.DeleteProgram(program.id);
        foreach (uint texture in textures.Values) gl.DeleteTexture(texture);

        buffers.Clear();
        layouts.Clear();
        shaders.Clear();
        textures.Clear();

        gl.Dispose();
        // All.Remove(this);
    }

    public bool destroyed { get; private set; }
    public void Destroy()
    {
        destroyed = true;
        Engine.DisposeLater(this);
    }
}