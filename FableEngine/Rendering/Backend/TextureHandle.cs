namespace FableEngine.Rendering.Backend;

public readonly record struct TextureHandle()
{
    private static uint nextID = 1;
    private readonly uint uniqueid = Interlocked.Increment(ref nextID);
}