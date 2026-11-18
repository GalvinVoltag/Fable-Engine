using FableEngine.Core;
using FableEngine.Math;
using FableEngine.Transforms;
using Silk.NET.Input;

namespace FableEngine;

public class SceneHierarchyPortal : BasePortal
{
    public Transform? targetTree;
    public InspectorPortal? targetPortal = null;
    
    private ListLayout _listLayout => field ??= GetOrAddRoutine<ListLayout>();
    private List<Thing> items = [];
    private List<Transform> itemTargets = [];
    private List<int> oldDepths = [];
    
    public void ClearList()
    {
        foreach (Thing sb in items) {
            sb.Destroy();
        }
        items.Clear();
        itemTargets.Clear();
        oldDepths.Clear();
    }
    
    public void GenerateHierarchy(Transform? target = null, int nesting = 0)
    {
        if (target == null)
        {
            target = targetTree;
            if (target == null) return;
            ClearList();
        }

        foreach (Transform child in target.children)
        {
            // Thing targetBtn = SimpleButton.CreateTemplate(transform, child.attachedObject.name + " (" +
            //                                             TypeExtensions.GetPrettyName(child.GetType()) + ")", new Vector2(18));
            Thing targetBtn = SimpleButton.CreateTemplate(transform, child.attachedObject.name + " (" +
                                                                     (child.depth - targetTree?.depth) + ")", new Vector2(18));
            targetBtn.GetRoutine<SimpleButton>()?.onClick = () =>
            {
                if (targetPortal == null || global::FableEngine.Input.current.GetKey(Key.ShiftLeft))
                {
                    InspectorPortal inspector = new InspectorPortal();
                    inspector.GenerateInspector(child.attachedObject);
                }else {
                    targetPortal?.GenerateInspector(child.attachedObject);
                }
            };
            targetBtn.GetRoutine<Rect>()?.outsidePadding.Left = nesting * 20;
            targetBtn.AddRoutine<ContextMenuProvider>().context = new Context(child.attachedObject);
            items.Add(targetBtn);
            itemTargets.Add(child);
            oldDepths.Add(child.depth - targetTree?.depth??0);
            if (child.children.Count > 0) GenerateHierarchy(child, nesting+1);
        }
        _listLayout.RecalculateLayout();
    }
    
    public override void Frame()
    {
        base.Frame();
        if (targetTree == null) return;
        for (int i = 0; i < items.Count; i++)
        {
            if (oldDepths[i] == (itemTargets[i].depth - targetTree.depth)) continue;
            GenerateHierarchy();
            break;
        }
        if (items.Count != targetTree.totalChildren)
            GenerateHierarchy();
    }
}