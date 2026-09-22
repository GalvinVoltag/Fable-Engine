using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using FableEngine.Core;
using FableEngine.Math;
using FableEngine.Transforms;
using StbImageSharp;
using StbTrueTypeSharp;

namespace FableEngine.Rendering;

public class FontAtlas
{
    public static Dictionary<string, FontAtlas> allFonts = new Dictionary<string, FontAtlas>();
    
    struct CharData(int width, int height, int offsetx, int offsety, byte[] data, char character)
    {
        public int width = width, height = height;
        public int offsetx = offsetx, offsety = offsety;
        public byte[] data = data;
        public char character = character;
    }

    public struct CharInfo(Vector2 uvOffset, Vector2 size, Vector2 offset)
    {
        public Vector2 uvOffset = uvOffset;
        public Vector2 size = size;
        public Vector2 offset = offset;
    }
    
    byte[] ttfBytes;

    StbTrueType.stbtt_fontinfo fontInfo = new StbTrueType.stbtt_fontinfo();

    public readonly Texture texture;
    public Vector2 size;
    public float maxWidth = 0;
    public float maxHeight = 0;
    public readonly Dictionary<char, CharInfo> chars = new Dictionary<char, CharInfo>();

    public FontAtlas(string file)
    {
        ttfBytes = File.ReadAllBytes(file);

        texture = Generate();
        size = texture.size;
    }

    public FontAtlas()
    {
        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "fc-match",
                Arguments = "--format=%{file}",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        
        process.Start();

        string fontPath = process.StandardOutput.ReadToEnd().Trim();
        allFonts[fontPath] = new FontAtlas(fontPath);

        process.WaitForExit();
        
        ttfBytes = File.ReadAllBytes(fontPath);

        texture = Generate();
        size = texture.size;
    }
    
    private Texture Generate()
    {
        const int start = 32;
        const int end = 128;
        maxHeight = 0;
        maxWidth = 0;
        CharData[] chardatas =  new CharData[end-start];
        // int maxHeight = 0;
        
        unsafe
        {

            fixed (byte* ttfPtr = ttfBytes)
            {
                StbTrueType.stbtt_InitFont(fontInfo, ttfPtr, 0);
                float scale = StbTrueType.stbtt_ScaleForPixelHeight(fontInfo, 128f);
                for (int i = start; i < end; i++)
                {
                    char c = (char)i;
                    int glyphWidth;
                    int glyphHeight;
                    int xOffset;  // pixels from the returned bitmap's left edge to the glyph origin
                    int yOffset;  // pixels from the returned bitmap's top edge to the glyph origin
                    byte* sdfData = StbTrueType.stbtt_GetCodepointSDF(
                        fontInfo,
                        scale,
                        c,
                        padding: 4,
                        onedge_value: 128, // 128 is exactly at edge
                        pixel_dist_scale: 16f, // gradient reach
                        &glyphWidth,
                        &glyphHeight,
                        &xOffset,
                        &yOffset
                    );

                    if (sdfData == null || glyphHeight == 0 || glyphWidth == 0)
                    {
                        continue;
                    }

                    byte[] managed = new byte[glyphWidth * glyphHeight];
                    Marshal.Copy((IntPtr)sdfData, managed, 0, managed.Length);

                    StbTrueType.stbtt_FreeSDF(sdfData, null);
                    chardatas[i-start] = new CharData(glyphWidth, glyphHeight, xOffset, yOffset, managed, c);
                    if (glyphHeight > maxHeight) maxHeight = glyphHeight;
                    if (glyphWidth > maxWidth) maxWidth = glyphWidth;
                }
            }
            
        }
        
        int atlasWidth = 1024;
        int penX = 0;
        int penY = 0;
        int rowHeight = 0;

        // get minimum required height
        foreach (CharData character in chardatas)
        {
            if (character.width == 0) continue;

            if (penX + character.width >= atlasWidth)
            {
                penX = 0;
                penY += rowHeight;
                rowHeight = 0;
            }

            if (rowHeight < character.height) rowHeight = character.height;
            penX += character.width;
        }
        int atlasHeight = penY + rowHeight;

        // write to atlas
        byte[] atlas = new byte[atlasWidth * atlasHeight];
        penX = 0;
        penY = 0;
        rowHeight = 0;

        foreach (CharData character in chardatas)
        {
            if (character.width == 0) continue;

            if (penX + character.width >= atlasWidth)
            {
                penX = 0;
                penY += rowHeight;
                rowHeight = 0;
            }
            
            Vector2 atlasSize = new  Vector2(atlasWidth, atlasHeight);
            chars[character.character] = new CharInfo(
                new Vector2(penX, atlasHeight-(float)penY-character.height) / atlasSize, 
                new Vector2(character.width, character.height),
                new Vector2(character.offsetx, 1 - character.offsety - character.height));

            for (int row = 0; row < character.height; row++)
            {
                int srcOffset = row * character.width;
                int dstOffset = (penY + row) * atlasWidth + penX;
                for (int j=0; j<character.width; j++)
                    atlas[dstOffset+j] = character.data[srcOffset+j];
            }
            penX += character.width;
            if (rowHeight < character.height) rowHeight = character.height;
        }
        
        Texture tex = new Texture();
        tex.LoadFromData(atlas, atlasWidth, atlasHeight, ColorComponents.Grey);
        return tex;

    }
}