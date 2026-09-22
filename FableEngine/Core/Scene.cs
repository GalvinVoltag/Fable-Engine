using System.Diagnostics;
using System.Text.Json;
using FableEngine.Rendering;
using FableEngine.Math;
using FableEngine.Rendering;
using FableEngine.Transforms;
using Silk.NET.OpenGL;
using Shader = FableEngine.Rendering.Shader;

namespace FableEngine.Core;

/// <summary>
/// Scene is a class with its own Transform and draw calls. Other objects with transforms may be added as children and managed in here.
/// Scenes have their own attached shapes, and these shapes behave as masks during rendering.
/// </summary>
public class Scene : ManagedRoutineProvider, ITransformProvider
{
    // private static HashSet<Scene> AllScenes = [];
    private Transform sceneTransform;
    public T Transform<T>() where T : Transform => (T)transform;
    // private int nesting = 1;
    // private Matrix4x4 drawMatrix = new();
    
    public string name { get; set; }
    // public Rectangle rectangle => shapeRenderer.shape.shapeRect;
    public Rectangle rectangle
    {
        get => rect.rect;
        set => rect.rect = value;
    }

    public Rect rect;
    
    ///// <summary>
    ///// A list of all Scenes
    ///// </summary>
    // public new static HashSet<Scene> All => AllScenes; // pending for method-based getter for security
    /// <summary>
    /// Background color of this Scene
    /// </summary>
    public Color backgroundColor = Color.softBlue;
    public float width
    {
        get => rect.width;
        set => rect.width = value;
    }
    public float height {
        get=> rect.height;
        set => rect.height = value;
    }
    /// <summary>
    /// Shape of this Scene. Everything in this Scene will be drawn inside this shape.
    /// </summary>
    public ShapeRenderer shapeRenderer;

    public Mask mask;
    
    /// <summary>
    /// Size of the Scene. This is in pixels if the Scene has no parent, and relative to its parent otherwise.
    /// </summary>
    public Vector2 size {
        get => new Vector2(width, height);
        set { width = value.x; height = value.y; }
    }

    /// <summary>
    /// Transform of this Scene, this is used for putting other objects with transform in this scene
    /// </summary>
    public Transform transform
    {
        get => sceneTransform;
        set => sceneTransform = value;
    }

    public Scene(string name = "Scene")
    {
        // AllScenes.Add(this);
        sceneTransform = new Transform2D(this);
        this.name = name;
        rect = AddRoutine<Rect>();
        (shapeRenderer = AddRoutine<ShapeRenderer>()).shape = rectangle;
        mask = new Mask(shapeRenderer.Render);
    }

    public new void Destroy()
    {
        if (destroyed) return;
        if (!transform.destroyed) transform.Destroy();
        // foreach (var t in sceneTransform.children)
        //     t.parent = null;
        // AllScenes.Remove(this);
        base.Destroy();
    }

    /// <summary>
    /// Get the first Scene parent of a given transform. This will traverse through all parents and return the first one that is a Scene.
    /// </summary>
    /// <param name="transform"> target transform </param>
    /// <returns> the first parent of given transform that is a Scene </returns>
    public static Scene? GetSceneOf(Transform transform)
    {
        Transform parent = transform;
        while (parent.parent != null)
        {
            parent = parent.parent;
        }
        if (parent.attachedObject is Scene)
            return (Scene)parent.attachedObject;
        
        return null;
    }

    // Todo: pending implementation
    public static Scene LoadFromFile(string path)
    {
        string json = File.ReadAllText(path);
        Scene? newScene = JsonSerializer.Deserialize<Scene>(json);
        if (newScene == null) return new Scene();
    
        ReCacheTransformParents(newScene.transform, null);
    
        return newScene;
    }

    // Todo: pending implementation or removal
    public static void ReCacheTransformParents(Transform t, Transform? parent)
    {
        t.parent = parent;
    
        foreach (var child in t.children)
            ReCacheTransformParents(child, t);
    }

    // Todo: pending implementation
    public void SaveToFile(string path)
    {
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            IncludeFields = true 
        };
        
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Set the current render mask as this Scene's shape.
    /// (this function may be removed or carried to other classes in the future, use it with caution)
    /// </summary>
    public void SetMask()
    {
        GL gl = Renderer.current.gl;
        gl.ColorMask(false, false, false, false);
        
        gl.Clear(ClearBufferMask.StencilBufferBit); // clear stencil
        gl.StencilFunc(StencilFunction.Always, 1, 0xFF);
        gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
        
        shapeRenderer.Render();
        
        gl.ColorMask(true, true, true, true);
        
        gl.StencilFunc(StencilFunction.Equal, 1, 0xFF);
        gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
    }
    
    /// <summary>
    /// Draw this Scene
    /// </summary>
    public override void Draw()
    {
        if (!active) return;

        if (transform.parent == null)
        {
            float scaleX = 1f / (Renderer.current.window.Size.X / 2f);
            float scaleY = 1f / (Renderer.current.window.Size.Y / 2f);
            
            transform.OverrideMatrix(new([
                scaleX, 0, 0, 0,
                0, scaleY, 0, 0,
                0, 0, 1, 0,
                -1, -1, 0, 1
            ]));
        }
        shapeRenderer.shape = rectangle;
        shapeRenderer.color = backgroundColor;
        
        mask.Set();
        
        foreach (Routine R in routines)
        {
            if (R is not RenderRoutine rr) continue;
            Engine.RunEnclosed(rr.Render);
        }
        foreach (Transform t in transform.children)
            if (t.attachedObject is ManagedRoutine { active: true } MR) MR.Draw();
        foreach (Routine R in routines)
        {
            if (R is not RenderRoutine rr) continue;
            Engine.RunEnclosed(rr.PostRender);
        }

        mask.UnSet();
    }
}
