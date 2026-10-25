using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Security.AccessControl;
using FableEngine.Math;
using FableEngine.Rendering;
using FableEngine;
using FableEngine.Core;
using FableEngine.Debugging;
using FableEngine.Transforms;
using Silk.NET.Input;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Debug = FableEngine.Debugging.Debug;
using Matrix4x4 = FableEngine.Math.Matrix4x4;
using MouseButton = Silk.NET.Input.MouseButton;
using Rectangle = FableEngine.Rendering.Rectangle;
using Vector2 = FableEngine.Math.Vector2;
using T2D = FableEngine.Transforms.Transform2D;
using Vector = System.Numerics.Vector;

// using FableEngine = FableEngine.FableEngine;

public class Theme
{
    public static Theme defaultFallback;
}

public class UIArea : IDestroyable, IDisposable
{
    public static List<UIArea> allAreas = new List<UIArea>();
    private static List<UIArea> dominantAreas = new List<UIArea>();
    private static bool initialized = false;
    private static StandardCursor cs = StandardCursor.Arrow;
    
    private static void UpdateAll() // todo: performance
    {
        cs = StandardCursor.Arrow;
        GameWindow.allWindowsList[0].input.ChangeCursorType(cs);
        foreach (UIArea area in dominantAreas)
        {
            cs = area.cursorShape;
            // area.hovering = true;
            if (area.areaCombinations != null)
                foreach (var pair in area.areaCombinations)
                    if (pair.Key.hovering)
                        cs = pair.Value;
            if (area.blocking) break;
        }
        if (dominantAreas.Count > 0) dominantAreas[0].window?.input?.ChangeCursorType(cs);
        else GameWindow.allWindowsList[0].input.ChangeCursorType(cs);
        dominantAreas.Clear();
    }
    
    public StandardCursor cursorShape = StandardCursor.Arrow;

    public Transform transform
    {
        get;
        set
        {
            field = value;
            window = GameWindow.GetWindowOf(value);
        }
    }
    private int lastParentVersion = -1;
    private Func<bool>[] maskStack = [];
    private GameWindow? window = null;
    public Dictionary<UIArea, StandardCursor>? areaCombinations;
    public Shape shape;
    public bool blocking;
    public UIArea(Shape shape, Transform transform, bool blocking = true)
    {
        if (!initialized) {
            Engine.RegisterGlobalMethod(GlobalMethodPosition.AfterWake, UpdateAll);
            initialized = true; }
        this.blocking = blocking;
        this.shape = shape;
        this.transform = transform;
        window = GameWindow.GetWindowOf(transform);
        allAreas.Add(this);
        this.hovering = false;
    }
    
    public void UpdateState()
    {
        hovering = false;
        dominantAreas.Remove(this);
        if (transform.destroyed) {
            Destroy();
            return; }
        if (lastParentVersion != transform.parentVersion || window == null) {
            window = GameWindow.GetWindowOf(transform);
            lastParentVersion = transform.parentVersion;
        }
        if (window == null || window.destroyed) 
            return;
        if (transform.matrix == Matrix4x4.identity)
            return;
        
        Vector2 relativeMouse = transform.worldToLocal * window.input.GetMousePos();
        if (!shape.PointIntersection(relativeMouse)) 
            return;
        
        hovering = true;
        if (dominantAreas.Count == 0)
        {
            dominantAreas.Add(this);
            return;
        }
        for (int i = 0; i < dominantAreas.Count; i++)
        {
            if (transform.depth >= dominantAreas[i].transform.depth || i == dominantAreas.Count - 1)
            {
                dominantAreas.Insert(i, this);
                break;
            }
        }
    }

    public bool destroyed
    {
        get;
        private set;
    }

    public void Destroy()
    {
        if (destroyed) return;
        destroyed = true;
        Engine.DisposeLater(this);
    }

    public void Dispose()
    {
        allAreas.Remove(this);
        dominantAreas.Remove(this);
    }

    public bool hovering { get; private set; } = false;

    public bool clicked
    {
        get
        {
            if (window == null) return false;
            return hovering && window.input.GetMouseButton(MouseButton.Left, 0);
        }
    }
    public bool rightclicked
    {
        get
        {
            if (window == null) return false;
            return  hovering && window.input.GetMouseButton(MouseButton.Right, 0);
        }
    }
}
public class Rect : RenderRoutine
{
    public Rect()
    {
        ShapeRenderer? sh = provider.GetRoutine<ShapeRenderer>();
        if (sh?.shape is Rectangle R) rect = R;
    }
    
    
    /// <summary>
    /// use this to mutate the shape of this Rect without triggering onShapeChange (in order to trigger onShapeChange, use rectShape setter)
    /// </summary>
    public Rectangle rect = new(Vector2.Zero, Vector2.One*10, true);

    /// <summary>
    /// use this to trigger OnShapeChange if the shape changes
    /// </summary>
    public Rectangle rectShape
    {
        get => rect;
        set { if (onShapeChanged != null && rect != value) onShapeChanged.Invoke(value); rect = value; }
    }

    public float width
    {
        get => rect.width;
        set => rect.width = value;
    }
    public float height
    {
        get => rect.height;
        set => rect.height = value;
    }
    
    /// <summary>
    /// Move the Left side of this Rect while respecting its minimum and maximum size
    /// </summary>
    public float Left
    {
        get => rect.Left;
        set {
            if (value != rect.Left) onShapeChanged?.Invoke(rect with {Left = value});
            rect.Left = Math.Clamp(value, rect.Right - maxSize.x, rect.Right - minSize.x);
        }
    }
    /// <summary>
    /// Move the Right side of this Rect while respecting its minimum and maximum size
    /// </summary>
    public float Right
    {
        get => rect.Right;
        set
        {
            if (value != rect.Right) onShapeChanged?.Invoke(rect with {Right = value});
            rect.Right = Math.Clamp(value, rect.Left + minSize.x, rect.Left + maxSize.x);
        }
    }
    /// <summary>
    /// Move the Top side of this Rect while respecting its minimum and maximum size
    /// </summary>
    public float Top
    {
        get => rect.Top;
        set
        {
            if (value != rect.Top) onShapeChanged?.Invoke(rect with {Top = value});
            rect.Top = Math.Clamp(value, rect.Bottom + minSize.y, rect.Bottom + maxSize.y);
        }
    }
    /// <summary>
    /// Move the Bottom side of this Rect while respecting its minimum and maximum size
    /// </summary>
    public float Bottom
    {
        get => rect.Bottom;
        set
        {
            if (value != rect.Bottom) onShapeChanged?.Invoke(rect with {Bottom = value});
            rect.Bottom = Math.Clamp(value, rect.Top - maxSize.y, rect.Top - minSize.y);
        }
    }

