using System.Reflection;
using FableEngine.Core;
using FableEngine.Math;
using FableEngine.Rendering;

namespace FableEngine.FableEngine.Editor.MemberEditors;

public class VectorEditor : IMemberEditor
{
    public static int GetPriority(Type type, MemberInfo member)
    {
        return type.IsAssignableTo(typeof(IVector)) ? 20 : 0;
    }

    public static Thing Generate(PropertyBinding<object?> binding, MemberInfo memberInfo, object? target)
    {
        IVector vector = (binding.value as IVector)!;
        
        Thing thing = new Thing();
        ListLayout ll = thing.AddRoutine<ListLayout>();
        ll.direction = ListLayout.Direction.LeftToRight;
        ll.stretch = true;
        ll.self.insidePadding = new Rectangle(0);
        
        SimpleButton.CreateTemplate(thing.transform, binding.name, new Vector2(18), true);

        for (int i = 0; i < vector.Dimensions; i++)
        {
            int index = i;
            Thing btn = DefultMemberEditor.Generate(
                new PropertyBinding<object?>( 
                    ()=> ((IVector)binding.value!)[index],
                    (f) => { vector[index] = (float)f; binding.value = vector; }, 
                    vector[i].ToString() )
                , memberInfo, target);
            btn.GetRoutineFromChildren<SimpleButton>()?.self.minSize = new Vector2(18);
            btn.transform.parent = thing.transform;
            // Thing btn = SimpleButton.CreateTemplate(thing.transform, vector[i].ToString(), new Vector2(18), true);
            // btn.GetOrAddRoutine<SimpleButton>().self.minSize = new Vector2(18);
            // btn.GetRoutineFromChildren<TextRenderer>()?.provider.AddRoutine<WheelMotion>().limitRect = true;
        }
        
        return thing;
    }
}