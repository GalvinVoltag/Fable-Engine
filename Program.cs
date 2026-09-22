using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Numerics;
using System.Buffers.Binary;
using System.ComponentModel.Design;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ConstrainedExecution;
using FableEngine.Math;
using FableEngine.Rendering;
using FableEngine;
using FableEngine.Core;
using FableEngine.Debugging;
using FableEngine.Math;
using FableEngine.Rendering;
using FableEngine.Transforms;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Debug = FableEngine.Debugging.Debug;
using Matrix4x4 = FableEngine.Math.Matrix4x4;
using MouseButton = Silk.NET.Input.MouseButton;
using Rectangle = FableEngine.Rendering.Rectangle;
using Shader = FableEngine.Rendering.Shader;
using Vector2 = FableEngine.Math.Vector2;
using T2D = FableEngine.Transforms.Transform2D;
using Vector = FableEngine.Math.Vector;
using Vector3 = FableEngine.Math.Vector3;
using Vector4 = FableEngine.Math.Vector4;

// using FableEngine = FableEngine.FableEngine;

public class Theme
{
    public static Theme defaultFallback;
}

public class UIArea : IDestroyable
{
    public static List<UIArea> allAreas = new List<UIArea>();
    private static uint lastUpdateFrame = 0;
    public static bool hasUpdated => lastUpdateFrame == GameWindow.current.frameCount;
    private static List<UIArea> dominantAreas = new List<UIArea>();
    
    private static void Update() // todo: performance
    {
        // Debug.Log("updating " + GameWindow.current.frameCount);
        // Debug.Log(Input.current.GetMousePos());
        lastUpdateFrame = GameWindow.current.frameCount;
        if (!Engine.isInWindowContext)
            return;
        dominantAreas.Clear();
        Input? input = null;
        foreach (UIArea area in allAreas)
        {
            input = GameWindow.GetWindowOf(area.transform)?.input;
            if (input == null) continue;
            Vector2 relativeMouse = area.transform.worldToLocal * input.GetMousePos();
            if (area.shape.PointIntersection( relativeMouse ) )
            {
                if (area.blocking) dominantAreas = dominantAreas.Where(ar => ar.transform.depth < area.transform.depth).ToList();
                dominantAreas.Insert(0, area);
            }
        }

        StandardCursor cs = StandardCursor.Arrow;
        foreach (UIArea area in dominantAreas)
        {
            cs = area.cursorShape;
            if (area.areaCombinations == null) continue;
            foreach (var pair in area.areaCombinations)
                if (dominantAreas.Contains(pair.Key))
                    cs = pair.Value;
        }

        GameWindow? win = null;
        if (dominantAreas.Count > 0)
        {
            win = GameWindow.GetWindowOf(dominantAreas[0].transform);
        }
        if (win == null) return;
        input = win.input;
        
        input?.ChangeCursorType(cs);
        // Debug.Log(cs);
    }
    
    
    
    public StandardCursor cursorShape = StandardCursor.Arrow;
    public Transform transform;
    public Dictionary<UIArea, StandardCursor>? areaCombinations;
    // Todo: add Dictionary<StandartCursor, StandardCursor>? shapeCombinations;
    public Shape shape;
    public bool blocking = true;
    public UIArea(Shape shape, Transform transform, bool blocking = true)
    {
        this.blocking = blocking;
        this.shape = shape;
        this.transform = transform;
        allAreas.Add(this);
    }

    public bool destroyed { get; private set; }

    public void Destroy()
    {
        allAreas.Remove(this);
        destroyed = true;
    }

