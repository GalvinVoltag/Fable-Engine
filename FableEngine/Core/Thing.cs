using FableEngine.Math;
using FableEngine.Rendering;
using FableEngine.Transforms;

namespace FableEngine.Core;

public interface INameable
{
    string name { get; set; }
}

public interface IDuplicable<out T>
{
    T Duplicate();
}

/// <summary>
/// Thing is a generic ManagedRoutine and IRoutineProvider with a transform. Any Thing can be created, removed,
/// carried, transformed and can manage its own Routines.
/// [similar classes in other popular game engines are GameObjects of Unity, and Nodes of Godot]
/// </summary>
public class Thing : ManagedRoutineProvider, ITransformProvider
{
    // private static readonly List<Thing<TransformType>> _All = [];
    // /// <summary>
    // /// A list of all Things
    // /// </summary>
    // public new static List<Thing<TransformTypeW>> All => _All;

    /// <summary>
    /// Default type of Transform that will be used to instantiate new Things if no specific type is defined in their constructor
    /// </summary>
    public static Type defaultTransformType = typeof(Transform2D);
    
    /// <summary>
    /// transform of this Thing.
    /// </summary>
    public Transform transform;
    
    public Transform? parent => transform.parent;

    public string name { get; set; }

    Transform ITransformProvider.transform => transform;

    public Routine? GetRoutineFromChildren(Type T)
    {
        Routine? routine = null;
        foreach (Transform child in transform.children)
        {
            if (child.attachedObject is not IRoutineProvider RP) continue;
            routine = RP.GetRoutine(T);
            if (routine == null )
            {
                if (RP is not Thing thing) continue;
                routine = thing.GetRoutineFromChildren(T);
                if (routine != null) return routine;
            }
            else return routine;
        }
        return routine;
    }
    public T? GetRoutineFromChildren<T>() where T : Routine
    {
        T? routine = null;
        foreach (Transform child in transform.children)
        {
            if (child.attachedObject is not IRoutineProvider RP) continue;
            routine = RP.GetRoutine<T>();
            if (routine == null )
            {
                if (RP is not Thing thing) continue;
                routine = thing.GetRoutineFromChildren<T>();
                if (routine != null) return routine;
            }
            else return routine;
        }
        return routine;
    }


    /// <summary>
    /// Create a Thing with said position and as a child of said transform
    /// </summary>
    /// <param name="position"> position, defaults to (0, 0) </param>
    /// <param name="parent"> parent transform. Things without a parent will still be updated and managed. </param>
    /// <param name="name"> name of this nameable </param>
    public Thing(Transform? parent = null, string name = "Thing", Type? transformType = null)
    {
        // Debug.Log("Thing creation");
        this.name = name;
        transformType ??= defaultTransformType;
        // transform = new Transform(this, parent);
        transform = Activator.CreateInstance(transformType, [this, parent]) as Transform;
        // transform = transformType.GetConstructor([typeof(ITransformProvider), typeof(Transform)])
        //     ?.Invoke([(object)this, (object?)parent]);
        // _All.Add(this);
    }

    /// <summary>
    /// Destroy this Thing with its Routines.
    /// </summary>
    /// <param name="destroyChildren"> Destroy the children alongside this Thing, otherwise de-parent them </param>
    public override void Destroy() // Todo: destroy routines and children, also pass bool for unparenting children
    {
        if (destroyed) return;
        transform.Destroy();
        transform.parent = null;
        base.Destroy();
    }

    public override void Draw()
    {
        foreach (Routine R in routines)
        {
            if (R is RenderRoutine rr && rr.active)
                Engine.RunEnclosed(rr.Render);
        }
        foreach (Transform child in transform.children)
            if (child.attachedObject is ManagedRoutine MR) MR.Draw();
        foreach (Routine R in routines)
        {
            if (R is RenderRoutine rr && rr.active)
                Engine.RunEnclosed(rr.PostRender);
        }
    }
}
