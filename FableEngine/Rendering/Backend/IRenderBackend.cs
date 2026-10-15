using FableEngine.Math;

namespace FableEngine.Rendering.Backend;

public readonly record struct RenderTargetHandle()
{
    private static uint nextID = 1;
    private readonly uint uniqueid = nextID++;
}

public interface IRenderBackend
{
    // INFORMATION
    public string Name => Info.Name;
    public BackendInfo Info { get; }
    
    // FRAME CONTROL
    public void BeginFrame();
    public void EndFrame(); 
    public void BeginPass(RenderTargetHandle? renderTarget = null, Color? clearColor = null, float? clearDepth = null, int? clearStencil = null);
    public void EndPass(); 
    public void WaitForGPU();
    
    // VIEWPORT
    public void SetViewport(int x, int y, int width, int height);
    public void SetScissor(int x, int y, int width, int height);
    
    // RESOURCE CREATION - DELETION
    public bool CreateBuffer(BufferHandle gb, BufferDescription description, ReadOnlySpan<byte> Data);
    public void DeleteBuffer(BufferHandle bufferHandle);
    public bool CreateShader(ShaderHandle s, IShaderSource source);
    public void DeleteShader(ShaderHandle s);
    public bool CreateTexture(TextureHandle t, TextureDescription desc, ReadOnlySpan<byte> Data);
    public void DeleteTexture(TextureHandle t);
    public bool CreatePipeline(PipelineHandle p, PipelineDescription description);
    public void DeletePipeline(PipelineHandle p);
    
    // STATE CONTROL
    public void SetUniformData(PipelineHandle p, string name, ReadOnlySpan<byte> data);
    public void SetBuffer(BufferHandle gb, int slot = 0);
    public void SetTexture(TextureHandle t, int slot = 0);
    public void SetPipeline(PipelineHandle p);
    
    // ACTION
    public void Draw(int count = 1);
}