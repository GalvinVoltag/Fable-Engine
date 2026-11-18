using System.Reflection;

namespace FableEngine.FableEngine.Editor;

public static class FieldOrProperty
{
    public static bool HasGetter(this MemberInfo member)
    {
        return (member is FieldInfo) || (member as PropertyInfo)?.GetMethod != null;
    }
    public static bool HasSetter(this MemberInfo member)
    {
        return (member is FieldInfo) || (member as PropertyInfo)?.SetMethod != null;
    }
    public static object? GetValue(this MemberInfo member, object? target)
    {
        if (member is FieldInfo F) return F.GetValue(target);
        if (member is PropertyInfo P) return P.GetValue(target);
        return null;
    }
    public static T GetValue<T>(this MemberInfo member, object? target)
    {
        if (member is FieldInfo F) return (T)F.GetValue(target);
        if (member is PropertyInfo P) return (T)P.GetValue(target);
        throw new Exception("member is neither field or property!");
    }
    public static void SetValue(this MemberInfo member, object? target, object? value)
    {
        (member as PropertyInfo)?.SetValue(target, value);
        (member as FieldInfo)?.SetValue(target, value);
    }
    
    public static Type? GetFieldOrPropertyType(this MemberInfo member) => 
        ((member is FieldInfo) ? 
            (member as FieldInfo)?.FieldType : 
            (member as PropertyInfo)?.PropertyType) ?? 
        null;
}