    public void RescaleCentered(Vector2 size)
    {
        Vector2 clamped = new Vector2(Math.Clamp(Math.Abs(size.x), minSize.x, maxSize.x), Math.Clamp(Math.Abs(size.y), minSize.y, maxSize.y));
        if (clamped != rect.size) onShapeChanged?.Invoke(rect with {size = clamped});
        rect.size = clamped;
        rect.position = -clamped / 2;
    }
    
    
    public Vector2 center {
        get => Transform<T2D>().position + ((rect.position+rect.size/2) * Transform<T2D>().scale).Rotated(Transform<T2D>().rotation);
        set => Transform<T2D>().position = value - ((rect.position+rect.size/2) * Transform<T2D>().scale).Rotated(-Transform<T2D>().rotation);
    }

    public Vector2 origin
    {
        get => (rect.position + rect.size / 2);
        set => rect.position =  value - rect.size / 2;
    }
    public Vector2 minSize = new Vector2(10, 10);
    public Vector2 maxSize = new Vector2(10000, 10000);
    public Rectangle outsidePadding = new Rectangle(5);
    public Rectangle insidePadding = new Rectangle(5);

    public Action<Rectangle>? onShapeChanged;

    public override void Draw()
    {
        ShapeRenderer.DrawRectangle(GameWindow.defaultShader, rect, new Color(1, 1, 1, 0.2f), transform.matrix);
        ShapeRenderer.DrawRectangle(GameWindow.defaultShader, new Rectangle(Vector2.Zero, Vector2.One, false), Color.red, transform.matrix);
    }
}

public class RectResizer : Routine
{
    public Rect self { get; private set; }
    public Rectangle resizeRect = new Rectangle(5);
    public bool allowLeftResizing = true;
    public bool allowRightResizing = true;
    public bool allowTopResizing = true;
    public bool allowBottomResizing = true;

    private bool hoverLeft => left.hovering;
    private bool hoverRight => right.hovering;
    private bool hoverTop => top.hovering;
    private bool hoverBottom => bottom.hovering;

    private UIArea left;
    private UIArea right;
    private UIArea top;
    private UIArea bottom;
    
    private Rectangle mouseOffset = new Rectangle(0);
    private bool resizeLeft, resizeRight, resizeTop, resizeBottom = false;
    
    public bool resizing => resizeLeft || resizeRight || resizeTop || resizeBottom;

    private void ModeChanged(Input input)
    {
        if ((hoverTop && hoverLeft) || (hoverRight && hoverBottom)) input.ChangeCursorType(StandardCursor.NwseResize);
        else if ((hoverTop && hoverRight) || (hoverLeft && hoverBottom)) input.ChangeCursorType(StandardCursor.NeswResize);
        else if (hoverLeft || hoverRight) input.ChangeCursorType(StandardCursor.HResize);
        else if (hoverTop || hoverBottom) input.ChangeCursorType(StandardCursor.VResize);
        else input.ChangeCursorType(StandardCursor.Default);
    }

    public RectResizer()
    {
        self = provider.GetOrAddRoutine<Rect>();
        left = new UIArea(self.rect, transform);
        right = new UIArea(self.rect, transform);
        top = new UIArea(self.rect, transform);
        bottom = new UIArea(self.rect, transform);
        left.cursorShape = StandardCursor.HResize;
        right.cursorShape = StandardCursor.HResize;
        top.cursorShape = StandardCursor.VResize;
        bottom.cursorShape = StandardCursor.VResize;
        bottom.areaCombinations = new Dictionary<UIArea, StandardCursor>() { 
            { left, StandardCursor.NwseResize }, { right, StandardCursor.NeswResize }};
        left.areaCombinations = new Dictionary<UIArea, StandardCursor>() { 
            { top, StandardCursor.NwseResize }, { bottom, StandardCursor.NeswResize }};
        right.areaCombinations = new Dictionary<UIArea, StandardCursor>() { 
            { bottom, StandardCursor.NwseResize }, { top, StandardCursor.NeswResize }};
        top.areaCombinations = new Dictionary<UIArea, StandardCursor>() { 
            { right, StandardCursor.NwseResize }, { left, StandardCursor.NeswResize }};
    }

    public override void Destroy()
    {
        left.Destroy();
        right.Destroy();
        top.Destroy();
        bottom.Destroy();
    }

    public override void Input(Input input)
    {
        // Debug.Log("self.rect.PointIntersection(relativeMouse)");
        base.Frame();
        // Vector2 relativeMouse = Transform().WorldToLocal(input.GetMousePos(0));
        Vector2 relativeMouse = Transform<T2D>().worldToLocal * input.GetMousePos();

        Rectangle leftrect = self.rect;
        Rectangle rightrect = self.rect;
        Rectangle toprect = self.rect;
        Rectangle bottomrect = self.rect;
        leftrect = self.rect with { Right = self.rect.Left + 10 };
        rightrect = self.rect with { Left = self.rect.Right - 10 };
        toprect = self.rect with { Bottom = self.rect.Top - 10 };
        bottomrect = self.rect with { Top = self.rect.Bottom + 10 };
        left.shape = leftrect;
        right.shape = rightrect;
        top.shape = toprect;
        bottom.shape = bottomrect;
        
        if (input.GetMouseButton(0) && !resizing)
        {
            resizeRight = hoverRight;
            resizeLeft = hoverLeft;
            resizeTop = hoverTop;
            resizeBottom = hoverBottom;
            mouseOffset = new Rectangle(
                self.rect.Left - relativeMouse.x,
                self.rect.Bottom - relativeMouse.y,
                self.rect.Top - relativeMouse.y,
                self.rect.Right - relativeMouse.x
            );
        } else if (!input.GetMouseButton(0) && resizing)
        {
            resizeRight = false;
            resizeLeft = false;
            resizeTop = false;
            resizeBottom = false;
        }

        if (resizing)
        {
            if (resizeRight) self.Right = relativeMouse.x + mouseOffset.Right;
            if (resizeLeft) self.Left = relativeMouse.x + mouseOffset.Left;
            if (resizeTop) self.Top = relativeMouse.y + mouseOffset.Top;
            if (resizeBottom) self.Bottom = relativeMouse.y + mouseOffset.Bottom;
        }
    }
}

public class RectAnchor : Routine
{
    public Rect self { get; private set; }
    public PropertyBinding<Rect> parent
    {
        get
        {
            // if (field != null) return field;
            if (transform.parent == null) return field = new PropertyBinding<Rect>(()=>null);
            if (transform.parent.attachedObject is Scene s) return field = new PropertyBinding<Rect>(()=>s.rect);
            return field = new PropertyBinding<Rect>(()=>((Thing)transform.parent.attachedObject).GetOrAddRoutine<Rect>()); // dunno if will work
        }
    }

    /// <summary>
    /// a point on the parent rect in normalized space
    /// </summary>
    public Rectangle anchor = new Rectangle(0, 0, 0, 0);
    public Rectangle offset = new Rectangle(0, 0, 0, 0);

    public readonly bool keepInBounds = true; // Todo: make it function.

    public RectAnchor()
    {
        if (owner is Scene s) self = s.rect;
        else self = provider.GetOrAddRoutine<Rect>();
    }
    
