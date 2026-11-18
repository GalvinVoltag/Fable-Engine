using FableEngine.Core;
using FableEngine.Debugging;
using FableEngine.Math;
using FableEngine.Rendering;
using FableEngine.Transforms;

public class PortalContextActions : IContextAction
{
    public bool ValidateAndGenerate(Context context, ContextWindow contextWindow, out ITransformProvider output)
    {
        output = null!;
        if (context.target is not BasePortal P) return false;
        
        Thing options = new Thing(contextWindow.scene.transform, "Thing Options", typeof(Transform2D));
        options.AddRoutine<ListLayout>().self.insidePadding = new Rectangle(1);
        options.GetRoutine<ListLayout>()?.self.outsidePadding = new Rectangle(0);
        SimpleButton.CreateTemplate(options.transform, "Detach", new Vector2(16), false,
            () =>
            {
                P.transform.parent = null;
                contextWindow.Destroy();
            }).GetOrAddRoutine<SimpleButton>().self.outsidePadding = new Rectangle(1);
        
        output = options;
        return true;
    }
}