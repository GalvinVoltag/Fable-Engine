namespace FableEngine.Rendering.Backend;

public readonly record struct ShaderHandle()
{
    private static uint nextID = 1;
    private readonly uint uniqueid = Interlocked.Increment(ref nextID);
}