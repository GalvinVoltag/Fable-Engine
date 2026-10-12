namespace FableEngine.Rendering.Backend;

public record struct BlendChannel(BlendFactor Source, BlendFactor Destination, BlendOp Operation);