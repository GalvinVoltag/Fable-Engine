using FableEngine.Debugging;
using FableEngine.Rendering;

namespace FableEngine.Core;

public class ManagedRoutineProvider : ManagedRoutine, IRoutineProvider
{
    private bool rSwitch = false;
    private Routine[] routinesA = [];
    private Routine[] routinesB = [];

    public Routine[] routines
    {
        get => rSwitch ? routinesA : routinesB;
        set
        {
            if (rSwitch) routinesB = value;
            else routinesA = value;
            rSwitch = !rSwitch;
        }
    }
    
    public override void Input(Input input)
    {
        foreach (var routine in routines)
            if (routine.active)
                Engine.RunEnclosed(()=>routine.Input(input), () =>
                {
                    Debug.Log("The error casting ManagedRoutine will be deactivated to avoid further errors!",
                        ConsoleColor.Red);
                    routine.active = false;
                });
    }

    public override void Frame()
    {
        foreach (var routine in routines)
            if (routine.active)
                Engine.RunEnclosed(routine.Frame, () =>
                {
                    Debug.Log("The error casting Routine will be deactivated to avoid further errors!",
                        ConsoleColor.Red);
                    routine.active = false;
                });
    }

    public override void Fixed()
    {
        foreach (var routine in routines)
            if (routine.active)
                Engine.RunEnclosed(routine.Fixed, () =>
                {
                    Debug.Log("The error casting Routine will be deactivated to avoid further errors!",
                        ConsoleColor.Red);
                    routine.active = false;
                });
    }

    public override void PostFrame()
    {
        foreach (var routine in routines)
            if (routine.active)
                Engine.RunEnclosed(routine.PostFrame, () =>
                {
                    Debug.Log("The error casting Routine will be deactivated to avoid further errors!",
                        ConsoleColor.Red);
                    routine.active = false;
                });
    }

    public override void Draw()
    {
        foreach (Routine R in routines)
        {
            if (!R.active || R is not RenderRoutine rr) continue;
            rr.shader.Use();
            Engine.RunEnclosed(rr.Render, () =>
            {
                Debug.Log("The error casting Routine will be deactivated to avoid further errors!",
                    ConsoleColor.Red);
                rr.active = false;
            });
        }
        foreach (Routine R in routines)
        {
            if (!R.active || R is not RenderRoutine rr) continue;
            rr.shader.Use();
            Engine.RunEnclosed(rr.PostRender, () =>
            {
                Debug.Log("The error casting Routine will be deactivated to avoid further errors!",
                    ConsoleColor.Red);
                rr.active = false;
            });
        }
    }

    public Routine AddRoutine(Type type)
    {
        Routine.OwnerCandidate = this;
        Routine? routine = Activator.CreateInstance(type) as Routine;
        if (routine == null) throw new Exception("error while creating the routine from type");
        routines = routines.Concat([routine]).ToArray();
        // _routines.Add(routine);
        routine.Wake();
        routine.newBorn = false;
        return routine;
    }
    public T AddRoutine<T>() where T : Routine, new()
    {
        Routine.OwnerCandidate = this;
        T routine = new T { owner = this };
        routines = routines.Concat([routine]).ToArray();
        // _routines.Add(routine);
        routine.Wake();
        routine.newBorn = false;
        return routine;
    }

    public override void Dispose()
    {
        base.Dispose();
        foreach (Routine R in routines)
            R.Destroy();
        routinesA = [];
        routinesB = [];
    }

    /// <summary>
    /// This method is used to add routines that have a constructor with parameters.
    /// Note that this method uses Activator.CreateInstance in its core, which utilizes reflection.
    /// It is recommended use AddRoutine(Routine routine) instead, as it allows constructor to be invoked directly.
    /// ANY type mismatch for the target constructor will result in an exception, i.e. an int instead of a float.
    /// </summary>
    /// <param name="args">parameters for the target routine's constructor</param>
    /// <typeparam name="T">type of the target routine</typeparam>
    /// <returns></returns>
    public T AddRoutine<T>(params object[] args) where T : Routine
    {
        Routine.OwnerCandidate = this;
        T? routine = (T?)Activator.CreateInstance(typeof(T), args);
        if (routine == null) throw new Exception("Adding Routine into RoutineProvider \"" + GetType().Name + "\" was unsuccessful.");
        routines = routines.Concat([routine]).ToArray();
        // _routines.Add(routine);
        routine.owner = this;
        routine.Wake();
        routine.newBorn = false;
        return routine;
    }
    public void RemoveRoutine(Routine routine) => routines = routines.Where(r => r != routine).ToArray();
    public void RemoveRoutines(Type type) => routines = routines.Where(r => r.GetType() != type).ToArray();
    public void RemoveRoutines<T>() => routines = routines.Where(r => r is not T).ToArray();
    
    public bool HasRoutine(Routine routine) => routines.Contains(routine);
    public Routine? GetRoutine(int index) => index >= 0 && index < routines.Length ? routines[index] : null;
    public Routine? GetRoutine(Type type) => routines.FirstOrDefault(r => r.GetType() == type);

    public T? GetRoutine<T>() where T:Routine => routines.FirstOrDefault(r => r is T) as T;
    public T GetOrAddRoutine<T>() where T:Routine => routines.FirstOrDefault(r => r is T) as T ?? AddRoutine<T>();
    public T[] GetRoutines<T>() where T : Routine => routines.OfType<T>().ToArray();

    public void SwitchRoutines(int indexA, int indexB) =>
        (routines[indexA], routines[indexB]) = (routines[indexB], routines[indexA]);
}