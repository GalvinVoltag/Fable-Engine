using System.Reflection;
using FableEngine.Core;
using FableEngine.Math;

namespace FableEngine.FableEngine.Editor.MemberEditors;

public class BooleanEditor : IMemberEditor
{
    public static int GetPriority(Type type, MemberInfo member)
    {
        return type == typeof(bool) ? 1 : 0;
    }

    public static Thing Generate(PropertyBinding<object?> binding, MemberInfo memberInfo, object? target)
    {
        Thing thing = SimpleButton.CreateTemplate(null, binding.name, new Vector2(18));
        SimpleButton button = thing.GetRoutine<SimpleButton>()!;
        button.neutralColor = (bool)binding.value!?Color.green:Color.red;
        button.hoveredColor = button.neutralColor + new Color(0.2f, 0.2f, 0.2f);
        button.clickedColor = button.neutralColor - new Color(0.2f, 0.2f, 0.2f, 0);
        button.onClick = () =>
        {
            binding.value = !(bool)binding.value!;
            button.neutralColor = (bool)binding.value!?Color.green:Color.red;
            button.hoveredColor = button.neutralColor + new Color(0.2f, 0.2f, 0.2f);
            button.clickedColor = button.neutralColor - new Color(0.2f, 0.2f, 0.2f, 0);
        };
        
        return thing;
    }
}