    public override void Frame()
    {
        base.Frame();
        // if (parent == null) return;
        
        float Right = parent.value.Right - offset.Right - anchor.Right * parent.value.rect.width;
        if (Right < parent.value.Left + offset.Left + self.minSize.x) Right = parent.value.Left + offset.Left + self.minSize.x;
        float Left = parent.value.Left + offset.Left + anchor.Left * parent.value.rect.width;
        if (Left > parent.value.Right - offset.Right - self.minSize.x) Left = parent.value.Right - offset.Right - self.minSize.x;
        
        float Top = parent.value.Top - offset.Top - anchor.Top * parent.value.rect.height;
        if (Top < parent.value.Bottom + offset.Bottom + self.minSize.y) Top = parent.value.Bottom + offset.Bottom + self.minSize.y;
        float Bottom = parent.value.Bottom + offset.Bottom + anchor.Bottom * parent.value.rect.height;
        if (Bottom > parent.value.Top - offset.Top - self.minSize.y) Bottom = parent.value.Top - offset.Top - self.minSize.y;

        self.RescaleCentered(new Vector2(Math.Abs(Left - Right), Math.Abs(Bottom - Top)));

        if (self.transform == null) throw new Exception("transform is null");
        if (self.transform is not Transform2D)
        {
            // Debug.Log(self.transform.GetType());
            return;
        }
        self.Transform<T2D>().x = (Left + Right) / 2;
        self.Transform<T2D>().y = (Bottom + Top) / 2;
    }
}

public class ListLayout : RenderRoutine
{
    public enum Direction { TopToBottom, LeftToRight, BottomToTop, RightToLeft }

    public Rect self { get; private set; }
    public Rectangle selfRect => self.rect;
    public List<Rect> targets = new List<Rect>();
    public bool overrideTargets = false;
    public Direction direction = Direction.TopToBottom;
    public bool stretch = false;

    public bool vertical => direction is Direction.TopToBottom or Direction.BottomToTop;
    public bool horizontal => direction is Direction.RightToLeft or Direction.LeftToRight;
    
    public ListLayout()
    {
        RecalculateLayout();
    }

    public void RecalculateLayout()
    {
        targets.Clear();
        self = provider.GetOrAddRoutine<Rect>();
        foreach (Transform child in transform.children)
        {
            Rect? ar = ((IRoutineProvider?)child.attachedObject)?.GetRoutine<Rect>();
            if (ar != null) targets.Add(ar);
        }
    }

    public override void Draw()
    {
        base.Draw();
        if (!overrideTargets && targets.Count != transform.children.Count) RecalculateLayout();
        
        float increment = vertical ? (direction == Direction.TopToBottom ? self.insidePadding.Top : self.insidePadding.Bottom) : 
                                     (direction == Direction.LeftToRight ? self.insidePadding.Left :  self.insidePadding.Right);
        float stretchRatio = vertical ? self.rect.size.y / self.minSize.y : self.rect.size.x / self.minSize.x;
        if (!stretch) stretchRatio = 1;
        foreach (Rect target in targets)
        {
            if (vertical)
            {
                target.rect.Rescale(new Vector2(
                        self.rect.width - target.outsidePadding.LeftRight - self.insidePadding.LeftRight,
                        MathF.Min(target.minSize.y * stretchRatio, target.maxSize.y)
                        ), true);
                if (target.minSize.x + target.outsidePadding.LeftRight + self.insidePadding.LeftRight > self.minSize.x)
                    self.minSize.x = target.minSize.x + target.outsidePadding.LeftRight + self.insidePadding.LeftRight;
            }
            else
            {
                target.rect.Rescale(new Vector2(
                        MathF.Min(target.minSize.x * stretchRatio, target.maxSize.x),
                        self.rect.height - target.outsidePadding.TopBottom - self.insidePadding.TopBottom
                        ), true);
                if (target.minSize.y + target.outsidePadding.TopBottom + self.insidePadding.TopBottom > self.minSize.y)
                    self.minSize.y = target.minSize.y + target.outsidePadding.TopBottom + self.insidePadding.TopBottom;
            }

            target.center = direction switch
            {
                Direction.TopToBottom => new Vector2(
                    self.origin.x + (target.outsidePadding.Left - target.outsidePadding.Right)/2,
                    self.rect.Top - target.rect.height / 2 - target.outsidePadding.Top - increment),
                Direction.BottomToTop => new Vector2(
                    self.origin.x - (target.outsidePadding.Left - target.outsidePadding.Right)/2,
                    self.rect.Bottom + target.rect.height / 2 + target.outsidePadding.Bottom + increment),
                Direction.RightToLeft => new Vector2(
                    self.rect.Right - target.rect.width / 2 - target.outsidePadding.Right - increment, 
                    self.origin.y - (target.outsidePadding.Top - target.outsidePadding.Bottom)/2),
                Direction.LeftToRight => new Vector2(
                    self.rect.Left + target.rect.width / 2 + target.outsidePadding.Left + increment, 
                    self.origin.y + (target.outsidePadding.Top - target.outsidePadding.Bottom)/2),
                _ => target.center
            };

            if (vertical)
                increment += target.rect.height + target.outsidePadding.TopBottom;
            else
                increment += target.rect.width + target.outsidePadding.LeftRight;
        }
        if (vertical)
            self.minSize.y = (increment + self.insidePadding.Bottom) / stretchRatio;
        else
            self.minSize.x = (increment + self.insidePadding.Right) / stretchRatio;
        
        if (self.rect.width < self.minSize.x) self.rect.RescaleX(MathF.Min(self.minSize.x, self.maxSize.x));
        if (self.rect.height < self.minSize.y) self.rect.RescaleY(MathF.Min(self.minSize.y, self.minSize.y));
    }
}

public class SimpleButton : RenderRoutine
{
    public Rect self { get; private set; }
    public Shape _shape = new Rectangle(-30, -10, 10, 30);
    public Shape shape => self?.rect ?? _shape;
    public UIArea area;
    public bool isDown
    {
        get => field;
        set
        {
            if (field == value) return;
            if (value == true) onClick?.Invoke();
            if (value == false) onRelease?.Invoke();
            field = value;
        }
    } = false;
    public bool isHovered
    {
        get => field;
        set
        {
            if (field == value) return;
            if (value == true)
            {
                onHoverEnter?.Invoke();
                // Input.current.ChangeCursorType(StandardCursor.Hand);
            }
            if (value == false)
            {
                onHoverExit?.Invoke();
                // Input.current.ChangeCursorType(StandardCursor.Default);
            }
            field = value;
        }
    } = false;
    
    public Action? onClick;
    public Action? onRelease;
    public Action? onHoverEnter;
    public Action? onHoverExit;

    public Color neutralColor = Color.white;
    public Color clickedColor = Color.red;
    public Color hoveredColor = Color.orange;

