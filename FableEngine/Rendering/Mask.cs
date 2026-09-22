using FableEngine.Rendering;
using Silk.NET.OpenGL;

namespace FableEngine.Rendering;

public struct Mask(Action? draw = null)
{
    private static Mask[] masks = [];

    private int index = 0;

    public StencilFunction func = StencilFunction.Gequal;
    /// <summary>
    /// void Method to draw the shape of the mask
    /// </summary>
    public Action? draw = draw;

    /// <summary>
    /// Set this mask as the current draw mask, draw operations will default to only draw inside this mask.
    /// </summary>
    public void Set()
    {
        index = masks.Length + 1;
        masks = [.. masks, this];
        IncrementMask();
    }

    /// <summary>
    /// Set the previous mask as the current draw mask, and discard this one
    /// </summary>
    public void UnSet()
    {
        DecrementMask();

        int di = index;
        foreach (Mask m in masks)
            if (m.index >= index) m.DecrementMask();
        masks = masks.Where(m => m.index < di).ToArray();
        
    }
    
    private void IncrementMask()
    {
        GL gl = Renderer.current.gl;
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
        GL gl = Renderer.current.gl;
        gl.ColorMask(false, false, false, false);
        
        gl.StencilFunc(StencilFunction.Equal, index, 0xFF);
        gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Decr);

        
        draw?.Invoke();
        
        gl.ColorMask(true, true, true, true);
        
        gl.StencilFunc(StencilFunction.Equal, index-1, 0xFF);
        gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
    }
}