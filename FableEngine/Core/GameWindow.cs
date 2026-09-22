using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using FableEngine.Debugging;
using FableEngine.Math;
using FableEngine.Rendering;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Shader = FableEngine.Rendering.Shader;
using FableEngine.Transforms;
using Rectangle = FableEngine.Rendering.Rectangle;

namespace FableEngine.Core;

public enum WindowMethodPosition  // todo: carry to Engine
{
    BeforeInput = 0,
    AfterInput = 1,
    BeforeRender = 1,
    AfterRender = 2,
}

/// <summary>
/// This class creates and maintains its own window for interactability. This class comes with its own Scene attached to it,
/// as well as information about the window it controls. A FableEngine program does not necessarily need a GameWindow to run,
/// but without one, there will be a console window created.
/// </summary>
public class GameWindow // todo: make this into a IDestroyable but keep Dispose() method
{
    private static Dictionary<int, GameWindow> All { get; } = []; // windows bound to IDs for convenience
    /// <summary>
    /// Current GameWindow that is being processed if in a window-dependent method, in a window-agnostic context it will return the last processed window
    /// </summary>
    public static GameWindow current { get; private set; }
    /// <summary>
    /// The GameWindow that is currently in focus
    /// </summary>
    public static GameWindow focusedWindow { get; private set; }
    
    public static List<GameWindow> allWindowsList => All.Values.ToList();
    public static int openWindowCount => All.Count;
    /// <summary>
    /// number of frames calculated since the creation of this window.
    /// </summary>
    public uint frameCount = 0;
    /// <summary>
    /// Custom methods that execute between engine managed methods, use RegisterWindowMethod() and UnregisterWindowMethod() for registeration
    /// </summary>
    private readonly Action?[] windowMethods = new Action?[5];
    public void RegisterWindowMethod(WindowMethodPosition methodPosition, Action method)
    {
        if (windowMethods[(int)methodPosition] == null) 
            windowMethods[(int)methodPosition] = new Action(method);
        else
            windowMethods[(int)methodPosition] += method;
    }
    public void UnregisterWindowMethod(Action method, WindowMethodPosition? methodPosition = null)
    {
        if (methodPosition == null)
        {
            for (int i = 0; i < 5; i++)
            {
                windowMethods[i] -= method;
            }
        }
        else
        {
            windowMethods[(int)methodPosition] -= method;
        }
    }
    
    /// <summary>
    /// A unique ID bound to this window upon creation.
    /// </summary>
    public readonly int id;
    
    /// <summary>
    /// If this window is currently focused or not
    /// </summary>
    public bool focused { get; private set; }

    public int width
    {
        get => window.Size.X;
        set => window.Size = new Vector2D<int>(value, window.Size.Y);
    }
    public int height
    {
        get => window.Size.Y;
        set => window.Size = new Vector2D<int>(window.Size.X, value);
    }

    public Vector2 size
    {
        get;
        set
        {
            window.Size = new Vector2D<int>((int)value.x, (int)value.y);
            scene.size = value;
            field = value;
        }
    } = new Vector2();

    private Vector2 positionField = new Vector2();
    public Vector2 position
    {
        get => positionField;
        set
        {
            positionField = value;
            window.Position = new Vector2D<int>((int)value.x, (int)value.y);
        }
    }

    // todo: make use of dynamic properties, and do this at setter / getter respectively
    public void RefreshPosition()
    {
        if ((int)positionField.x != window.Position.X || (int)positionField.y != window.Position.Y)
            positionField = (Vector2)window.Position; // refresh position field in case if window is moved
    }

    public string title
    {
        get => window.Title;
        set => window.Title = value;
    }

    /// <summary>
    /// Background color of the window and its scene.
    /// </summary>
    public Color backgroundColor // todo: obsolete, make use of window.scene.backgroundColor instead
    {
        get => scene.backgroundColor;
        set => scene.backgroundColor = value;
    }

    /// <summary>
    /// Whether the window uses transparent background or not. This value cannot be changed after the window is created.
    /// </summary>
    public bool isTransparent => window.TransparentFramebuffer;

    public Renderer renderer;

    public Input input;

    public GameWindow? parent
    {
        get => field;
        set
        {
            if (field == value) return;
            field?.children.Remove(this);
            field?.window.Closing -= Close;
            value?.children.Add(this);
            value?.window.Closing += Close;
            field = value;
        }
    }

