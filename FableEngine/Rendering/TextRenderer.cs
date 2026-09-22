using FableEngine.Math;
using FableEngine.Rendering;
using Silk.NET.OpenGL;
using Shader = FableEngine.Rendering.Shader;

namespace FableEngine.Rendering;

public class TextRenderer : RenderRoutine
{
    public static readonly Shader defaultFontShader = new Shader(
        "default Font Shader", 
        """
        #version 330 core
        layout (location = 0) in vec2 aPosition;
        layout (location = 1) in vec2 aUV;
        layout (location = 2) in vec4 aColor;
        out vec2 fragUV;
        out vec4 vertexColor;
        uniform mat4 transform;
        uniform vec4 color;
        void main() {
          gl_Position = transform * vec4(aPosition, 0.0, 1.0);
          fragUV = vec2(aUV.x, 1.0 - (aUV.y));
          vertexColor = aColor * color;
        }
        """, 
        """
        #version 330 core
        in vec2 fragUV;
        in vec4 vertexColor;
        out vec4 FragColor;
        uniform sampler2D uTexture;
        uniform float edgeRoughness = 10;
        uniform float edgeThickness = 0;
        uniform float outlineRoughness = 100;
        uniform float outlineThickness = 0;
        uniform vec4 outlineColor = vec4(0, 0, 0, 1);
        void main() {
            vec4 alpha = texture(uTexture, fragUV);
            
            float a = (alpha.r-(1-outlineThickness)*0.5);
            float edge = (alpha.r-(1-edgeThickness)*0.5)*outlineRoughness;
            vec4 col = vertexColor * edge + outlineColor * (1-edge);
            FragColor = vec4(col.rgb, a*edgeRoughness);
        }
        """);
    public FontAtlas fontAtlas;
    public Polygon textPolygon;
    public static string defaultFont = "/usr/share/fonts/noto/NotoSans-Regular.ttf";

    public Rectangle textRect
    {
        private set;
        get
        {
            if (outOfDate) textPolygon.data = RegenerateMesh();
            return field;
        }
    }
    private bool outOfDate = true;
    
    public enum HorizontalAlignment { Left, Right, Center }
    public enum VerticalAlignment { Bottom, Top, Center }