    public bool hovering
    {
        get
        {
            if (!hasUpdated) Update();
            return dominantAreas.Contains(this);
        }
    }
    public bool clicked
    {
        get
        {
            if (!hasUpdated) Update();
            Input? input = GameWindow.GetWindowOf(transform)?.input;
            return input != null && input.GetMouseButton(MouseButton.Left, 0) && dominantAreas.Contains(this);
        }
    }
    public bool rightclicked
    {
        get
        {
            if (!hasUpdated) Update();
            Input? input = GameWindow.GetWindowOf(transform)?.input;
            return input != null && input.GetMouseButton(MouseButton.Right, 0) && dominantAreas.Contains(this);
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

    public override void Render()
    {
        // if (owner is Scene s) rect = s.shapeRenderer.shape.shapeRect;
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

    public override void Render()
    {
        base.Render();
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

    public static Thing CreateTemplate(Transform? parent, string text, Vector2? minSize = null, bool separateText = false)
    {
        if (separateText)
        // if (false)
        {
            Thing newButton = new Thing(parent, text + " (SimpleButton)");
            Rect r = newButton.AddRoutine<Rect>();
            if (minSize != null) r.minSize = (Vector2)minSize;
            newButton.AddRoutine<SimpleButton>();
            newButton.AddRoutine<MaskShape>().mask = new Mask(r.Render);
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
            newButton.AddRoutine<SimpleButton>();
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
        if (!input.GetMouseButton(0)) isHovered = area.hovering;
        if (isHovered && input.GetMouseButton(0)) isDown = true;
        if (isDown && !input.GetMouseButton(0)) isDown = false;
    }

    public override void Render()
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
    }

    public override void Render()
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
        else if ( area.hovering )
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
        Debug.Log("new binding created: " + name);
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
        Debug.Log("new binding created: " + name);
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
            SceneHierarchyPortal.targetEditingText = tr;
            SceneHierarchyPortal.binding = new PropertyBinding<object?>(
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

public class SceneHierarchyPortal : Routine
{
    public Transform? targetTree;
    public Transform? settingsPanel;
    
    
    private ListLayout _listLayout { get { return field ??= provider.GetOrAddRoutine<ListLayout>(); } }
    private List<Thing> items = [];
    private IRoutineProvider? targetProvider;

    private Type[] AllPropertyEditors =>
        field ??=
        [
            .. Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.IsAssignableTo(typeof(IMemberEditor)) && type != typeof(IMemberEditor) )
        ];
    
    public void ClearList()
    {
        foreach (Thing sb in items)
        {
            sb.Destroy();
        }
        items.Clear();
    }
    
    
    public override void Input(Input input)
    {
        if (targetEditingText == null || binding == null) return;
        
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
    }

    public static TextRenderer? targetEditingText = null;
    // public static object? targetEditingObject = null;
    // public static MemberInfo? targetEditingMember = null;
    public static PropertyBinding<object?>? binding = null;
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
    
    public void GenerateInspector(Transform? child)
    {
        // if (child.attachedObject is IDestroyable destroyable) destroyable.Destroy();
        if (child == null) return;
        if (settingsPanel == null) return;
        foreach (Transform dest in settingsPanel.children.ToArray())
            dest.Destroy();
        if (child.attachedObject is not IRoutineProvider MRP) return;
        targetProvider = MRP;
        
        SimpleButton.CreateTemplate(settingsPanel, child.GetType().Name, new Vector2(20));
        ListLayout transformLayout = new Thing(settingsPanel).GetOrAddRoutine<ListLayout>();
        GeneratePropertyList(transformLayout, child);
        
        foreach (Routine R in targetProvider.routines)
        {
            SimpleButton.CreateTemplate(settingsPanel, GetPrettyName(R.GetType()), new Vector2(20), true);
            ListLayout layout = new Thing(settingsPanel).GetOrAddRoutine<ListLayout>();
            GeneratePropertyList(layout, R);
        }

        (settingsPanel.attachedObject as IRoutineProvider)?.GetOrAddRoutine<ListLayout>().RecalculateLayout();
    }
    
    static Type[] AllConcreteTypes => // get all types in all loaded assemblies
        field ??= AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .Where(t => !t.IsAbstract && !t.ContainsGenericParameters)
            .Distinct()
            .ToArray();

    public Type[] GetPossibleGenericTypes(Type T)
    {
        if (!T.IsGenericTypeDefinition) return [T];
        
        List<Type> final = new List<Type>();
        Type GP = T.GetGenericArguments()[0];
            
        if (T.GetGenericArguments().Length != 1 && Debug.Log("skipped " + T + " because it has too many generic types", ConsoleColor.Red)) return [];

        Type[] constraints = GP.GetGenericParameterConstraints();
            
        if (constraints.Length == 0 && Debug.Log("skipped " + T + " because it is generic with no constraints", ConsoleColor.Red)) return [];

        foreach ( Type GT in AllConcreteTypes )
        {
            // if (constraints.Any(c => !c.IsAssignableFrom(GT))) continue;

            Type GPT;
            try {
                GPT = T.MakeGenericType(GT);
            } catch (Exception e) { continue; }
            Debug.Log(" --- ---> " + GT);
                
            final.Add(GPT);
        }
        Debug.Log(" -> total of " + final.Count + " possible types found");
        return final.ToArray();

    }
    
    static string GetPrettyName(Type t)
    {
        if (!t.IsGenericType) return t.Name;

        string name = t.Name[..t.Name.IndexOf('`')];          // "Slider"
        IEnumerable<string> args = t.GetGenericArguments().Select(GetPrettyName); // recurse for nested generics
        return $"{name}<{string.Join(", ", args)}>";
    }
    
    public void GenerateHierarchy(Transform? t = null, int nesting = 0)
    {
        if (t == null)
        {
            t = targetTree;
            if (t == null) return;
            ClearList();
        }

        if (settingsPanel?.attachedObject is IRoutineProvider RP && RP.GetRoutine<ContextMenuProvider>() == null)
        { 
            ContextMenuProvider CMP = RP.AddRoutine<ContextMenuProvider>();
            
            
            foreach (Type T in Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsAssignableTo(typeof(Routine))))
            {
                Thing thing = SimpleButton.CreateTemplate(null, GetPrettyName(T), new Vector2(18));
                if (!T.IsGenericType)
                    thing.GetOrAddRoutine<SimpleButton>().onClick += () =>
                    {
                        targetProvider?.AddRoutine(T);
                        GenerateInspector((targetProvider as ITransformProvider)?.transform);
                        (settingsPanel.attachedObject as IRoutineProvider)?.GetOrAddRoutine<ListLayout>().RecalculateLayout();
                    };
                CMP.elements.Add(thing);
                
                if (!T.IsGenericType) continue;
                
                ContextMenuProvider CMP2 = thing.AddRoutine<ContextMenuProvider>();
                foreach (Type TT in GetPossibleGenericTypes(T))
                {
                    Thing thingy = SimpleButton.CreateTemplate(null, GetPrettyName(TT), new Vector2(18));
                    thingy.GetOrAddRoutine<SimpleButton>().onClick += () =>
                    {
                        targetProvider?.AddRoutine(TT);
                        GenerateInspector((targetProvider as ITransformProvider)?.transform);
                        (settingsPanel.attachedObject as IRoutineProvider)?.GetOrAddRoutine<ListLayout>().RecalculateLayout();
                    };
                    CMP2.elements.Add(thingy);
                }

            }
        }

        foreach (Transform child in t.children)
        {
            // Debug.Log("going about it");
            Thing targetBtn = SimpleButton.CreateTemplate(transform, child.attachedObject.name + " (" + child.parentVersion + ")", new Vector2(18));
            targetBtn.GetRoutine<SimpleButton>()?.onClick = () => GenerateInspector(child);
            targetBtn.GetRoutine<Rect>()?.outsidePadding.Left = nesting * 20;
            items.Add(targetBtn);
            if (child.children.Count > 0) GenerateHierarchy(child, nesting+1);
        }
    }
    
    public override void PostFrame()
    {
        base.PostFrame();
        if (targetTree == null) return;
        if (items.Count != targetTree.totalChildren)
            GenerateHierarchy();
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ShowDeclared : Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class Slidable : Attribute
{
    public float min;
    public float max;

    public Slidable(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
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

// not sure if this thing is actually called a Binary Tree
public class BinaryTree : Routine
{
    public float percentage = 0.5f;
    public bool vertical
    {
        get;
        set
        {
            field = value;
            if (value)
                rescaler.cursorShape = StandardCursor.VResize;
            else
                rescaler.cursorShape = StandardCursor.HResize;
        }
    } = false;
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
            if (vertical)
            {
                aSide.Transform<T2D>().position = origin + new Vector2(0, (percentage/2 - 0.5f)*self.height);
                aSide.rect.Rescale(new Vector2(self.width, self.height*percentage), true);
            }
            else
            {
                aSide.Transform<T2D>().position = origin + new Vector2((percentage/2 - 0.5f)*self.width, 0);
                aSide.rect.Rescale(new Vector2(self.width*percentage, self.height), true);
            }
        }
        if (bSide != null)
        {
            if (vertical)
            {
                bSide.Transform<T2D>().position = origin + new Vector2(0, (percentage / 2) * self.height);
                bSide.rect.Rescale(new Vector2(self.width, self.height * (1 - percentage)), true);
            }
            else
            {
                bSide.Transform<T2D>().position = origin + new Vector2((percentage / 2) * self.width, 0);
                bSide.rect.Rescale(new Vector2(self.width * (1 - percentage), self.height), true);
            }
        }
        
    }

    public override void Input(Input input)
    {
        if (vertical)
            rescaler.shape = new Rectangle( (self.origin + new Vector2(0, (percentage-0.5f)*self.height)), new Vector2(self.width, 10), true);
        else
            rescaler.shape = new Rectangle( (self.origin + new Vector2((percentage-0.5f)*self.width, 0)), new Vector2(10, self.height), true);
        
        
        if (rescaler.clicked) rescaling = true;
        
        Vector2 relativeMouse = transform.worldToLocal * input.GetMousePos();

        if (rescaling)
        {
            if (vertical)
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

public class ContextMenuProvider : Routine
{
    private UIArea area;
    private Rect self;
    private GameWindow? gw;
    private bool focusedOnce;

    public bool activateOnHover = false;
    public List<Thing> elements = new();
    public Vector2 clickPosition { get; private set; } = Vector2.Zero;

    public ContextMenuProvider()
    {
        self = provider.GetOrAddRoutine<Rect>();
        area = new UIArea(self.rect, transform, false);
        // layout.self.insidePadding = new Rectangle(5);
    }
    /// <summary>
    /// close the context window provided by this provider
    /// </summary>
    public void Close()
    {
        gw?.Close();
    }
    public override void Input(Input input)
    {
        area.shape = self.rect;
        if (area.rightclicked && gw == null)
        {
            WindowOptions wo = WindowOptions.Default;
            clickPosition = input.GetMousePos();
            wo.Position = input.GetScreenMousePosIntegrated();
            wo.Size = new Vector2D<int>(128, 32);
            wo.WindowBorder = WindowBorder.Hidden;
            wo.TopMost = true;
            wo.Title = "context window";
            gw = new GameWindow(wo);
            gw.title += gw.id;
            gw.backgroundColor = Color.blue;
            gw.window.FocusChanged += b =>
            {
                if (!b && gw.children.Count == 0) Close();
            };
            gw.window.Closing += () =>
            {
                foreach (Thing t in elements)
                    t.transform.parent = null;
                gw = null;
            };
            gw.parent = input.parentWindow;

            foreach (Thing t in elements)
            {
                t.transform.parent = gw.scene.transform;
                t.GetRoutine<Rect>()?.outsidePadding = new Rectangle(0, 1, 0, 1);
            }
            ListLayout layout = gw.scene.AddRoutine<ListLayout>();
            layout.self.insidePadding = new Rectangle(2);
            layout.direction = ListLayout.Direction.TopToBottom;
            layout.Render();

            gw.window.Render += f =>
            {
                float incriment = 0;
                float width = 10;
                foreach (Thing t in elements)
                {
                    Rect? r = t.GetRoutine<Rect>();
                    if (r == null) continue;
                    incriment += r.minSize.y + 2;
                    if (width < r.minSize.x) width = r.minSize.x;
                }

                gw.window.Size = new Vector2D<int>((int)width,
                    (int)(incriment + (self.insidePadding.Top + self.insidePadding.Bottom) / 2));
                gw.scene.rectangle = new Rectangle(0, 0, gw.window.Size.X, gw.window.Size.Y);
            };
            
            
            // Debug.Log(gw.scene.rectangle);
            gw.scene.rectangle = new Rectangle(0, 0, gw.scene.rectangle.width, gw.scene.rectangle.height);
            // Debug.Log(gw.scene.rectangle);
        }
    }
}

public class MaskShape : RenderRoutine
{
    public Mask mask;

    public override void Render()
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
                );

            transform.Position = transform.Position.Add(0).Clamp(-limit, limit);
        }
    }
}

public class FitRect : RenderRoutine
{
    private Rect self;
    private Rect parentRect;

    public bool fitX = true;
    public bool fitY = true;

    public FitRect()
    {
        self = provider.GetOrAddRoutine<Rect>();
        if (transform.parent !=null && transform.parent.attachedObject is IRoutineProvider RP)
            parentRect = RP.GetOrAddRoutine<Rect>();
    }
    public override void Render()
    {
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
        Engine.MaxFPS = 60; // oh yeah BD)

        
        
        Scene otherScene = new Scene();
        otherScene.transform.parent = window.scene.transform;
        otherScene.backgroundColor = Color.pink;
        
        
        Thing inspectWindow = new Thing();
        inspectWindow.transform.parent = (window.scene.transform);
        Rect inspectorRect = inspectWindow.GetOrAddRoutine<Rect>();
        inspectWindow.GetOrAddRoutine<MaskShape>().mask = new Mask(inspectorRect.Render);
        
        
        Thing firstlist = new Thing(inspectWindow.transform);
        ListLayout Ll = firstlist.AddRoutine<ListLayout>();
        Ll.self.rect.size = new Vector2(100, 100);
        Ll.stretch = false;
        firstlist.AddRoutine<WheelMotion>();

        // inspectWindow.AddRoutine<WheelMotion>();
        firstlist.AddRoutine<FitRect>().fitY = false;
        
        
        
        Scene gameScene = new Scene();
        gameScene.transform.parent = otherScene.transform;
        gameScene.backgroundColor = Color.red;
        ContextMenuProvider cmp = gameScene.AddRoutine<ContextMenuProvider>();
        cmp.elements = [
            SimpleButton.CreateTemplate(null, "New Thing(2D)", new Vector2(18)),
            SimpleButton.CreateTemplate(null, "New Thing(3D)", new Vector2(18)),
            SimpleButton.CreateTemplate(null, "Settings", new Vector2(18)),
            SimpleButton.CreateTemplate(null, "Delete", new Vector2(18)),
            SimpleButton.CreateTemplate(null, "Play", new Vector2(18)),
            SimpleButton.CreateTemplate(null, "Group With", new Vector2(18)),
            SimpleButton.CreateTemplate(null, "New Light", new Vector2(18)),
            SimpleButton.CreateTemplate(null, "New Sound", new Vector2(18)),
            Slider<int>.CreateTemplate(null, 0, 5),
            SimpleButton.CreateTemplate(null, "Disparent", new Vector2(18)),
            SimpleButton.CreateTemplate(null, "Make Root", new Vector2(18))
        ];
        cmp.elements[0].GetOrAddRoutine<SimpleButton>().onClick =
            () =>
            {
                GameWindow? win = GameWindow.GetWindowOf(gameScene.transform);
                if (win == null) return;
                Thing t = new Thing(gameScene.transform);
                t.AddRoutine<ShapeRenderer>().shape = new Rectangle(-20, 40);
                t.transform.Cast<T2D>().position = cmp.transform.worldToLocal * cmp.clickPosition;
                // Debug.Log(cmp.transform.localToWorld * cmp.clickPosition);
                cmp.Close();
            };
        
        
        
        
        Scene testScene = new Scene();
        testScene.transform.parent = window.scene.transform;
        testScene.backgroundColor = Color.orange;
        ListLayout sllay = testScene.AddRoutine<ListLayout>();
        sllay.self.rect.Rescale(new Vector2(200, 400), true);
        sllay.self.insidePadding.Left = 0;
        
        
        
        testScene.AddRoutine<SceneHierarchyPortal>().targetTree = gameScene.transform;
        testScene.GetRoutine<SceneHierarchyPortal>()?.settingsPanel = firstlist.transform;

        
        
        
        window.scene.GetOrAddRoutine<BinaryTree>().aSide = testScene.rect;
        window.scene.GetOrAddRoutine<BinaryTree>().bSide = otherScene.rect;
        window.scene.GetOrAddRoutine<BinaryTree>().ignoreMinSize = true;
        

        new Thing(inspectWindow.transform).AddRoutine<Rect>().maxSize.y = 10;

        inspectWindow.transform.parent = otherScene.transform;
        inspectWindow.transform.Cast<T2D>().position = Vector2.Zero;
        
        otherScene.GetOrAddRoutine<BinaryTree>().bSide = inspectWindow.GetOrAddRoutine<Rect>();
        otherScene.GetOrAddRoutine<BinaryTree>().aSide = gameScene.GetOrAddRoutine<Rect>();
        otherScene.GetOrAddRoutine<BinaryTree>().vertical = true;
        otherScene.GetOrAddRoutine<BinaryTree>().ignoreMinSize = true;
        
        
        Thing firstButton = SimpleButton.CreateTemplate(firstlist.transform, nameof(firstButton), new Vector2(18, 18));
        firstButton.GetRoutine<SimpleButton>()?.onClick = () =>
        {
            firstButton.transform.parent?.children[1].attachedObject?.Destroy();
        };
        SimpleButton.CreateTemplate(firstlist.transform, "one", new Vector2(18, 18));
        SimpleButton.CreateTemplate(firstlist.transform, "two", new Vector2(18, 18));
        new Thing(firstlist.transform).AddRoutine<Rect>().provider.AddRoutine<Slider<double>>();
        new Thing(firstlist.transform).AddRoutine<Rect>().provider.AddRoutine<Slider<int>>();
        Slider<int>.CreateTemplate(firstlist.transform, 0, 10);
        Slider<float>.CreateTemplate(firstlist.transform, 0, 10);
        new Thing(firstlist.transform).AddRoutine<Rect>().provider.AddRoutine<SimpleButton>().onClick = () =>
            {
                new Thing(firstlist.transform).AddRoutine<Rect>().provider.AddRoutine<SimpleButton>();
            };
        
        new Thing(inspectWindow.transform).AddRoutine<Rect>();
        
        
        Thing fpsCounter = new Thing(window.scene.transform);
        
        Engine.Run();
    }
}