    public List<GameWindow> children = new List<GameWindow>();
    
    /// <summary>
    /// The default shader used to draw sprites for FableEngine.
    /// </summary>
    public static Shader defaultShader { get; private set; } = new Shader("default", 
        """
        #version 330 core
        layout (location = 0) in vec2 aPosition;
        layout (location = 1) in vec2 aUV;
        layout (location = 2) in vec4 aColor;
        out vec2 fragUV;
        out vec4 vertexColor;
        uniform mat4 transform;
        uniform vec4 color;
        uniform vec4 UVOffset;
        void main() {
          gl_Position = transform * vec4(aPosition, 0.0, 1.0);
          fragUV = vec2(aUV.x*UVOffset.b+UVOffset.r, 1.0 - (aUV.y*UVOffset.a+UVOffset.g));
          vertexColor = aColor * color;
        }
        """,
        """
        #version 330 core
        in vec2 fragUV;
        in vec4 vertexColor;
        out vec4 FragColor;
        uniform sampler2D uTexture;
        void main() {
            FragColor = texture(uTexture, fragUV) * vertexColor;
        }
        """);
    
    /// <summary>
    /// The Scene bound to this GameWindow. This can be swapped with other scenes. Beware that
    /// position of this scene is always (0, 0), its size and background color will always be set by its window.
    /// </summary>
    public Scene scene = new();
    
    /// <summary>
    /// The OS window bound to this GameWindow.
    /// </summary>
    public IWindow window { get => renderer.window; private set => renderer.window = value; }

    /// <summary>
    /// If the window is flagged to close at the end of this frame
    /// </summary>
    public bool aboutToClose = false; 

    private void Initialize(WindowOptions windowOptions)
    {
        IWindow win = Window.Create(windowOptions);
        // if (All.Count == 0) {
        // //     current = this; }
        // id = 0;
        // while (All.ContainsKey(id))
        //     id++;
        // All.Add(id, this);
        // win.Render += Render;
        win.Resize += (newSize) =>
        {
            size = new Vector2(window.Size.X, window.Size.Y);
            scene.rectangle = new Rectangle( Vector2.Zero, size );
        };
        win.FocusChanged += b =>
        {
            if (b) focusedWindow = this;
            this.focused = b;
        };
        win.Render += Render;
        // win.FocusChanged += b => { Debug.Log("focus changed"); }; 
        Debug.Log("okay");
        win.Initialize();
        win.Size = new Vector2D<int>(windowOptions.Size.X, windowOptions.Size.Y);
        // size = new Vector2(windowOptions.Size.X, windowOptions.Size.Y);
        
        
        GL gl = win.CreateOpenGL();
        gl.Enable(EnableCap.Blend);
        gl.Enable(EnableCap.StencilTest);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        renderer = new Renderer(gl, win);
        
        // Debug.Log(win.Size.X + " " + win.Size.Y + "  --  " + window.Size.X + " " + window.Size.Y);
        // scene.size = new Vector2(windowOptions.Size.X, windowOptions.Size.Y);
        scene.rectangle = new Rectangle(Vector2.Zero, new Vector2(windowOptions.Size.X, windowOptions.Size.Y));
        input = new Input(this);
        size = new Vector2(windowOptions.Size.X, windowOptions.Size.Y);
    }

    /// <summary>
    /// Create a GameWindow using WindowOptions directly from Silk.NET. VSync is off by default for Linux platforms because VSYNC IS EVIL!!!
    /// </summary>
    /// <param name="windowOptions"> options for this window </param>
    public GameWindow(WindowOptions windowOptions)
    {
        id = 0;
        while (All.ContainsKey(id))
            id++;
        windowOptions.VSync = !OperatingSystem.IsLinux();
        Initialize(windowOptions);
        All.Add(id, this);
    }

