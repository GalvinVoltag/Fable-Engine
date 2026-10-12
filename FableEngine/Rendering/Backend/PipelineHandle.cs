namespace FableEngine.Rendering.Backend;

public readonly record struct PipelineHandle()
{
    private static uint nextID = 1;
    private readonly uint uniqueid = nextID++;
}