using System.Reflection;
using FableEngine.Core;

namespace FableEngine.FableEngine.Editor.MemberEditors;

public class SliderEditor : IMemberEditor
{
    public static int GetPriority(Type type, MemberInfo member)
    {
        var attributes = member.GetCustomAttributes(false);
        if (attributes.Any(o => o is Slidable))
            return 5;
        return 0;
    }
    
    public static Thing Generate(PropertyBinding<object?> binding, MemberInfo memberInfo, object? target)
    {
        Slidable? s = memberInfo.GetCustomAttributes(false).First(o => o is Slidable) as Slidable;
        if (s  == null) throw new NullReferenceException();
        
        float min = s.min;
        float max = s.max;
        // SimpleButton b = SimpleButton.CreateTemplate(layout.transform, name, new Vector2(16))
        //     .GetOrAddRoutine<SimpleButton>();

        Type numberType = memberInfo.GetFieldOrPropertyType() ?? typeof(float);
        Type sliderType = typeof(Slider<>).MakeGenericType(numberType);

        MethodInfo? CreateTemplate = sliderType.GetMethod(nameof(Slider<>.CreateTemplate),
            BindingFlags.Static | BindingFlags.Public);
        if (CreateTemplate == null) throw new NullReferenceException();

        object? thing = CreateTemplate.Invoke(null, [
            null,
            Convert.ChangeType(min, numberType),
            Convert.ChangeType(max, numberType)
        ]);
        object? slider = (thing as Thing)?.GetRoutineFromChildren(sliderType);

        PropertyBinding<Single> pbs = new PropertyBinding<Single>();
        Type bindingType = typeof(PropertyBinding<>).MakeGenericType(numberType);
        
        sliderType.GetField(nameof(Slider<>.target))?.SetValue(slider,
            Activator.CreateInstance(bindingType,
                binding.getter,
                binding.setter,
                binding.name));


        if (thing != null) return (Thing)thing;
        throw new NullReferenceException("slider template returned null");
    }
}