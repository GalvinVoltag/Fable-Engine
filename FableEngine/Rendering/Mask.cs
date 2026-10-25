using FableEngine.Debugging;
using FableEngine.Math;
using FableEngine.Rendering.Backend;
using Silk.NET.OpenGL;

namespace FableEngine.Rendering;

public struct Mask(Action? draw = null, Func<bool>? inputMask = null)
{
    private static Stack<Mask> masks = [];

    public static bool TestCurrentInputMask(Vector2 mousePosition)
    {
        foreach (Mask mask in masks)
            if (mask.inputMask != null && !mask.inputMask())
                return false;
        
        return true;
    }

    private int index = 0;

    public StencilFunction func = StencilFunction.Gequal;
    /// <summary>
    /// void Method to draw the shape of the mask
    /// </summary>
    public Action? draw = draw;
    public Func<bool>? inputMask = inputMask;

    /// <summary>
    /// Set this mask as the current draw mask, draw operations will default to only draw inside this mask.
    /// </summary>
    public void Set()
    {
        index = masks.Count + 1;
        masks.Push(this);
        IncrementMask();
    }

    /// <summary>
    /// Set the previous mask as the current draw mask, and discard this one
    /// </summary>
    public void UnSet()
    {
        DecrementMask();

        int di = index;
        Mask m = masks.Pop();
        while (m.index > index)
        {
            m.DecrementMask();
            m = masks.Pop();
        }
            
        // foreach (Mask m in masks)
        //     if (m.index >= index) m.DecrementMask();
        // masks = masks.Where(m => m.index < di).ToArray();
        
    }
    
    private void IncrementMask()
    {
        if (Renderer.current.backend is not OpenGL) return;
        GL gl = ((OpenGL)Renderer.current.backend).gl;
        gl.ColorMask(false, false, false, false);
        
        gl.StencilFunc(StencilFunction.Equal, index - 1, 0xFF);
        gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Incr);

        draw?.Invoke();
        
        gl.ColorMask(true, true, true, true);
        
        gl.StencilFunc(StencilFunction.Equal, index, 0xFF);
        gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
    }
    private void DecrementMask()
    {
        if (Renderer.current.backend is not OpenGL) return;
        GL gl = ((OpenGL)Renderer.current.backend).gl;
        gl.ColorMask(false, false, false, false);
        
        gl.StencilFunc(StencilFunction.Equal, index, 0xFF);
        gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Decr);

        
        draw?.Invoke();
        
        gl.ColorMask(true, true, true, true);
        
        gl.StencilFunc(StencilFunction.Equal, index-1, 0xFF);
        gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
    }
}