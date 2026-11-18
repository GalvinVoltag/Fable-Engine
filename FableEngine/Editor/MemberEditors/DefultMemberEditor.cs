using System.Reflection;
using FableEngine.Core;
using FableEngine.Math;
using FableEngine.Rendering;

namespace FableEngine.FableEngine.Editor.MemberEditors;

public class DefultMemberEditor : IMemberEditor
{
    public static int GetPriority(Type type, MemberInfo member)
    {
        return 1;
    }
    
    public static Thing Generate(PropertyBinding<object?> binding, MemberInfo memberInfo, object? target)
    {
        string name = binding.name;
        if (memberInfo.DeclaringType != target?.GetType()) name = "+" + name;
        
        SimpleButton b = SimpleButton.CreateTemplate(null, name, new Vector2(16), true).GetOrAddRoutine<SimpleButton>();
        TextRenderer tr = b.thing.GetRoutineFromChildren<TextRenderer>()!;
        b.onClick = () =>
        {
            tr.text = binding.value?.ToString() ?? "???";
            InspectorPortal.targetEditingText = tr;
            InspectorPortal.binding = new PropertyBinding<object?>(
                binding.getter,
                binding.setter,
                memberInfo.Name
            );
            // Debug.Log(memberInfo.HasSetter() ?? "no");
        };
        if (!memberInfo.HasSetter()) b.neutralColor = Color.gray;

        return b.thing;
    }
}