    public static Thing CreateTemplate(Transform? parent, string text, Vector2? minSize = null, bool separateText = false, Action? OnClick = null)
    {
        if (separateText)
        {
            Thing newButton = new Thing(parent, text + " (SimpleButton)");
            Rect r = newButton.AddRoutine<Rect>();
            if (minSize != null) r.minSize = (Vector2)minSize;
            newButton.AddRoutine<SimpleButton>().onClick = OnClick;
            newButton.AddRoutine<MaskShape>().mask = new Mask(r.Draw);
            TextRenderer tr = new Thing(newButton.transform).AddRoutine<TextRenderer>();
            tr.text = text;
            tr.horizontalAlignment = TextRenderer.HorizontalAlignment.Center;
            tr.verticalAlignment = TextRenderer.VerticalAlignment.Center;
            tr.color = Color.black;
            tr.fontSize = MathF.Min(r.minSize.x, r.minSize.y) + 2;
            tr.provider.AddRoutine<Rect>().rect = tr.textRect;
            tr.provider.AddRoutine<WheelMotion>();
            // Debug.Log(tr.Transform<T2D>().position);
            // if (tr.owner is ManagedRoutineProvider MRP) MRP.GetOrAddRoutine<FitRect>();
            if (r.minSize.x < tr.textRect.size.x+8) r.minSize.x = tr.textRect.size.x+8;
            return newButton;
        }
        else
        {
            Thing newButton = new Thing(parent, text + " (SimpleButton)");
            Rect r = newButton.AddRoutine<Rect>();
            if (minSize != null) r.minSize = (Vector2)minSize;
            newButton.AddRoutine<SimpleButton>().onClick = OnClick;;
            TextRenderer tr = newButton.AddRoutine<TextRenderer>();
            tr.text = text;
            tr.horizontalAlignment = TextRenderer.HorizontalAlignment.Center;
            tr.verticalAlignment = TextRenderer.VerticalAlignment.Center;
            tr.color = Color.black;
            tr.fontSize = MathF.Min(r.minSize.x, r.minSize.y) + 2;
            if (r.minSize.x < tr.textRect.size.x+8) r.minSize.x = tr.textRect.size.x+8;
            return newButton;
        }
    }
    
    public SimpleButton()
    {
        isHovered = false;
        self = provider.GetOrAddRoutine<Rect>();
        area = new UIArea(shape, transform);
        area.cursorShape = StandardCursor.Hand;
    }

    public override void Destroy()
    {
        area.Destroy();
        base.Destroy();
    }

    public override void Input(Input input)
    {
        area.shape = shape;
        area.UpdateState();
        if (!input.GetMouseButton(0)) isHovered = area.hovering;
        if (isHovered && input.GetMouseButton(0)) isDown = true;
        if (isDown && !input.GetMouseButton(0)) isDown = false;
    }

    public override void Draw()
    {
        shader.Use();
        if (isDown)
            shader.SetColor("color", clickedColor);
        else if (isHovered)
            shader.SetColor("color", hoveredColor);
        else
            shader.SetColor("color", neutralColor);
        shader.SetMatrix4("transform",  transform.matrix);
        shape.Draw();
    }
}

public class Slider<T> : RenderRoutine where T : INumber<T>
{
    private UIArea area;
    private T defaultValue = T.Zero;
    private bool carrying = false;
    
    public Rect self { get; private set; }
    public Rectangle rect => self.rect;

    public T value
    {
        get => target.getter();
        set => target.setter(value);
    }

    public PropertyBinding<T> target;

    public T min = T.Zero;
    public T max = T.CreateChecked(10);
    public Action? onValueChanged;

    public bool loop = true;

    public int roundingDigits = 6;
    public bool isInteger = false;

    public static Thing CreateTemplate(Transform? parent, T min, T max)
    {
        Thing sliderContainer = new Thing(parent, "SliderContainer");
        ListLayout ll = sliderContainer.AddRoutine<ListLayout>();
        ll.direction = ListLayout.Direction.LeftToRight;
        ll.stretch = true;
        ll.self.insidePadding = new Rectangle(0);
        ll.self.minSize.x = 90;
        
        Thing sliderText =  new Thing(sliderContainer.transform, "SliderText");
        Rect r = sliderText.AddRoutine<Rect>();
        r.minSize.y = 18;
        r.outsidePadding = new Rectangle(0, 0, 0, 2);
        sliderText.AddRoutine<SimpleButton>();
        
        TextRenderer tr = sliderText.AddRoutine<TextRenderer>();
        tr.text = "test";
        tr.horizontalAlignment = TextRenderer.HorizontalAlignment.Center;
        tr.verticalAlignment = TextRenderer.VerticalAlignment.Center;
        tr.color = Color.black;
        tr.fontSize = 18;
        r.maxSize.x = 40;
        
        Thing newSlider = new Thing(sliderContainer.transform,  "Slider "+ "<" + typeof(T).Name + ">");
        newSlider.AddRoutine<Rect>().outsidePadding = new Rectangle(2, 0, 0, 0);
        Slider<T> s = newSlider.AddRoutine<Slider<T>>();
        s.min = min;
        s.max = max;
        
        DebugText dt = sliderText.AddRoutine<DebugText>();
        dt.getter = () =>
        {
            if (tr.textRect.size.x > r.maxSize.x)
            {
                r.minSize = tr.textRect.size;
                r.maxSize.x = tr.textRect.size.x;
                s.self?.minSize = r.minSize;
            }
            return s.value.ToString() ?? "0";
        };

        return sliderContainer;
    }
    
    public Slider()
    {
        area = new UIArea(new Rectangle(5, true), transform);
        area.cursorShape = StandardCursor.Hand;
        isInteger = typeof(T).GetInterfaces().Any(i => i.IsGenericType 
                    && i.GetGenericTypeDefinition() == typeof(IBinaryInteger<>));
        self = provider.GetOrAddRoutine<Rect>();
        target = new PropertyBinding<T>(() => defaultValue);
        if (isInteger) roundingDigits = 0;
    }

    public override void Input(Input input)
    {
        base.Input(input);
        // Vector2 relativeMouse = Transform().WorldToLocal(input.GetMousePos(0));
        Vector2 relativeMouse = Transform<T2D>().worldToLocal * input.GetMousePos(0);
        Matrix4x4 tm = transform.matrix;
        float halfHeight = rect.height / 2;
        float relativeWidth = rect.width - rect.height;
        float relativeValue = float.CreateChecked(target.getter() - min) / float.CreateChecked(max-min);
        
        Rectangle handleRect = new Rectangle(
            new Vector2(relativeValue*relativeWidth+rect.Left, -halfHeight),
            new Vector2(rect.height));
        area.shape = handleRect;
        
        // if (!input.GetMouseButton(0)) hovering = handleRect.PointIntersection(relativeMouse);
        areaHover = area.hovering;
        if (area.hovering && input.GetMouseButton(0)) carrying = true;
        if (carrying && !input.GetMouseButton(0)) carrying = false;
        
        if (carrying)
        {
            float normalized = (relativeMouse.x - rect.Left - halfHeight) / relativeWidth;
            if (loop)
            {
                normalized %= 1;
                if (normalized < 0)
                    normalized += 1;
            }
            else normalized = Math.Clamp(normalized, 0, 1);
            
            T newValue =
                T.CreateChecked(
                    MathF.Round(
                        normalized
                        * float.CreateChecked(max - min) + float.CreateChecked(min), roundingDigits
                    )
                );
            if (loop) newValue = newValue % max;
            target.setter(newValue);
            if (target.getter().Equals(newValue)) onValueChanged?.Invoke();
            carrying = input.GetMouseButton(0);
        }
        area.UpdateState();
    }

