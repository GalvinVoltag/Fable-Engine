using System.Reflection;
using FableEngine.Core;
using FableEngine.Debugging;
using FableEngine.FableEngine.Editor;
using FableEngine.Math;
using FableEngine.Rendering;
using FableEngine.Transforms;
using Silk.NET.Input;
using TypeExtensions = FableEngine.FableEngine.Editor.TypeExtensions;

namespace FableEngine;

public class InspectorPortal : BasePortal
{
    private static readonly Type[] AllPropertyEditors =
    [
        .. Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => type.IsAssignableTo(typeof(IMemberEditor)) && type != typeof(IMemberEditor) )
    ];

    public object? targetObject = null;
    // private ContextMenu addRoutineMenu = new ContextMenu([]);
    private Thing ScrollArea;
    

    public InspectorPortal(Transform? parent = null, string name = "Inspector Portal")
    {
        backgroundColor = Color.green;
        ScrollArea = new Thing(transform, "Scroller");
        if (ScrollArea.GetRoutine<ContextMenuProvider>() != null) return;
        
        ListLayout Ll = ScrollArea.AddRoutine<ListLayout>();
        Ll.self.rect.size = new Vector2(100, 100);
        Ll.stretch = false;
        ScrollArea.AddRoutine<WheelMotion>();

        ScrollArea.AddRoutine<WheelMotion>();
        ScrollArea.AddRoutine<FitRect>().fitY = false;
        
        ContextMenuProvider CMP = ScrollArea.AddRoutine<ContextMenuProvider>();
        CMP.context = new Context(this, [], [typeof(TransformContextActions), typeof(RoutineProviderContextActions)]);
    }
    
    public static TextRenderer? targetEditingText = null;
    public static PropertyBinding<object?>? binding = null;
    public override bool Input(Input input)
    {
        if (targetEditingText == null || binding == null) return base.Input(input);
        
        targetEditingText.color = Color.blue;
        targetEditingText.text += input.GetKeyboardInputStream();
        if (input.GetKey(Key.Backspace) && targetEditingText.text.Length > 0) targetEditingText.text = targetEditingText.text[..^1];
        
        if (input.GetKey(Key.Enter)) {
            targetEditingText.color = Color.black;
            
            Type type = binding?.value?.GetType() ?? typeof(int);
            
            MethodInfo? parseMethod = type.GetMethod(nameof(float.Parse), BindingFlags.Public | BindingFlags.Static, [typeof(string), typeof(IFormatProvider)]);
            if (parseMethod != null)
            {
                binding?.value = parseMethod.Invoke(null, [targetEditingText.text, null]);
            }
            else
            {
                if (type == typeof(string))
                {
                    binding?.value = targetEditingText.text;
                }
                else if (type.IsEnum)
                {
                    if (int.TryParse(targetEditingText.text, out var value))
                    {
                        binding?.value = value;
                    }
                }
                else Debug.Log(type.Name + " is not parsable", DebugLogLevel.Error);
            }
            
            targetEditingText.text = binding?.value?.ToString() ?? "???";
            
            targetEditingText = null;
            binding = null;
        }

        return base.Input(input);
    }

    private int oldRoutineCount = 0;
    public override void Frame()
    {
        base.Frame();
        switch (targetObject)
        {
            case null:
                return;
            case IRoutineProvider RP when RP.routines.Length != oldRoutineCount:
                GenerateInspector(targetObject);
                oldRoutineCount = RP.routines.Length;
                break;
        }
    }

    public void GenerateInspector(object? target)
    {
        // if (child.attachedObject is IDestroyable destroyable) destroyable.Destroy();
        if (target == null) return;
        foreach (Transform dest in ScrollArea.transform.children.ToArray())
            dest.Destroy();
        
        targetObject = target;

        if (target is ITransformProvider TP)
        {
            SimpleButton.CreateTemplate(ScrollArea.transform, TP.transform.GetType().Name, new Vector2(20));
            ListLayout transformLayout = new Thing(ScrollArea.transform).GetOrAddRoutine<ListLayout>();
            GeneratePropertyList(transformLayout, TP.transform);
        }

        if (target is IRoutineProvider RP)
        {
            foreach (Routine R in RP.routines)
            {
                SimpleButton.CreateTemplate(ScrollArea.transform, TypeExtensions.GetPrettyName(R.GetType()), new Vector2(20), true);
                ListLayout layout = new Thing(ScrollArea.transform).GetOrAddRoutine<ListLayout>();
                GeneratePropertyList(layout, R);
            }
        }

        ScrollArea.GetOrAddRoutine<ListLayout>().RecalculateLayout();
    }
    public void GeneratePropertyList(ListLayout layout, object R)
    {
        foreach (MemberInfo member in R.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(m => m is  PropertyInfo or FieldInfo))
        {
            object[] attributes = member.GetCustomAttributes(false);
            Type type = member.GetValue(R)?.GetType() ?? typeof(int);

            if (member.DeclaringType != R.GetType() && !attributes.Any(a => a is ShowDeclared)) continue;
        
            Type? editorType = null;
            int lastPriority = -1;
            foreach (var T in AllPropertyEditors)
            {
                int priority = (int)T.GetMethod(nameof(IMemberEditor.GetPriority))!.Invoke(null, [type, member])!;
                if (priority <= lastPriority) continue;
            
                editorType = T;
                lastPriority = priority;
            }

            if (editorType == null) continue;
        
            Thing t = (Thing)editorType.GetMethod(nameof(IMemberEditor.Generate))!
                .Invoke(null, 
                [new PropertyBinding<object?>( 
                        () => member.GetValue(R), 
                        (v) => member.SetValue( R, v), 
                        member.Name)
                    , member, R])!;
            t.transform.parent = layout.transform;
        
        }
    }
    
}