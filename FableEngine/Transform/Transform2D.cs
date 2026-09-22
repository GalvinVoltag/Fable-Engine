using System.Text.Json.Serialization;
using FableEngine.Math;
using FableEngine.Core;
using FableEngine.Math;
using Silk.NET.GLFW;
using Silk.NET.Windowing;

namespace FableEngine.Transforms;





public class Transform2D(ITransformProvider attachment, Transform? parent = null) : Transform(attachment, parent)
{
    private Vector2 wp = new(); // world position
    private Vector2 p = new(); // position
    private float wr;  // world rotation
    private float r;  // rotation
    
    /// <summary>
    /// Calculates local transform matrix using the position, rotation and scale of the transform
    /// </summary>
    /// <returns>local transform matrix, not including matrix override</returns>
    public override Matrix4x4 GetLocalTransformMatrix()
    {
        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);

        Matrix4x4 objectTransformMatrix = new([
            cos * scale.x, -sin * scale.x, 0, 0,
            sin * scale.y, cos * scale.y, 0, 0,
            0, 0, 1, 0,
            position.x, position.y, 0, 1
        ]);
        return objectTransformMatrix;
    }
    public static Matrix4x4 TransformToMatrix4(Vector2 position, float rotation, Vector2 scale)
    {
        float cos = 1;
        float sin = 0;
        if (rotation != 0) {
            cos = MathF.Cos(rotation);
            sin = MathF.Sin(rotation);
        }
    
        Matrix4x4 objectTransformMatrix = new([
            cos *scale.x, -sin * scale.x, 0,         0,
            sin *scale.y,  cos *scale.y,  0,         0,
            0,             0,             1,         0,
            position.x,    position.y,    0,         1
        ]);
        return objectTransformMatrix;
    }
    
    public Vector2 WorldToLocal(Vector2 worldPoint)
    {
        Vector2 inParentSpace = (parent is Transform2D t2d) 
            ? t2d.WorldToLocal(worldPoint) 
            : worldPoint;
    
        // 1. Translate
        Vector2 local = inParentSpace - position;  // local position, not world
    
        // 2. Rotate 
        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);
        local = new Vector2(
            local.x * cos - local.y * sin,
            local.x * sin + local.y * cos
        );
    
        // 3. Scale 
        if (scale.x != 0) local.x /= scale.x;
        if (scale.y != 0) local.y /= scale.y;
    
        return local;
    }


    public Vector2 up => new( float.Sin(rotation), float.Cos(rotation) );
    public Vector2 right => new( float.Cos(rotation), -float.Sin(rotation) );

    public float worldx
    {
        get => worldPosition.x;
        set => worldPosition = worldPosition with { x = value };
    }
    public float worldy
    {
        get => worldPosition.y;
        set => worldPosition = worldPosition with { y = value };
    }
    public Vector2 worldPosition
    {
        get
        {
            if (cached) return wp;
            if (parent == null) { cached = true; return wp; }
            if (parent is Transform2D t2d)
                wp = t2d.localToWorld * position;
            cached = true;
            return wp;
        }
        set
        {
            wp = value;
            if (parent != null && parent is Transform2D t2d) p = t2d.worldToLocal * wp;
            cached = true;
            foreach (Transform child in children)
                child.cached = false;
        }
    }

    public float x
    {
        get => position.x;
        set => position = position with { x = value };
    }
    public float y
    {
        get => position.y;
        set => position = position with { y = value };
    }

    public override IVector Position
    {
        get => position;
        set => position = (Vector2)value;
    }

    public Vector2 position
    {
        get => parent == null ? wp : p;
        set
        {
            if (parent == null) worldPosition = value;
            p = value;
            cached = false;
        }
    }

    public float worldRotation
    {
        get
        {
            if (cached) return wr;
            if (parent == null) { cached = true; return wr; }
            if (parent is Transform2D t2d)
                wr = t2d.worldRotation + rotation;
            cached = true;
            return wr;
        }
        set
        {
            wr = value;
            if (parent is Transform2D t2d) r = wr - t2d.worldRotation;
            cached = true;
            foreach (Transform child in children)
                child.cached = false;
        }
    }

    [Slidable(-Single.Pi, Single.Pi)]
    public float rotation
    {
        get => parent == null ? wr : r;
        set
        {
            if (value.Equals(r)) return;
            cached = false;
            r = value;
            if (parent == null) worldRotation = value;
        }
    }

    public Vector2 scale
    {
        get;
        set
        {
            if (field == value) return;
            cached = false;
            field = value;
        }
    } = new Vector2(1);
}