    private bool areaHover = false;
    public override void Draw()
    {
        // Vector2 relativeMouse = transform.WorldToLocal(input.GetMousePos(0));
        Matrix4x4 tm = transform.matrix;
        float halfHeight = rect.height / 2;
        float relativeWidth = rect.width - rect.height;
        float relativeValue = float.CreateChecked(target.getter() - min) / float.CreateChecked(max-min);
        
        ShapeRenderer.DrawRectangle(shader, rect, new Color(1, 1, 1, 0.2f), tm);
        
        ShapeRenderer.DrawRectangle(shader, new Rectangle(rect.position+new Vector2(halfHeight), new Vector2(relativeWidth, 2)), Color.gray, tm);
        
        Rectangle handleRect = new Rectangle(
                    new Vector2(relativeValue*relativeWidth+rect.Left, -halfHeight),
                    new Vector2(rect.height));
        
        if (carrying)
        {
            ShapeRenderer.DrawRectangle(shader, handleRect, Color.orange, tm);
        }
        else if ( areaHover )
        {
            ShapeRenderer.DrawRectangle(shader, handleRect, Color.red, tm);
        }
        else
            ShapeRenderer.DrawRectangle(shader, handleRect, Color.white, tm);
    }
}

public class DebugText : Routine
{
    private TextRenderer text;

    /// <summary>
    /// getter method that sets the text of this routine every Frame()
    /// </summary>
    public Func<string> getter
    {
        get => field ??= () => Engine.FPS.ToString();
        set;
    }

    public DebugText()
    {
        text ??= provider.GetOrAddRoutine<TextRenderer>();
    }

    public override void Frame()
    {
        text.text = getter();
    }
}

/// <summary>
/// This struct represents a getter-setter pair to any field or property, whether that's in a struct or class.
/// </summary>
/// <typeparam name="T">type of the property to bind to</typeparam>
public readonly struct PropertyBinding<T>
{
    public readonly Func<T> getter;
    public readonly Action<T> setter;
    public readonly string name; // for serialization

    public T value
    {
        get => getter();
        set => setter(value);
    }

    /// <summary>
    /// Create and store a copy of getter-setter pair for a parameter.
    /// </summary>
    /// <param name="expression">expression to access a property or field, i.e. `() => transform.position.x`</param>
    public PropertyBinding(Expression<Func<T>> expression)
    {
        getter = GetGetter(expression);
        setter = GetSetter(expression);
        name = GetMemberRepresentation(expression.Body); // for serialization
    }
    
    /// <summary>
    /// Create and store an interactive copy of getter and setter methods for said expression.
    /// </summary>
    /// <param name="getter">getter method for said property or field</param>
    /// <param name="setter">getter method for said property or field</param>
    /// <param name="name">name of this binding for ease of use, usually for serialization. If empty name will be set automatically</param>
    public PropertyBinding(Func<T> getter, Action<T> setter, string name = "")
    {
        this.getter = getter;
        this.setter = setter;
        this.name = name; // for serialization
        if (name == "") this.name = "PropertyBinding<" + typeof(T).Name + ">";
    }
    
    public PropertyBinding(Func<object?> getter, Action<object> setter, string name)
    {
        this.getter = () => (T)getter();
        this.setter = (f) => setter(f);
        this.name = name; // for serialization
    }
    
    private static string GetMemberRepresentation(Expression expression)
    {
        List<string> parts = [];
        while (expression is MemberExpression memberExp)
        {
            parts.Add(memberExp.Member.Name);
            expression = memberExp.Expression!;
        }
        return string.Join(".", parts);
    }

    /// <summary>
    /// Enter an expression such as `() => transform.position.x` and extract the getter of target parameter.
    /// </summary>
    /// <returns>a function that returns the value of said parameter.</returns>
    public static Func<T> GetGetter(Expression<Func<T>> expression) => expression.Compile();
    
    /// <summary>
    /// Enter an expression such as `() => transform.position.x` and extract the setter of target parameter.
    /// </summary>
    /// <returns>an action that sets said parameter's value.</returns>
    public static Action<T> GetSetter(Expression<Func<T>> expression)
    {
        
        // value to write, will be the setter's parameter
        ParameterExpression value = Expression.Parameter(typeof(T), "value");

        try
        {
            // get a version of the expression so it copies the struct, changes the target value on a copy, and writes the copy back
            Expression assignment = BuildAssignment(expression.Body, value);

            // compile into delegate
            return Expression.Lambda<Action<T>>(assignment, value).Compile();
        }
        catch (Exception e)
        {
            Debug.Log($"An error occurred while building setter of a PropertyBinding<{typeof(T)}> " + e, DebugLogLevel.Error);
            return T => { };
        }
    }

    private static Expression BuildAssignment(Expression target, Expression value)
    {
        // recursively build back up until target is a direct member of a class
        switch (target)
        {
            // target IS a direct member of a class
            case MemberExpression { Expression: { Type.IsValueType: false } } mem:
                return Expression.Assign(target, value);
            // target is a field in a struct located in a class
            case MemberExpression { Expression: { } structParent } structMem:
            {
                ParameterExpression structCopy = Expression.Variable(structParent.Type, "temp");

                return Expression.Block(
                    variables: [structCopy],
                    expressions: [
                        Expression.Assign(structCopy, structParent),              // temp = transform.position
                        Expression.Assign(Expression.MakeMemberAccess(structCopy, structMem.Member), value), // temp.x = value
                        BuildAssignment(structParent, structCopy)                 // recurse: write temp back up the chain
                    ]
                );
            }
            // bruh, what even is target?
            default:
                throw new InvalidOperationException($"Cannot build setter for expression: {target}");
        }
    }
}

public interface IMemberEditor
{
    public static abstract int GetPriority(Type type, MemberInfo member);

    public static abstract Thing Generate(PropertyBinding<object?> binding, MemberInfo memberInfo, object? target);
}

public class TransformEditor : IMemberEditor
{
    public static int GetPriority(Type type, MemberInfo member)
    {
        return type.IsAssignableTo(typeof(Transform)) ? 10 : 0;
    }

    public static Thing Generate(PropertyBinding<object?> binding, MemberInfo memberInfo, object? target)
    {
        return SimpleButton.CreateTemplate(null, binding.name + " (Nu Uh)", new Vector2(18));
    }
}

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
public class DefultMemberEditor : IMemberEditor
{
    public static int GetPriority(Type type, MemberInfo member)
    {
        return 1;
    }
    
