using System.Reflection;
using FableEngine.Core;
using FableEngine.Math;
using FableEngine.Transforms;

namespace FableEngine.FableEngine.Editor.MemberEditors;

public class TransformEditor : IMemberEditor
{
    public static int GetPriority(Type type, MemberInfo member)
    {
        return type.IsAssignableTo(typeof(Transform)) ? 10 : 0;
    }

    public static Thing Generate(PropertyBinding<object?> binding, MemberInfo memberInfo, object? target)
    {
        return SimpleButton.CreateTemplate(null, binding.name + " (Nu Uh)", new Vector2(18));
    }
}