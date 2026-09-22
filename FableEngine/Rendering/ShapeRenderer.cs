using System.ComponentModel;
using FableEngine.Core;
using FableEngine.Math;
using FableEngine.Transforms;
using Silk.NET.OpenGL;

namespace FableEngine.Rendering;

public class ShapeRenderer : RenderRoutine
{
    public Shape shape;
    public Color color = Color.white;
    public Rectangle uvOffset = new Rectangle(0, 0, 1, 1);
    public Texture texture = new Texture();
    
    public ShapeRenderer(Shader? shader = null, Shape? shape = null) : base(shader ?? GameWindow.defaultShader)
    {
        this.shape = shape??Polygon.GetPrimitiveShape(Polygon.PrimitiveShape.square);
    }

    public ShapeRenderer() : base(GameWindow.defaultShader)
    {
        shape = Polygon.GetPrimitiveShape(Polygon.PrimitiveShape.square);
    }
    public static void DrawPolygon(Polygon poly)
    {
        GL gl =  Renderer.current.gl;
        
        gl.BindVertexArray(poly.GetID());
        gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)(poly.GetRawData().Length/6));
    }

    public static void DrawRectangle(Shader? shader = null, Rectangle rect = new Rectangle(), Color? color = null, Matrix4x4 pMatrix = default)
    {
        shader ??= Shader.current;
        shader.Use();
        shader.SetMatrix4("transform", pMatrix * Transform2D.TransformToMatrix4( rect.position, 0, rect.size));
        shader.SetColor("color", color??Color.white);
        shader.SetTexture("uTexture", Texture.defaultTexture);
        ShapeRenderer.DrawPolygon(Polygon.GetPrimitiveShape(Polygon.PrimitiveShape.square));
    }

    public override void Render()
    {
        // if (transform == null) return;
        shader.Use();
        shader.SetMatrix4("transform", transform.matrix);
        shader.SetColor("color", color);
        shader.SetVector4("UVOffset", uvOffset.position.x, uvOffset.position.y, uvOffset.size.x, uvOffset.size.y);
        shader.SetTexture("uTexture", texture);
        shape.Draw();
    }
}