    public static Thing Generate(PropertyBinding<object?> binding, MemberInfo memberInfo, object? target)
    {
        string name = binding.name;
        if (memberInfo.DeclaringType != target?.GetType()) name = "+" + name;
        
        SimpleButton b = SimpleButton.CreateTemplate(null, name, new Vector2(16), true).GetOrAddRoutine<SimpleButton>();
        TextRenderer tr = b.thing.GetRoutineFromChildren<TextRenderer>()!;
        b.onClick = () =>
        {
            tr.text = binding.value?.ToString() ?? "???";
            InspectorPortal.targetEditingText = tr;
            InspectorPortal.binding = new PropertyBinding<object?>(
                    binding.getter,
                    binding.setter,
                    memberInfo.Name
                );
            // Debug.Log(memberInfo.HasSetter() ?? "no");
        };
        if (!memberInfo.HasSetter()) b.neutralColor = Color.gray;

        return b.thing;
    }
}

public static class FieldOrProperty
{
    public static bool HasGetter(this MemberInfo member)
    {
        return (member is FieldInfo) || (member as PropertyInfo)?.GetMethod != null;
    }
    public static bool HasSetter(this MemberInfo member)
    {
        return (member is FieldInfo) || (member as PropertyInfo)?.SetMethod != null;
    }
    public static object? GetValue(this MemberInfo member, object? target)
    {
        if (member is FieldInfo F) return F.GetValue(target);
        if (member is PropertyInfo P) return P.GetValue(target);
        return null;
    }
    public static T GetValue<T>(this MemberInfo member, object? target)
    {
        if (member is FieldInfo F) return (T)F.GetValue(target);
        if (member is PropertyInfo P) return (T)P.GetValue(target);
        throw new Exception("member is neither field or property!");
    }
    public static void SetValue(this MemberInfo member, object? target, object? value)
    {
        (member as PropertyInfo)?.SetValue(target, value);
        (member as FieldInfo)?.SetValue(target, value);
    }
    
    public static Type? GetFieldOrPropertyType(this MemberInfo member) => 
        ((member is FieldInfo) ? 
            (member as FieldInfo)?.FieldType : 
            (member as PropertyInfo)?.PropertyType) ?? 
        null;
}

public static class TypeExtensions
{
    static Type[] AllConcreteTypes => // get all types in all loaded assemblies
        field ??= AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .Where(t => !t.IsAbstract && !t.ContainsGenericParameters)
            .Distinct()
            .ToArray();
    
    public static Type[] GetPossibleGenericTypes(Type T)
    {
        if (!T.IsGenericTypeDefinition) return [T];
        
        List<Type> final = new List<Type>();
        Type GP = T.GetGenericArguments()[0];
            
        if (T.GetGenericArguments().Length != 1 && Debug.Log("skipped " + T + " because it has too many generic types", DebugLogLevel.Error)) return [];

        Type[] constraints = GP.GetGenericParameterConstraints();
            
        if (constraints.Length == 0 && Debug.Log("skipped " + T + " because it is generic with no constraints", DebugLogLevel.Error)) return [];

        foreach ( Type GT in AllConcreteTypes )
        {
            // if (constraints.Any(c => !c.IsAssignableFrom(GT))) continue;

            Type GPT;
            try {
                GPT = T.MakeGenericType(GT);
            } catch (Exception e) { continue; }
                
            final.Add(GPT);
        }
        return final.ToArray();

    }
    
    public static string GetPrettyName(Type t)
    {
        if (!t.IsGenericType) return t.Name;

        string name = t.Name[..t.Name.IndexOf('`')];          // "Slider"
        IEnumerable<string> args = t.GetGenericArguments().Select(GetPrettyName); // recurse for nested generics
        return $"{name}<{string.Join(", ", args)}>";
    }
}

public class BasePortal : Scene
{
    int lastParentVersion = -1;
    private GameWindow? gameWindow = null;
    
    public override void Frame()
    {
        if (transform.parentVersion != lastParentVersion)
        {
            if (transform.parent == null)
            {
                if (gameWindow == null)
                {
                    gameWindow = new GameWindow(WindowOptions.Default with { Size = new Vector2D<int>(400, 400) });
                    gameWindow.scene = this;
                }
            }
            else
            {
                if (gameWindow?.scene == this) gameWindow.Close();
            }
            lastParentVersion = transform.parentVersion;
        }
    }
}

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
                if (targetPortal == null || FableEngine.Input.current.GetKey(Key.ShiftLeft))
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

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ShowDeclared : Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class Slidable(float min, float max) : Attribute
{
    public float min = min;
    public float max = max;
}

public class Positioner : Routine
{
    public float positionX
    {
        get => Transform<T2D>().position.x;
        set => Transform<T2D>().position = new Vector2(value, Transform<T2D>().position.y);
    }
    public float positionY
    {
        get => Transform<T2D>().position.y;
        set => Transform<T2D>().position = new Vector2(Transform<T2D>().position.x, value);
    }
    [Slidable(0, Single.Pi*2)]
    public float rotation
    {
        get => Transform<T2D>().rotation;
        set => Transform<T2D>().rotation = value;
    }
    
    [Slidable(-4, 4)]
    public int sliderX
    {
        get => 0;
        set => Transform<T2D>().position += new Vector2(value, 0);
    }
    [Slidable(-4, 4)]
    public int sliderY
    {
        get => 0;
        set => Transform<T2D>().position += new Vector2(0, value);
    }

}

// todo: not sure if this thing is actually called a Binary Tree
public class BinaryTree : Routine
{
    public float percentage = 0.5f;
    public float borderThickness =5;
    public bool reverse = false;

    public int axis
    {
        get;
        set
        {
            field = Math.Abs(value);
            reverse = (value < 0);
            rescaler.cursorShape = value == 1 ? StandardCursor.VResize : StandardCursor.HResize;
        }
    } = VectorAxis.X;
    public Rect? aSide;
    public Rect? bSide;
    public bool ignoreMinSize = false;
    
    private readonly UIArea rescaler;
    private bool rescaling = false;
    private readonly Rect self;

    public BinaryTree()
    {
        rescaler = new UIArea(new Rectangle(), transform);
        rescaler.cursorShape = StandardCursor.HResize;
        self = provider.GetOrAddRoutine<Rect>();
    }

    public override void Frame()
    {
        base.Frame();
        Vector2 origin = self.origin;
        self.minSize.x = (aSide == null ? 0 : aSide.minSize.x) + (bSide == null ? 0 : bSide.minSize.x);
        self.minSize.y = (aSide == null ? 0 : aSide.minSize.y) + (bSide == null ? 0 : bSide.minSize.y);
        if (aSide != null)
        {
            IVector aSidePosition = origin;
            aSidePosition[axis] += (percentage / 2 - 0.5f) * self.rect.size[axis] - borderThickness/4;
            aSide.transform.Position = aSidePosition;
            IVector aSideSize = self.rect.size;
            aSideSize[axis] *= percentage;
            aSideSize[axis] -= borderThickness/2;
            aSide.rect.Rescale((Vector2)aSideSize, true);
        }
        if (bSide != null)
        {
            IVector bSidePosition = origin;
            bSidePosition[axis] += (percentage / 2) * self.rect.size[axis] + borderThickness/4;
            bSide.transform.Position = bSidePosition;
            IVector bSideSize = self.rect.size;
            bSideSize[axis] *= 1 - percentage;
            bSideSize[axis] -= borderThickness/2;
            bSide.rect.Rescale((Vector2)bSideSize, true);
        }
        
    }

