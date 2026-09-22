namespace FableEngine.Core;

/// <summary>
/// This interface will provide, manage, and keep track of its own Routines.
/// </summary>
public interface IRoutineProvider
{
    /// <summary>
    /// A list of all routines attached to this provider
    /// </summary>
    public Routine[] routines { get; }
    
    /// <summary>
    /// Add a Routine to this provider.
    /// </summary>
    /// <param name="type"> routine type to be added </param>
    Routine AddRoutine(Type type);
    /// <summary>
    /// Add a new routine of type T to this provider
    /// </summary>
    /// <typeparam name="T"> type of the Routine </typeparam>
    /// <returns> the new Routine that was just added </returns>
    T AddRoutine<T>() where T : Routine, new();
    /// <summary>
    /// Add a new routine to this provider and pass the arguments to its constructor
    /// </summary>
    /// <param name="args"> arguments for the Routine's constructor </param>
    /// <typeparam name="T"> type of the Routine </typeparam>
    /// <returns> the new Routine that was just added </returns>
    T AddRoutine<T>(params object[] args) where T : Routine;
    /// <summary>
    /// Remove said routine from this provider. Will do nothing if this Routine is not
    /// in the list of this provider's Routines
    /// </summary>
    /// <param name="routine"> the Routine to be removed </param>
    void RemoveRoutine(Routine routine);
    /// <summary>
    /// If this provider houses said Routine
    /// </summary>
    /// <param name="routine"> routine to check for </param>
    /// <returns> if the routine is housed by this provider </returns>
    bool HasRoutine(Routine routine);
    /// <summary>
    /// Get the routine at said index of this provider.
    /// </summary>
    /// <param name="index"> index of target Routine </param>
    /// <returns> the routine at index </returns>
    Routine? GetRoutine(int index);
    Routine? GetRoutine(Type type);
    /// <summary>
    /// Gets the first Routine this provider houses that matches said type
    /// </summary>
    /// <typeparam name="T"> type of target Routine </typeparam>
    /// <returns> a Routine of type T, null if not found </returns>
    T? GetRoutine<T>() where T : Routine;
    /// <summary>
    /// If a provider has any Routine of type T, this will get it for you, if it doesn't it will be added automatically
    /// </summary>
    /// <typeparam name="T"> type of target Routine </typeparam>
    /// <returns> a Routine of type T housed by this provider </returns>
    T GetOrAddRoutine<T>() where T : Routine;
    /// <summary>
    /// Get an array of all the Routines of a certain type this provider houses. Beware that this method searches for said methods
    /// and reallocates a new array in every use. It is best practice to cache the output.
    /// </summary>
    /// <typeparam name="T"> type of target Routines </typeparam>
    /// <returns> an array of all the Routines that matches the target type </returns>
    T[] GetRoutines<T>() where T : Routine;
}
