using FableEngine.Transforms;

namespace FableEngine.Core;


public interface IActivatable
{
    bool active { get; set; }
}

/// <summary>
/// A generic class for maintainable, attachable, and manually managable routine behaviors
/// </summary>
public abstract class Routine : IDestroyable, IActivatable
{
    public static object? OwnerCandidate;

    [ShowDeclared]
    public virtual bool active { get;
        set;
    } = true;
    
    /// <summary>
    /// The object this Routine is attached to. Beware that this getter may be null, and is not guaranteed to be a IRoutineProvider.
    /// Usage of this is intended for more low level control.
    /// </summary>
    public object owner;
    
    /// <summary>
    /// The RoutineProvider this Routine is attached to. It can be used to get, add and manage Routines. Beware that this
    /// must not be used if owner is set manually to a non-provider object.
    /// </summary>
    /// <exception cref="NullReferenceException"> A null exception will be thrown if no provider exists for this Routine </exception>
    public IRoutineProvider provider => 
        owner as IRoutineProvider ?? throw new NullReferenceException("Trying to access a Routine's provider, but it's null or not a RoutineProvider.");

    public Thing thing => 
        owner as Thing ?? throw new NullReferenceException("Trying to access a Routine's Thing, but it's not a Thing.");

    public Transform transform
    {
        get
        {
            if (owner is not ITransformProvider transformProvider)
                throw new NullReferenceException(
                    "Trying to access transform of owner" + (owner.GetType()) + ", but owner is not a transform provider ITransformProvider.");
            return transformProvider.transform;
        }
    }

    public T Transform<T>() where T : Transform => (T)transform;

    /// <summary>
    /// If this Routine's Wake has been called since its creation. If this is set to true, Wake() will be called next frame before anything.
    /// </summary>
    public bool newBorn = true;

    /// <summary>
    /// In order for this to work as intended, set Routine.ownerCandidate as the intended owner BEFORE calling the constructor
    /// </summary>
    protected Routine()
    {
        owner = OwnerCandidate ?? throw new Exception("tried to create a Routine with no owner");

        declaresWake = GetType().GetMethod(nameof(Wake))?.GetBaseDefinition().DeclaringType != GetType();
        declaresFrame = GetType().GetMethod(nameof(Frame))?.GetBaseDefinition().DeclaringType != GetType();
        declaresInput = GetType().GetMethod(nameof(Input))?.GetBaseDefinition().DeclaringType != GetType();
        declaresFixed = GetType().GetMethod(nameof(Fixed))?.GetBaseDefinition().DeclaringType != GetType();
        declaresPostFrame = GetType().GetMethod(nameof(PostFrame))?.GetBaseDefinition().DeclaringType != GetType();
    }
    
    public bool declaresWake { get; private set; }
    public bool declaresFrame { get; private set; }
    public bool declaresInput { get; private set; }
    public bool declaresFixed { get; private set; }
    public bool declaresPostFrame { get; private set; }
    
    /// <summary>
    /// Called when this Routine is created
    /// </summary>
    public virtual void Wake()
    {
        
    }
    
    /// <summary>
    /// Called before the frame is drawn
    /// </summary>
    public virtual void Frame()
    {
        
    }
    /// <summary>
    /// Called while a windows input is being processed. May be called multiple times if the object appears on multiple windows at once
    /// </summary>
    /// <param name="input"></param>
    public virtual void Input(Input input)
    {
        
    }
    /// <summary>
    /// Called independently of framerate with a constant rate. Rate is calculated based on Time.timeScale
    /// </summary>
    public virtual void Fixed()
    {
        
    }
    /// <summary>
    /// Called after the frame is drawn
    /// </summary>
    public virtual void PostFrame()
    {
        
    }

    public bool destroyed { get; private set; }

    /// <summary>
    /// Called before this routine and its owner are destroyed
    /// </summary>
    public virtual void Destroy()
    {
        destroyed = true;
    }
}
