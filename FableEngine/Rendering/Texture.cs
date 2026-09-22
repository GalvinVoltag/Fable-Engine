using FableEngine.Debugging;
using FableEngine.Math;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace FableEngine.Rendering;

public class Texture
{
    // a white single pixel texture as default
    public static readonly ImageResult defaultImage = new() { Data = [255, 255, 255], Height = 1, Width = 1, Comp = ColorComponents.RedGreenBlue, SourceComp = ColorComponents.RedGreenBlue};
    public static readonly Texture defaultTexture = new() { IR = defaultImage };
    private ImageResult IR = new();
    private Dictionary<GL, uint> textures = [];
    private bool compiled = false;

    public int width => IR.Width;
    public int height => IR.Height;
    public Vector2 size => new(width, height);

    ~Texture()
    {
        foreach (var ogl in textures.Keys)
            ogl.DeleteTexture(textures[ogl]);
    }
    public uint GetID()
    {
        // if (Renderer.current.gl == null)
        //     return 0;

        if (!textures.ContainsKey(Renderer.current.gl) || !compiled)
            Compile(Renderer.current.gl);

        return textures[Renderer.current.gl];
    }

    public void LoadImage(string path, ColorComponents comps)
    {
        try {
            IR = ImageResult.FromMemory(File.ReadAllBytes(path), comps);
        }catch (Exception e) {
            Debug.Log("Image load was failed, path: " + path + "\n    fallback to default texture", ConsoleColor.Red);
            IR = defaultTexture.IR;
        }
        compiled = false;
    }
    
    public void LoadFromData(byte[] data, int width, int height, ColorComponents comps)
    {
        try {
            IR = new ImageResult
            {
                Width = width,
                Height = height,
                Comp = ColorComponents.Grey,
                SourceComp = ColorComponents.Grey,
                Data = data
            };
        }catch (Exception e) {
            Debug.Log("Image load was failed fallback to default texture", ConsoleColor.Red);
            Debug.Log(e.Message, ConsoleColor.Red);
            IR = defaultTexture.IR;
        }
        compiled = false;
    }
    
    private void Compile(GL gl)
    {
        if (!compiled)
        {
            foreach (var ogl in textures.Keys)
                ogl.DeleteTexture(textures[ogl]);
            textures.Clear();
        }
        if (textures.ContainsKey(gl))
        {
            gl.DeleteTexture(textures[gl]);
            textures.Remove(gl);
        }
        textures[gl] = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, textures[gl]);
        ImageResult result = IR;
        if (result.Data == null)
            result = defaultTexture.IR;
        (InternalFormat internalFormat, PixelFormat pixelFormat) = result.Comp switch
        {
            ColorComponents.Grey => (InternalFormat.R8, PixelFormat.Red),
            ColorComponents.GreyAlpha => (InternalFormat.RG8, PixelFormat.RG),
            ColorComponents.RedGreenBlue => (InternalFormat.Rgb, PixelFormat.Rgb),
            ColorComponents.RedGreenBlueAlpha => (InternalFormat.Rgba, PixelFormat.Rgba),
            _ => (InternalFormat.Rgba, PixelFormat.Rgba) // fallback
        };
        gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        gl.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, (uint)result.Width,
            (uint)result.Height, 0, pixelFormat, PixelType.UnsignedByte, result.Data);
        if (result.Comp == ColorComponents.Grey)
        {
            gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureSwizzleG, (int)GLEnum.Red);
            gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureSwizzleB, (int)GLEnum.Red);
        }
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.Repeat);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.Repeat);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
        gl.BindTexture(TextureTarget.Texture2D, 0);
        compiled = true;
    }
}
