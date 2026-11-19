using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
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

public class BasePortal : Scene
{
    int lastParentVersion = -1;
    private GameWindow? gameWindow = null;

    public BasePortal()
    {
    }
    
    public override void Frame()
    {
        if (transform.parentVersion != lastParentVersion)
        {
            Debug.Log("parent versions: ", (transform.parentVersion != lastParentVersion), "   ", transform.parentVersion, " - ", lastParentVersion);
            // Debug.Log("parent changed");
            if (transform.parent == null)
            {
                if (gameWindow == null)
                {
                    gameWindow = new GameWindow(WindowOptions.Default with { Size = new Vector2D<int>((int)size.x, (int)size.y) });
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

public class EditorWindow : GameWindow
{
    private BinaryTree rootTree;

    private void Initialize()
    {
        
    }
    
    public EditorWindow(WindowOptions windowOptions) : base(windowOptions)
    {
        rootTree = new BinaryTree(scene.transform);
    }

    public EditorWindow(string title, Vector2? size = null, Vector2 position = default, bool transparent = false) : base(title, size, position, transparent)
    {
        rootTree = new BinaryTree(scene.transform);
    }
}

public struct Tracker<T>(T target)
{
    private T oldValue = target;
    public bool Value(T target)
    {
        bool returner = oldValue != null && oldValue.Equals(target);
        if (!returner) oldValue = target;
        return oldValue != null && oldValue.Equals(target);
    }
}

// todo: not sure if this thing is actually called a Binary Tree
public class BinaryTree : Thing
{
    public float percentage = 0.5f;
    public float borderThickness = 5;
    public bool reverse = false;
    public bool empty = false;
    // public bool missing => (aSide == null || bSide == null);

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
    public Ref<BinaryTree> aSide;
    public Ref<BinaryTree> bSide;
    public bool ignoreMinSize = false;
    
    private UIArea rescaler;
    private bool rescaling = false;
    private readonly Rect self;
    
    public BinaryTree(Transform? parent)
    {
        transform.parent = parent;
        rescaler = new UIArea(new Rectangle(0), transform, true);
        self = GetOrAddRoutine<Rect>();
    }
    private BinaryTree(BinaryTree parent)
    {
        transform.parent = parent.transform;
        rescaler = new UIArea(new Rectangle(0), transform, true);
        self = GetOrAddRoutine<Rect>();
    }

    public (BinaryTree A, BinaryTree B) Split(bool passAllContentToB)
    {
        (BinaryTree A, BinaryTree B) = Split();

        foreach (Transform child in transform.children)
        {
            if (child != A.transform && child != B.transform)
                child.parent = passAllContentToB ? B.transform : A.transform;
        }

        return (A, B);
    }
    
    public (BinaryTree A, BinaryTree B) Split(ITransformProvider? aContent = null, ITransformProvider? bContent = null)
    {
        if (aContent == this || bContent == this)
            throw new NotSupportedException("None of the Binary Tree's newly split content can be itself!");
        
        BinaryTree A = new BinaryTree(this);
        aContent?.transform.parent = A.transform;
        
        BinaryTree B = new BinaryTree(this);
        bContent?.transform.parent = B.transform;
        
        int index = transform.children.IndexOf(A.transform);
        if (index != 0)
            (transform.children[0], transform.children[index]) = (transform.children[index], transform.children[0]);
        index = transform.children.IndexOf(B.transform);
        if (index != 1)
            (transform.children[1], transform.children[index]) = (transform.children[index], transform.children[1]);

        aSide = A;
        bSide = B;
        
        return (A, B);
    }

    
    int oldParent = -1;
    public override void Frame()
    {
        base.Frame();
        Vector2 origin = self.origin;

        if (parent?.attachedObject is IRoutineProvider PRP and not BinaryTree)
        {
            Rect? PR = PRP.GetRoutine<Rect>();

            if (PR != null)
                self.rect = PR.rect;
        }

        BinaryTree? A = aSide;
        BinaryTree? B= bSide;
        
        foreach (Transform child in transform.children)
        {
            if (child == A?.transform || child == B?.transform)
                continue;
            
            if (child.attachedObject is not IRoutineProvider RP)
                continue;
            
            Rect? R = RP.GetRoutine<Rect>();
            
            if (R == null) continue;

            R.rect = self.rect;
        }

        if (A?.transform.children.Count == 0)
        {
            foreach (Transform child in A.transform.children)
                child.parent = transform;
            A.Destroy();
            A = aSide.value;
        }
        if (B?.transform.children.Count == 0)
        {
            foreach (Transform child in B.transform.children)
                child.parent = transform;
            B.Destroy();
            B = bSide.value;
        }
        
        if (A == null && B == null)
            return;
        
        if (A == null || B == null)
        {
            BinaryTree side = (A ?? B)!;

            while (side.transform.children.Count > 0)
                side.transform.GetChild(0).parent = transform;
            
            side.Destroy();
            return;
        }
        
        int index = transform.children.IndexOf(A.transform);
        if (index != 0)
            (transform.children[0], transform.children[index]) = (transform.children[index], transform.children[0]);
        index = transform.children.IndexOf(B.transform);
        if (index != 1)
            (transform.children[1], transform.children[index]) = (transform.children[index], transform.children[1]);
        
        IVector aSidePosition = origin;
        aSidePosition[axis] += (percentage / 2 - 0.5f) * self.rect.size[axis] - borderThickness/4;
        A.transform.Position = aSidePosition;
        IVector aSideSize = self.rect.size;
        aSideSize[axis] *= percentage;
        aSideSize[axis] -= borderThickness/2;
        A.self.rect.Rescale((Vector2)aSideSize, true);
        
        IVector bSidePosition = origin;
        bSidePosition[axis] += (percentage / 2) * self.rect.size[axis] + borderThickness/4;
        B.transform.Position = bSidePosition;
        IVector bSideSize = self.rect.size;
        bSideSize[axis] *= 1 - percentage;
        bSideSize[axis] -= borderThickness/2;
        B.self.rect.Rescale((Vector2)bSideSize, true);
        
    }

    public override bool Input(Input input)
    {
        if (aSide.value == null || bSide.value == null)
            return base.Input(input);
        
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
                    (!ignoreMinSize)?aSide.value.self.minSize.y/self.height:0,
                    (!ignoreMinSize)? MathF.Max(1 - bSide.value.self.minSize.y / self.height, bSide.value.self.minSize.y/self.height):1);
            else
                percentage = Math.Clamp(
                    (relativeMouse.x - self.Left) / self.width,
                    (!ignoreMinSize)?aSide.value.self.minSize.x/self.width:0,
                    (!ignoreMinSize)? MathF.Max(1 - bSide.value.self.minSize.x / self.width, bSide.value.self.minSize.x/self.width):1);
        }

        if (!input.GetMouseButton(0)) rescaling = false;
        return base.Input(input);
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
        layout.self.insidePadding = new Rectangle(0);
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
        // otherScene.transform.parent = window.scene.transform;
        otherScene.backgroundColor = Color.pink;
        
        
        
        
        
        SceneHierarchyPortal hierarchyPortal = new SceneHierarchyPortal();
        // hierarchyPortal.transform.parent = window.scene.transform;
        hierarchyPortal.backgroundColor = Color.orange;
        ListLayout sllay = hierarchyPortal.AddRoutine<ListLayout>();
        sllay.self.rect.Rescale(new Vector2(200, 400), true);
        sllay.self.insidePadding.Left = 0;
        
        
        
        InspectorPortal editorInspectorPortal = new InspectorPortal();
        
        GameScenePortal gameScene = new GameScenePortal(new Scene("Game Scene"), otherScene.transform);
        gameScene.gameScene.backgroundColor = Color.red;
        
        hierarchyPortal.targetTree = gameScene.gameScene.transform;
        hierarchyPortal.targetPortal = editorInspectorPortal;
        
        
        
        BinaryTree rootTree = new BinaryTree(window.scene.transform);
        rootTree.ignoreMinSize = true;
        rootTree.percentage = 0.3f;

        (_, BinaryTree B ) = rootTree.Split(hierarchyPortal, null);

        B.axis = -VectorAxis.Y;
        B.Split(editorInspectorPortal, gameScene);
        
        


        // editorInspectorPortal.transform.parent = otherScene.transform;
        // otherScene.GetOrAddRoutine<BinaryTree>().axis = 1;
        // otherScene.GetOrAddRoutine<BinaryTree>().ignoreMinSize = true;
        
        
        Thing fpsCounter = new Thing(window.scene.transform);
        
        Debug.stackSameLines = false;
        LogChildren(window.scene.transform);
        Debug.stackSameLines = true;
        
        Engine.Run();
        
        Debug.stackSameLines = false;
        LogChildren(window.scene.transform);
        Debug.stackSameLines = true;

    }

    private static void LogChildren(Transform transform)
    {
        
        string pre = "";
        for (int i = 0; i < transform.depth; i++)
        {
            pre += "| ";
        }
        pre += "|-";
        Debug.stackSameLines = false;
        Debug.Log(pre, transform.attachedObject.name, "(", transform.attachedObject.GetType(), ")"); 
        foreach(Transform child in transform.children)
            LogChildren(child);
    }
}

