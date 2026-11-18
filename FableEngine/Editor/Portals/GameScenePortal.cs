using FableEngine.Core;
using FableEngine.Math;
using FableEngine.Transforms;

namespace FableEngine;

public class GameScenePortal : BasePortal
{
    public Scene gameScene;

    private ContextMenuProvider CMP;

    public GameScenePortal(Scene gameScene, Transform? parent = null)
    {
        transform.parent = parent;
        this.gameScene = gameScene;
        this.gameScene.transform.parent = transform;
        this.backgroundColor = Color.yellow;
        CMP = GetOrAddRoutine<ContextMenuProvider>();
        CMP.context = new Context(this, [], [typeof(TransformContextActions), typeof(RoutineProviderContextActions)]);

        // rect.onShapeChanged = UpdateShape;
    }

    public override void Frame()
    {
        base.Frame();
        gameScene?.rectangle = rectangle;
    }

    // private void UpdateShape(Rectangle newShape)
    // {
    //     gameScene?.rectangle = newShape;
    // }

    public override void Dispose()
    {
        // rect.onShapeChanged -= UpdateShape;
        base.Dispose();
    }
}