using FableEngine.Debugging;

namespace FableEngine.FableEngine.Editor;

public static class TypeExtensions
{
    static Type[] AllConcreteTypes => // get all types in all loaded assemblies
        field ??= AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .Where(t => !t.IsAbstract && !t.ContainsGenericParameters)
            .Distinct()
            .ToArray();
    
    public static Type[] GetPossibleGenericTypes(Type T)
    {
        if (!T.IsGenericTypeDefinition) return [T];
        
        List<Type> final = new List<Type>();
        Type GP = T.GetGenericArguments()[0];
            
        if (T.GetGenericArguments().Length != 1 && Debug.Log("skipped " + T + " because it has too many generic types", DebugLogLevel.Error)) return [];

        Type[] constraints = GP.GetGenericParameterConstraints();
            
        if (constraints.Length == 0 && Debug.Log("skipped " + T + " because it is generic with no constraints", DebugLogLevel.Error)) return [];

        foreach ( Type GT in AllConcreteTypes )
        {
            // if (constraints.Any(c => !c.IsAssignableFrom(GT))) continue;

            Type GPT;
            try {
                GPT = T.MakeGenericType(GT);
            } catch (Exception e) { continue; }
                
            final.Add(GPT);
        }
        return final.ToArray();

    }
    
    public static string GetPrettyName(Type t)
    {
        if (!t.IsGenericType) return t.Name;

        string name = t.Name[..t.Name.IndexOf('`')];          // "Slider"
        IEnumerable<string> args = t.GetGenericArguments().Select(GetPrettyName); // recurse for nested generics
        return $"{name}<{string.Join(", ", args)}>";
    }
}