    public override void Input(Input input)
    {
        rescaler.UpdateState();
        if (axis == 1)
            rescaler.shape = new Rectangle( (self.origin + new Vector2(0, (percentage-0.5f)*self.height)), new Vector2(self.width, 10), true);
        else
            rescaler.shape = new Rectangle( (self.origin + new Vector2((percentage-0.5f)*self.width, 0)), new Vector2(10, self.height), true);
        
        
        if (rescaler.clicked) rescaling = true;
        
        Vector2 relativeMouse = transform.worldToLocal * input.GetMousePos();

        if (rescaling)
        {
            if (axis == 1)
                percentage = Math.Clamp(
                    (relativeMouse.y - self.Bottom) / self.height,
                    (aSide != null && !ignoreMinSize)?aSide.minSize.y/self.height:0,
                    (bSide != null && !ignoreMinSize)? MathF.Max(1 - bSide.minSize.y / self.height, bSide.minSize.y/self.height):1);
            else
                percentage = Math.Clamp(
                    (relativeMouse.x - self.Left) / self.width,
                    (aSide != null && !ignoreMinSize)?aSide.minSize.x/self.width:0,
                    (bSide != null && !ignoreMinSize)? MathF.Max(1 - bSide.minSize.x / self.width, bSide.minSize.x/self.width):1);
        }

        if (!input.GetMouseButton(0)) rescaling = false;
    }
}

public readonly struct Context(object target, object[]? objects = null, Type[]? suppressed = null)
{
    public static IContextAction[] AllContextActions =>
        field ??=
        [
            .. Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.IsAssignableTo(typeof(IContextAction)) && type != typeof(IContextAction) )
                .Select(type => (IContextAction)Activator.CreateInstance(type))
        ];

    public readonly object target = target;
    public readonly object[] related = objects ?? [];
    public readonly Type[] suppressed = suppressed ?? [];
}

public interface IContextAction
{
    bool ValidateAndGenerate(Context context, ContextWindow ctxWindow, out ITransformProvider output);
}

public class ContextWindow : IDestroyable, IDisposable
{
    public Scene scene { get; private set; }
    public bool destroyed { get; private set; }

    public GameWindow? window { get; private set; }

    public bool closeAfterClick = true;
    
    private ListLayout layout;
    
    public ContextWindow(GameWindow parent, Vector2 windowPosition, Context context) {
        scene = new Scene();
        // scene.rect.insidePadding = new Rectangle(0);

        window = new GameWindow(WindowOptions.Default with
        {
            Position = (Vector2D<int>)(windowPosition + Vector2.One),
            Size = new Vector2D<int>(128, 32),
            WindowBorder = WindowBorder.Hidden,
            TopMost = true,
            Title = "context window "
        });
        window.dontInputWhenUnfocused = false;
        // parent.window.Focus();
        window.scene = this.scene;
        window.title += window.id;
        window.backgroundColor = Color.gray;
        // window.window.Closing += () =>
        // {
        //     window.UnregisterWindowMethod(UpdateHeight);
        //     window = null;
        // };
        window.parent = parent;

        layout = scene.AddRoutine<ListLayout>();
        layout.self.insidePadding = new Rectangle(2);
        layout.direction = ListLayout.Direction.TopToBottom;
        
        window.RegisterWindowMethod(WindowMethodPosition.BeforeRender, UpdateHeight);

        foreach (IContextAction action in Context.AllContextActions)
        {
            if (context.suppressed.Contains(action.GetType())) continue;
            if (action.ValidateAndGenerate(context, this, out ITransformProvider t))
            {
                t.transform.parent = scene.transform;
            }
        }
    }
    
    private void UpdateHeight(GameWindow gameWindow)
    {
        if (!(window?.focused??false) && window?.children.Count == 0) window.Close();
        if (window?.window == null) return;
        gameWindow.height = (int)layout.selfRect.height;
        scene.rectangle = new Rectangle(0, 0, window.window.Size.X, window.window.Size.Y);
        if (closeAfterClick &&
            (gameWindow.input.GetMouseButton(MouseButton.Left) ||
             (gameWindow.parent?.input.GetMouseButton(MouseButton.Left) ?? false))) gameWindow.Close();
    }
    public void Destroy()
    {
        destroyed = true;
        window?.UnregisterWindowMethod(UpdateHeight);
        window?.Close();
        window = null;
        scene.Destroy();
        Engine.DisposeLater(this);
    }
    public void Dispose()
    {
    }
}

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
                t.transform.Cast<T2D>().position = S.gameScene.transform.worldToLocal * GameWindow.GetWindowOf(S.transform)?.input.GetMousePos()??Vector2.Zero;
                contextWindow.Destroy();
            }).GetOrAddRoutine<SimpleButton>().self.outsidePadding = new Rectangle(1);
        SimpleButton.CreateTemplate(options.transform, "New 3D Thing", new Vector2(16), false,
            () =>
            {
                GameWindow? win = GameWindow.GetWindowOf(S.transform);
                if (win == null) return;
                Thing t = new Thing(S.gameScene.transform);
                t.AddRoutine<ShapeRenderer>().shape = new Rectangle(-20, 40);
                t.transform.Cast<T2D>().position = S.gameScene.transform.worldToLocal * GameWindow.GetWindowOf(S.transform)?.input.GetMousePos()??Vector2.Zero;
                contextWindow.Destroy();
            }).GetOrAddRoutine<SimpleButton>().self.outsidePadding = new Rectangle(0);
        options.AddRoutine<ListLayout>().self.insidePadding = new Rectangle(1);
        options.GetRoutine<ListLayout>()?.self.outsidePadding = new Rectangle(0);
        output = options;
        return true;
    }
}

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

public class ContextMenuProvider : Routine
{
    private UIArea area;
    private Rect self;
    private ContextWindow? contextMenu = null;

    public bool activateOnHover = false;
    public Vector2 contextMenuPosition = Vector2.Zero;
    public Vector2 clickPosition { get; private set; } = Vector2.Zero;
    public Context context = new();

    public ContextMenuProvider()
    {
        self = provider.GetOrAddRoutine<Rect>();
        area = new UIArea(self.rect, transform, false);
        // layout.self.insidePadding = new Rectangle(5);
    }