    /// <summary>
    /// Create a new GameWindow using general options. VSync is off by default for Linux platforms because VSYNC IS EVIL!!!
    /// </summary>
    /// <param name="title"> title of the window </param>
    /// <param name="size"> size of the window, doesn't need to be pixel perfect </param>
    /// <param name="position"> position of the window, doesn't need to be pixel perfect </param>
    /// <param name="transparent"> whether to use a transparent buffer for the window, this can allow for transparent windows if the OS supports it,
    /// this option will also automatically set the background color to transparent </param>
    public GameWindow(string title, Vector2? size = null, Vector2 position = default, bool transparent = false)
    {
        id = 0;
        while (All.ContainsKey(id))
            id++;
        Vector2 Size = size ?? new Vector2(1280, 720);
        WindowOptions options = WindowOptions.Default;
        options.VSync = !OperatingSystem.IsLinux();
        options.Title = title;
        options.Size = new Vector2D<int>((int)Size.x, (int)Size.y);
        options.Position = new Vector2D<int>((int)position.x, (int)position.y);
        options.TransparentFramebuffer = transparent;
        if (transparent) scene.backgroundColor = Color.transparent;
        Initialize(options);
        this.size = Size;
        this.position = position;
        All.Add(id, this);
    }

    

    /// <summary>
    /// get the bound window of a given Transform if any.
    /// </summary>
    /// <param name="transform"> transform </param>
    /// <returns> the window given transform's root scene is bound to </returns>
    public static GameWindow? GetWindowOf(Transform transform)
    {
        Scene? s = Scene.GetSceneOf(transform);
        return s == null ? null : All.Values.FirstOrDefault(window => s == window.scene);
    }
    
    ~GameWindow() {
        Close();
    }

    /// <summary>
    /// Update methods that are window-dependent through this window.
    /// </summary>
    public void WindowUpdate()
    {
        GameWindow.current = this;
        Renderer.current = renderer;
        RefreshPosition(); // needed to detect if window is moved by the OS

        windowMethods[0]?.Invoke();
        
        foreach (var MR in ManagedRoutine.InputRunners) // Input is before render
            try { if (MR is ITransformProvider TP && GetWindowOf(TP.transform) == this) MR.Input(input); }
            catch (Exception e) {
                Debug.Log(e, DebugLogLevel.Error);
                Debug.Log("The error casting ManagedRoutine will be deactivated to avoid further errors!", DebugLogLevel.Error);
                MR.active = false;
            }
                
        windowMethods[1]?.Invoke();
        
        // if (size != scene.rectangle.size) size = scene.rectangle.size; // todo: implement min and max window size
        // if (window.Size.X < scene.rect.minSize.x || window.Size.Y < scene.rect.minSize.y)
        //     window.Size = new Vector2D<int>(
        //         (int)MathF.Max(window.Size.X, scene.rect.minSize.x),
        //         (int)MathF.Max(window.Size.Y, scene.rect.minSize.y));
        window.DoRender();
        DoEvents();
        frameCount++;
        
        windowMethods[2]?.Invoke();
    }
    
    /// <summary>
    /// close this game window
    /// </summary>
    public void Close()
    {
        aboutToClose = true;
        parent?.children.Remove(this);
    }

    /// <summary>
    /// Dispose this window, remove it from global list, and close it. This will close the window but will not destroy the Scene,
    /// it will instead be unparented. If you want to destroy this window with everything bound to it, use Destroy() instead.
    /// </summary>
    public void Dispose()
    {
        All.Remove(id);
        window.Close();
        window.DoEvents();
        window.SwapBuffers();
        window.Dispose();
        input.Dispose();
        Shader.ClearGL(renderer.gl);
        All.Remove(id);
    }

    /// <summary>
    /// Interpret necessary window events such as cosing, moving, resizing, etc. and increment current frame count. This will close the window and dispose if necessary,
    /// so only call it after you've done anything else with the window for the frame.
    /// You do not need to call gameWindow.window.DoEvents if you call this method instead.
    /// </summary>
    public void DoEvents()
    {
        if (aboutToClose || window.IsClosing)
        {
            Dispose();
            return;
        }
        window.DoEvents();
    }

    
    /// <summary>
    /// Render the scene tied to this window, onto itself
    /// </summary>
    public void Render(double d)
    {
        if (window.IsClosing) return;
        
        renderer.gl.Viewport(0, 0, (uint)window.Size.X, (uint)window.Size.Y);
        
        renderer.gl.StencilMask(0xFF);
        renderer.gl.ClearColor(0, 0, 0, 0);
        renderer.gl.ClearStencil(1);
        renderer.gl.Clear(ClearBufferMask.ColorBufferBit |  ClearBufferMask.StencilBufferBit);
        
        defaultShader.Use();

        scene.mask.func = StencilFunction.Always;
        scene.Draw();
    }
}
