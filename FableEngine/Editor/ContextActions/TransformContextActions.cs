using FableEngine.Core;
using FableEngine.Debugging;
using FableEngine.Math;
using FableEngine.Rendering;
using FableEngine.Transforms;

public class TransformContextActions : IContextAction
{
    public bool ValidateAndGenerate(Context context, ContextWindow contextWindow, out ITransformProvider output)
    {
        output = null!;
        if (context.target is not ITransformProvider T) return false;
        
        Debug.Log("transform context actions triggered");
        Thing options = new Thing(contextWindow.scene.transform, "Thing Options", typeof(Transform2D));
        options.AddRoutine<ListLayout>().self.insidePadding = new Rectangle(1);
        options.GetRoutine<ListLayout>()?.self.outsidePadding = new Rectangle(0);
        SimpleButton.CreateTemplate(options.transform, "New Child", new Vector2(16), false,
            () =>
            {
                Thing child = new Thing(T.transform, "Thing (2D)");
                contextWindow.Destroy();
            }).GetOrAddRoutine<SimpleButton>().self.outsidePadding = new Rectangle(1);
        SimpleButton.CreateTemplate(options.transform, "Unparent", new Vector2(16), false,
            () =>
            {
                T.transform.parent = Scene.GetSceneOf(T.transform)?.transform;
            }).GetOrAddRoutine<SimpleButton>().self.outsidePadding = new Rectangle(1);
        
        output = options;
        return true;
    }
}