    public void Close()
    {
        contextMenu?.Destroy();
        contextMenu = null;
    }
    public override void Input(Input input)
    {
        area.UpdateState();
        area.shape = self.rect;
        if (activateOnHover)
        {
            switch (area.hovering)
            {
                case true when contextMenu == null:
                    // Debug.Log(GameWindow.current.size);
                    // Debug.Log(transform.Cast<T2D>().position);
                    contextMenu = new ContextWindow(GameWindow.current, 
                        GameWindow.current.position + (GameWindow.current.size - transform.Cast<T2D>().position) + new Vector2(self.width/2, -self.height/2), context);
                    break;
                case false when contextMenu != null && !area.hovering:
                    contextMenu.Destroy();
                    contextMenu = null;
                    break;
            }
        } else {
            if (area.rightclicked && contextMenu == null)
            {
                contextMenu = new ContextWindow(GameWindow.current, input.GetScreenMousePos(), context);
            }
        }

        if (contextMenu?.window?.destroyed??true) Close();
    }
}


public class MaskShape : RenderRoutine
{
    public Mask mask;

    public override void Draw()
    {
        // mask.depth = transform.depth;
        mask.Set();
    }

    public override void PostRender()
    {
        mask.UnSet();
    }
}

public class WheelMotion : Routine
{
    public Rect parentRect;
    public Rect self;
    public float sensitivity = 40;
    public bool invertX = true;
    public bool invertY = false;
    public float smoothness = 5;
    public bool limitRect = true;
    public IVector delta;
    public WheelMotion()
    {
        self = provider.GetOrAddRoutine<Rect>();
        parentRect = transform.parent is { attachedObject: IRoutineProvider RP } ? RP.GetOrAddRoutine<Rect>() : self;
        delta = transform.Position.Multiply(0);
    }

    public override void Input(Input input)
    {
        if (transform.parent != null)
            if (!parentRect.rect.PointIntersection(transform.parent.worldToLocal * input.GetMousePos())) return;

        delta = delta.Add(input.GetMouseWheel() * sensitivity * new Vector2((invertX ? 1 : -1), (invertY ? 1 : -1)));
    }

    public override void Frame()
    {
        if (smoothness == 0)
        {
            transform.Position = transform.Position.Add(delta);
            delta = delta.Set(0);
        }
        else
        {
            transform.Position = transform.Position.Add(delta.Divide(smoothness));
            delta = delta.Add(delta.Divide(smoothness).Negative());
        }

        if (limitRect)
        {
            Vector2 limit = new Vector2(
                MathF.Max((self.width - parentRect.width)/2, 0),
                MathF.Max((self.height - parentRect.height)/2, 0)
                ) + parentRect.origin;

            transform.Position = transform.Position.Clamp(-limit, limit);
        }
    }
}

public class FitRect : RenderRoutine
{
    private Rect self;
    private Rect? parentRect;

    public bool fitX = true;
    public bool fitY = true;

    public FitRect()
    {
        self = provider.GetOrAddRoutine<Rect>();
        if (transform.parent !=null && transform.parent.attachedObject is IRoutineProvider RP)
            parentRect = RP.GetOrAddRoutine<Rect>();
    }
    public override void Draw()
    {
        // if (transform.parent !=null && transform.parent.attachedObject is IRoutineProvider RP)
        //     parentRect = RP.GetRoutine<Rect>();
        if (parentRect == null) return;
        
        Vector2 position = self.Transform<T2D>().position;
        if (fitX)
        {
            position.x = parentRect.center.x;
            self.rect.RescaleX(parentRect.width);
        }
        if (fitY)
        {
            position.y = parentRect.center.y;
            self.rect.RescaleY(parentRect.height);
        }

        self.Transform<T2D>().position = position;
    }
}

internal static class Program
{
    public static void Main()
    {
        // Todo: display a warning dialog to ask the user for consent to abandon wayland platform
        Debug.Log("env: " + Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), DebugLogLevel.Info);
        if (OperatingSystem.IsLinux() &&
            Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland" &&
            Environment.GetEnvironmentVariable("_RELAUNCH_LOCK") == null)
        {
            Debug.Log("Wayland detected, fall back to X11!", DebugLogLevel.Warning);
            ProcessStartInfo psi = new (Environment.ProcessPath ?? "")
            {
                UseShellExecute = false,
                Environment =
                {
                    ["XDG_SESSION_TYPE"] = "x11",
                    ["_RELAUNCH_LOCK"] = "1"
                }
            };
            Process.Start(psi)?.WaitForExit();
            return;
        }
        string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        
        
        WindowOptions options = WindowOptions.Default;
        options.Title = "Fable Engine - developer version 26.7";
        options.Position = new Vector2D<int>(0, 0);
        options.Size = new Vector2D<int>(600, 600);
        options.WindowClass = "FableEngine";
        GameWindow window = new GameWindow(options);
        window.scene.transform = new Transform2D(window.scene);
        window.backgroundColor = Color.gray;
        Engine.MaxFPS = 999; // oh yeah BD)

        
        
        Scene otherScene = new Scene();
        otherScene.transform.parent = window.scene.transform;
        otherScene.backgroundColor = Color.pink;
        
        
        // Thing inspectWindow = new Thing();
        // inspectWindow.transform.parent = (window.scene.transform);
        // Rect inspectorRect = inspectWindow.GetOrAddRoutine<Rect>();
        // inspectWindow.GetOrAddRoutine<MaskShape>().mask = new Mask(inspectorRect.Draw);
        
        
        
        
        
        
        SceneHierarchyPortal hierarchyPortal = new SceneHierarchyPortal();
        hierarchyPortal.transform.parent = window.scene.transform;
        hierarchyPortal.backgroundColor = Color.orange;
        ListLayout sllay = hierarchyPortal.AddRoutine<ListLayout>();
        sllay.self.rect.Rescale(new Vector2(200, 400), true);
        sllay.self.insidePadding.Left = 0;
        
        
        
        InspectorPortal editorInspectorPortal = new InspectorPortal();
        
        GameScenePortal gameScene = new GameScenePortal(new Scene("Game Scene"), otherScene.transform);
        gameScene.gameScene.backgroundColor = Color.red;
        
        
        hierarchyPortal.targetTree = gameScene.gameScene.transform;
        hierarchyPortal.targetPortal = editorInspectorPortal;
        
        window.scene.GetOrAddRoutine<BinaryTree>().aSide = hierarchyPortal.rect;
        window.scene.GetOrAddRoutine<BinaryTree>().bSide = otherScene.rect;
        window.scene.GetOrAddRoutine<BinaryTree>().ignoreMinSize = true;
        window.scene.GetOrAddRoutine<BinaryTree>().percentage = 0.3f;
        

        editorInspectorPortal.transform.parent = otherScene.transform;
        otherScene.GetOrAddRoutine<BinaryTree>().bSide = editorInspectorPortal.rect;
        otherScene.GetOrAddRoutine<BinaryTree>().aSide = gameScene.GetOrAddRoutine<Rect>();
        otherScene.GetOrAddRoutine<BinaryTree>().axis = 1;
        otherScene.GetOrAddRoutine<BinaryTree>().ignoreMinSize = true;
        
        
        Thing fpsCounter = new Thing(window.scene.transform);
        
        Engine.Run();
    }
}

