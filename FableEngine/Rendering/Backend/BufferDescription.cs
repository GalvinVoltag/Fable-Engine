namespace FableEngine.Rendering.Backend;

public record struct BufferDescription(BufferType type, int size, bool dynamic = false);