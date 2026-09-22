using System.Text.Json.Serialization;

namespace FableEngine.Core;

public interface IDestroyable
{
    public bool destroyed { get; }
    public void Destroy();
}

public class ManagedRoutineContainer : IDestroyable
{
    public readonly Dictionary<int, List<ManagedRoutine>> All = new();
    public readonly List<ManagedRoutine> NewBorns = [];
    public readonly List<ManagedRoutine> FrameRunners = [];
    public readonly List<ManagedRoutine> FixedRunners = [];
    public readonly List<ManagedRoutine> InputRunners = [];
    public readonly List<ManagedRoutine> DrawRunners = [];
    public readonly List<ManagedRoutine> PostFrameRunners = [];
    public bool destroyed { get; }

    public void Destroy()
    {
        
    }
}

/// <summary>
/// A generig class for updatable and manageble routine behaviors. This class is generally used for engine-managed objects.
/// </summary>
public abstract class ManagedRoutine : IDestroyable, IDisposable, IActivatable
{
    // public int boundWindowID = -1;
    public static readonly Dictionary<int, List<ManagedRoutine>> All = new();
    public static readonly List<ManagedRoutine> NewBorns = [];
    public static readonly List<ManagedRoutine> FrameRunners = [];
    public static readonly List<ManagedRoutine> FixedRunners = [];
    public static readonly List<ManagedRoutine> InputRunners = [];
    public static readonly List<ManagedRoutine> DrawRunners = [];
    public static readonly List<ManagedRoutine> PostFrameRunners = [];
    public static readonly List<ManagedRoutine> deathRow = [];
    public static readonly List<ManagedRoutine> activationRow = [];
    private int layer;
    [JsonIgnore]
    public bool newBorn = true;
    
    /// <summary>
    /// the execution layer of this ManagedRoutine. FableEngine executes lower layers first.
    /// </summary>
    public int Layer
    {
        get => layer;
        set { 
            All[layer].Remove(this);
            if (All[layer].Count == 0) All.Remove(layer);
            layer = value;
            if (!All.ContainsKey(layer)) All.Add(layer, new List<ManagedRoutine>()); 
            All[layer].Add(this); 
        }
    }

    protected ManagedRoutine()
    {
        // Debug.Log("MR creation");
        NewBorns.Add(this);
    }


    /// <summary>
    /// Used by FableEngine to whether include this object and its children in update and render loops. An inactive ManagedRoutine will not be updated nor be drawn, and neither will its children
    /// </summary>
    public virtual bool active
    {
        get;
        set
        {
            if (field == value) return;
            activationRow.Add(this);
            field = value;
        }
    } = true;

    public bool destroyed { get; private set; }

    /// <summary>
    /// Destroy the ManagedRoutine
    /// </summary>
    public virtual void Destroy()
    {
        deathRow.Add(this);
        destroyed = true;
    }

    public virtual void Dispose()
    {
        NewBorns.Remove(this);
        PostFrameRunners.Remove(this);
        InputRunners.Remove(this);
        DrawRunners.Remove(this);
        FixedRunners.Remove(this);
        FrameRunners.Remove(this);
        All[Layer].Remove(this);
        
    }
    
    
    /// <summary>
    /// Called by FableEngine after this Routine is created
    /// </summary>
    public virtual void Wake()
    {
        if (!All.ContainsKey(layer)) All.Add(layer, new List<ManagedRoutine>());
        All[layer].Add(this);
        FrameRunners.Add(this);
        InputRunners.Add(this);
        FixedRunners.Add(this);
        DrawRunners.Add(this);
        PostFrameRunners.Add(this);
    }
    
    /// <summary>
    /// Called every frame by FableEngine
    /// </summary>
    public virtual void Frame()
    {
        
    }
    /// <summary>
    /// Called independently of framerate with a constant rate by FableEngine. Rate is calculated based on Time.timeScale. Called after Frame()
    /// </summary>
    public virtual void Fixed()
    {
        
    }
    /// <summary>
    /// Called while window inputs are being processed by FableEngine. This method is called per window, multiple times per object if it appears on multiple windows at once.
    /// </summary>
    public virtual void Input(Input input)
    {
        
    }
    /// <summary>
    /// Called while the frame is being drawn by FableEngine. This method is called per window, multiple times per object if it renders on multiple windows at once
    /// </summary>
    public virtual void Draw()
    {
        
    }
    /// <summary>
    /// Called after Frame() and Fixed() by FableEngine
    /// </summary>
    public virtual void PostFrame()
    {
        
    }
}


