namespace FableEngine.Rendering.Backend;

public record struct BlendMode(bool Enabled, BlendChannel color, BlendChannel alpha)
{
    public static readonly BlendMode None = new(false, new BlendChannel(), new BlendChannel());
    public static readonly BlendMode Alpha = new(true,
        new BlendChannel( BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha, BlendOp.Add ),
        new BlendChannel( BlendFactor.One, BlendFactor.OneMinusSrcAlpha, BlendOp.Add )
    );
    public static readonly BlendMode Additive = new(true, 
        new BlendChannel(BlendFactor.SrcAlpha, BlendFactor.One, BlendOp.Add),
        new BlendChannel(BlendFactor.One, BlendFactor.One, BlendOp.Add)
    );
}