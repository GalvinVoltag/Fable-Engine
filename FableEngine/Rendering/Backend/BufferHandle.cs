namespace FableEngine.Rendering.Backend;

public readonly record struct BufferHandle()
{
    private static uint nextID = 1;
    private readonly uint uniqueid = nextID++;
}