    public float fontSize
    {
        get;
        set
        {
            field = value;
            outOfDate = true;
        }
    } = 24;
    public float edgeRoughness
    {
        get;
        set
        {
            field = value;
            outOfDate = true;
        }
    } = 10;
    public float edgeThickness
    {
        get;
        set
        {
            field = value;
            outOfDate = true;
        }
    } = 0;
    public float outlineRoughness
    {
        get;
        set
        {
            field = value;
            outOfDate = true;
        }
    } = 10;
    public float outlineThickness
    {
        get;
        set
        {
            field = value;
            outOfDate = true;
        }
    } = 0;
    public Color color
    {
        get;
        set
        {
            field = value;
            outOfDate = true;
        }
    } = Color.white;
    public Color outlineColor
    {
        get;
        set
        {
            field = value;
            outOfDate = true;
        }
    } = Color.black;
    public string text
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            outOfDate = true;
        }
    } = "default";

    public HorizontalAlignment horizontalAlignment
    {
        get;
        set { field = value; outOfDate = true; }
    } = HorizontalAlignment.Left;
    
    public VerticalAlignment verticalAlignment
    {
        get;
        set { field = value; outOfDate = true; }
    } = VerticalAlignment.Bottom;

    public TextRenderer() : base(defaultFontShader)
    {
        if (!FontAtlas.allFonts.ContainsKey(defaultFont))
        {
            FontAtlas.allFonts[defaultFont] = new FontAtlas(defaultFont);
        }
        fontAtlas = FontAtlas.allFonts[defaultFont];
        textPolygon = new Polygon(RegenerateMesh());
        // shader = defaultFontShader;
    }

    public override void Render()
    {
        shader.Use();
        // if (transform == null) return;
        if (outOfDate)
        {
            textPolygon.data = RegenerateMesh();
        }
        
        GL gl =  Renderer.current.gl;
        
        shader.SetMatrix4("transform", transform.matrix, gl);
        
        shader.SetColor("color", color, gl);
        shader.SetColor("outlineColor", outlineColor, gl);
        shader.SetFloat("outlineThickness", outlineThickness, gl);
        shader.SetFloat("outlineRoughness", outlineRoughness, gl);
        shader.SetFloat("edgeRoughness", edgeRoughness, gl);
        shader.SetFloat("edgeThickness", edgeThickness, gl);
        shader.SetTexture("uTexture", fontAtlas.texture, gl);
        textPolygon.Draw();
    }

    public float[] RegenerateMesh()
    {
        outOfDate = false;
        Vertex[] verts = new Vertex[text.Length*6];
        List<int> lines = new List<int>();
        List<float> lineLengths = new List<float>();
        Vector2 Pen = new Vector2();
        textRect = new Rectangle(0, 0, 0, 0);
        float spaceLength = fontAtlas.maxWidth/fontAtlas.size.x*fontSize*4;
        float lineHeight = 1.1f;
        int vertIndex = 0;
        for (int i=0; i<text.Length;i++)
        {
            char c = text[i];
            switch (c)
            {
                case ' ':
                    Pen.x += spaceLength;
                    continue;
                case '\n':
                    lines.Add((i+1)*6);
                    lineLengths.Add(Pen.x);
                    Pen.x = 0;
                    Pen.y -= lineHeight * (fontAtlas.maxHeight / fontAtlas.size.y * fontSize)*4;
                    continue;
                case '\t':
                    Pen.x += MathF.Ceiling(Pen.x / (spaceLength*4)) * (spaceLength*4);
                    continue;
            }

            if (!fontAtlas.chars.TryGetValue(c, out var ci)) continue;
            
            Vector2 letterOffset = (ci.offset / new Vector2(fontAtlas.maxHeight, fontAtlas.maxWidth)) * fontSize;
            Vector2 uvBottomLeft = ci.uvOffset;
            Vector2 uvTopRight = ci.uvOffset + ci.size/fontAtlas.size;
            Vector2 bottomLeft = Pen + letterOffset;
            Vector2 topRight = ci.size;
            topRight = topRight * (fontSize / fontAtlas.maxHeight) + Pen + letterOffset;
            verts[i*6+0] = new Vertex(bottomLeft, uvBottomLeft, Color.white);
            verts[i*6+1] = new Vertex(bottomLeft.x, topRight.y, uvBottomLeft.x ,uvTopRight.y, Color.white);
            verts[i*6+2] = new Vertex(topRight.x, bottomLeft.y, uvTopRight.x, uvBottomLeft.y, Color.white);
            verts[i*6+3] = verts[i*6+1];
            verts[i*6+4] = verts[i*6+2];
            verts[i*6+5] = new Vertex(topRight, uvTopRight, Color.white);
            Pen.x += topRight.x - Pen.x;
            // if (textRect.Left > Pen.x) textRect.Left = Pen.x;
            // if (textRect.Bottom > Pen.y) textRect.Bottom = Pen.y;
            // if (textRect.Right < topRight.x) textRect.Right = topRight.x;
            // if (textRect.Top < topRight.y) textRect.Top = topRight.y;
            if (textRect.Left > Pen.x) textRect = textRect with { Left = Pen.x };
            if (textRect.Bottom > Pen.y) textRect = textRect with { Bottom = Pen.y };
            if (textRect.Right < topRight.x) textRect = textRect with { Right = topRight.x };
            if (textRect.Top < topRight.y) textRect = textRect with { Top = topRight.y };
            if (i == text.Length - 1)
            {
                lines.Add((i+1)*6);
                lineLengths.Add(Pen.x);
            }
            // textRect = new Rectangle(textRect.position*fontSize, textRect.size*fontSize);
        }
        
        if (horizontalAlignment != HorizontalAlignment.Left)
        {
            int currentLineIndex = 0;
            for (int i = 0; i < verts.Length; i++)
            {
                if (currentLineIndex >= lines.Count) break;
                if (i == lines[currentLineIndex]) currentLineIndex++;
                if (horizontalAlignment == HorizontalAlignment.Center)
                    verts[i].pos.x -= textRect.size.x - lineLengths[currentLineIndex] / 2;
                else
                    verts[i].pos.x -= lineLengths[currentLineIndex];
            }
        }
        
        if (verticalAlignment != VerticalAlignment.Bottom)
        {
            for (int i = 0; i < verts.Length; i++)
            {
                if (horizontalAlignment == HorizontalAlignment.Center)
                    verts[i].pos.y -= textRect.size.y/2;
                else
                    verts[i].pos.y -= textRect.size.y;
            }
        }

        List<float> data = new List<float>();
        foreach (Vertex v in verts)
        {
            data.Add(v.pos.x);
            data.Add(v.pos.y);
            data.Add(v.UV.x);
            data.Add(v.UV.y);
            data.Add(v.color.r);
            data.Add(v.color.g);
            data.Add(v.color.b);
            data.Add(v.color.a);
        }
        outOfDate = false;
        return data.ToArray();
    }
}