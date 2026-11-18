using System.Reflection;
using FableEngine.Core;
using FableEngine.Debugging;
using FableEngine.Math;
using FableEngine.Rendering;
using FableEngine.Transforms;
using TypeExtensions = FableEngine.FableEngine.Editor.TypeExtensions;

public class RoutineProviderContextActions : IContextAction
{
    public bool ValidateAndGenerate(Context context, ContextWindow contextWindow, out ITransformProvider output)
    {
        switch (context.target)
        {
            case IRoutineProvider R:
            {
                SimpleButton button = SimpleButton.CreateTemplate(contextWindow.scene.transform, "Add Routine",
                    new Vector2(16), false,
                    () => { Debug.Log("Unimplemented", DebugLogLevel.Error); }
                ).GetOrAddRoutine<SimpleButton>();
                button.provider.GetOrAddRoutine<SimpleButton>().self.outsidePadding = new Rectangle(1);
                ContextMenuProvider cmp = button.provider.AddRoutine<ContextMenuProvider>();
                cmp.context = new Context(this, [context.target]);
                cmp.activateOnHover = true;
                output = (button.provider as ITransformProvider)!;
                return true;
            }
            case RoutineProviderContextActions RPCA:
            {
                Thing options = new Thing(contextWindow.scene.transform, "Thing Options", typeof(Transform2D));
                options.AddRoutine<ListLayout>().self.insidePadding = new Rectangle(0);
                options.GetRoutine<ListLayout>()?.self.outsidePadding = new Rectangle(0);

                switch (context.related)
                {
                    case [IRoutineProvider RP]:
                    {
                        foreach (Type T in Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsAssignableTo(typeof(Routine)) && type != typeof(Routine) ))
                        {
                            Thing thing = SimpleButton.CreateTemplate(options.transform, TypeExtensions.GetPrettyName(T),
                                new Vector2(18));
                            thing.GetRoutine<SimpleButton>()?.self.outsidePadding = new Rectangle(1);
                            if (!T.IsGenericType) {
                                thing.GetOrAddRoutine<SimpleButton>().onClick += () => { RP.AddRoutine(T); };
                                continue;
                            }

                            ContextMenuProvider CMP2 = thing.AddRoutine<ContextMenuProvider>();
                            CMP2.context = new Context(RPCA, [T, RP]);
                        }
                        break;
                    }
                    case [Type Ty, IRoutineProvider RP2]:
                    {
                        foreach (Type TT in TypeExtensions.GetPossibleGenericTypes(Ty))
                        {
                            Thing thingy = SimpleButton.CreateTemplate(options.transform, TypeExtensions.GetPrettyName(TT), new Vector2(18));
                            thingy.GetRoutine<SimpleButton>()?.self.outsidePadding = new Rectangle(1);
                            thingy.GetOrAddRoutine<SimpleButton>().onClick += () => { RP2.AddRoutine(TT); };
                        }
                        break;
                    }
                    default:
                        output = null!;
                        return false;
                }
                output = options;
                return true;
            }
            default:
                output = null!;
                return false;
        }
    }
}