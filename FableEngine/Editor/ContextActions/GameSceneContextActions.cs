using FableEngine;
using FableEngine.Core;
using FableEngine.Math;
using FableEngine.Rendering;
using FableEngine.Transforms;

public class GameSceneContextActions : IContextAction
{
    public bool ValidateAndGenerate(Context context, ContextWindow contextWindow, out ITransformProvider output)
    {
        output = null!;
        if (context.target is not GameScenePortal S) return false;
        
        Thing options = new Thing(contextWindow.scene.transform, "Game Scene Options", typeof(Transform2D));
        SimpleButton.CreateTemplate(options.transform, "New 2D Thing", new Vector2(16), false,
            () =>
            {
                GameWindow? win = GameWindow.GetWindowOf(S.transform);
                if (win == null) return;
                Thing t = new Thing(S.gameScene.transform);
                t.AddRoutine<ShapeRenderer>().shape = new Rectangle(-20, 40);
                t.transform.Cast<Transform2D>().position = S.gameScene.transform.worldToLocal * GameWindow.GetWindowOf(S.transform)?.input.GetMousePos()??Vector2.Zero;
                contextWindow.Destroy();
            }).GetOrAddRoutine<SimpleButton>().self.outsidePadding = new Rectangle(1);
        SimpleButton.CreateTemplate(options.transform, "New 3D Thing", new Vector2(16), false,
            () =>
            {
                GameWindow? win = GameWindow.GetWindowOf(S.transform);
                if (win == null) return;
                Thing t = new Thing(S.gameScene.transform);
                t.AddRoutine<ShapeRenderer>().shape = new Rectangle(-20, 40);
                t.transform.Cast<Transform2D>().position = S.gameScene.transform.worldToLocal * GameWindow.GetWindowOf(S.transform)?.input.GetMousePos()??Vector2.Zero;
                contextWindow.Destroy();
            }).GetOrAddRoutine<SimpleButton>().self.outsidePadding = new Rectangle(0);
        options.AddRoutine<ListLayout>().self.insidePadding = new Rectangle(1);
        options.GetRoutine<ListLayout>()?.self.outsidePadding = new Rectangle(0);
        output = options;
        return true;
    }
}