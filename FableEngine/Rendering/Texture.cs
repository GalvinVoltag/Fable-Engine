using FableEngine.Debugging;
using FableEngine.Math;
using FableEngine.Rendering.Backend;
using StbImageSharp;

namespace FableEngine.Rendering;

public class Texture
{
    // a white single pixel texture as default
    public static readonly ImageResult defaultImage = new() { Data = [255, 255, 255], Height = 1, Width = 1, Comp = ColorComponents.RedGreenBlue, SourceComp = ColorComponents.RedGreenBlue};
    public static readonly Texture defaultTexture = new() { IR = defaultImage };
    private ImageResult IR = defaultImage;
    private List<IRenderBackend> hosts = [];

    public TextureHandle handle
    {
        get {
            if (!compiled || !hosts.Contains(Renderer.current.backend))
                Compile(Renderer.current.backend, field);
            return field;
        }
        set;
    } = new();

    private bool compiled = false;

    public int width => IR.Width;
    public int height => IR.Height;
    public Vector2 size => new(width, height);

    public Texture()
    {
        
    }

    ~Texture()
    {
        foreach (var backend in hosts)
            backend.DeleteTexture(handle);
    }

    public void LoadImage(string path, ColorComponents comps)
    {
        try {
            IR = ImageResult.FromMemory(File.ReadAllBytes(path), comps);
        }catch (Exception e) {
            Debug.Log("Image load was failed, path: " + path + "\n    fallback to default texture", DebugLogLevel.Error);
            IR = defaultTexture.IR;
        }
        compiled = false;
    }
    
    public void LoadFromData(byte[] data, int imageWidth, int imageHeight, ColorComponents comps)
    {
        try {
            IR = new ImageResult
            {
                Width = imageWidth,
                Height = imageHeight,
                Comp = ColorComponents.Grey,
                SourceComp = ColorComponents.Grey,
                Data = data
            };
        }catch (Exception e) {
            Debug.Log( DebugLogLevel.Error, "Image load was failed fallback to default texture");
            Debug.Log(e.Message, DebugLogLevel.Error);
            IR = defaultTexture.IR;
        }
        compiled = false;
    }
    
    private void Compile(IRenderBackend backend, TextureHandle textureHandle)
    {
        if (!compiled)
        {
            foreach (var host in hosts)
                host.DeleteTexture(textureHandle);
            hosts.Clear();
        }
        if (hosts.Contains(backend))
        {
            backend.DeleteTexture(textureHandle);
        }
        ImageResult result = IR; 
        compiled = backend.CreateTexture(textureHandle, new TextureDescription(width, height, result.Comp), result.Data);
        hosts.Add(backend);
    }
}
