using System.Runtime.CompilerServices;
using FableEngine.Core;
using FableEngine.Math;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace FableEngine;

public class Input : IDisposable
{
    /// <summary>
    /// Current input in input-dependent and window-dependent methods. Will return latest used Input outside of that context.
    /// </summary>
    public static Input current => GameWindow.current.input;
    
    public GameWindow parentWindow { get; private set; }
    private readonly IInputContext input;
    private readonly Dictionary<IKeyboard, string> lastChar = new();

    public Input(GameWindow parent)
    {
        input = parent.window.CreateInput();
        parentWindow = parent;
        input.ConnectionChanged += HandleDeviceConnection;
        foreach (IKeyboard k in input.Keyboards) HandleDeviceConnection(k, true);
        parent.RegisterWindowMethod(WindowMethodPosition.AfterInput, InputUpdate);
    }
    private void InputUpdate()
    {
        foreach (IKeyboard keyboard in lastChar.Keys)
        {
            lastChar[keyboard] = "";
        }
    }
    private void HandleCharInput(IKeyboard keyboard, char c)
    {
        lastChar[keyboard] += c;
    }
    private void HandleDeviceConnection(IInputDevice device, bool status)
    {
        if (device is IKeyboard keyboard)
        {
            if (status)
            {
                keyboard.KeyChar += HandleCharInput;
                lastChar.Add(keyboard, "");
            }
            else
            {
                keyboard.KeyChar -= HandleCharInput;
                lastChar.Remove(keyboard);
            }
        }
    }

    public IInputDevice[] GetAllConnectedDevices() => 
        [..input.OtherDevices, ..input.Gamepads, ..input.Joysticks, ..input.Keyboards, ..input.Mice];

    public T[] GetConnectedDevices<T>() where T : IInputDevice => GetAllConnectedDevices().OfType<T>().ToArray();
    
    

    public bool AnyCharInput(int keyboardIndex = 0) => 
        keyboardIndex >= 0 && 
        keyboardIndex < lastChar.Count && 
        lastChar.ElementAt(keyboardIndex).Value != "";
    
    public string GetKeyboardInputStream(int keyboardIndex = 0) => 
        (keyboardIndex < 0 || keyboardIndex >= lastChar.Count) ? 
        "" : lastChar.ElementAt(keyboardIndex).Value;

    public void Dispose()
    {
        parentWindow.UnregisterWindowMethod(InputUpdate);
        input.Dispose();
        lastChar.Clear();
        parentWindow = null!;
        GC.SuppressFinalize(this);
    }

    public void ChangeCursorType(StandardCursor cursorType, int mouseIndex = 0)
    {
        if (input.Mice.Count <= mouseIndex || mouseIndex < 0) return;
        input.Mice[mouseIndex].Cursor.StandardCursor = cursorType;
    }

    public Vector2 GetMousePos(int mouseIndex = 0, bool invertY = false)
    {
        if (input.Mice.Count <= mouseIndex || mouseIndex < 0)
            return new Vector2(0);
        Vector2 pos = new Vector2(input.Mice[mouseIndex].Position.X, parentWindow.height-input.Mice[mouseIndex].Position.Y) / parentWindow.size * 2 - new Vector2(1, 1);
        if (invertY) pos *= new Vector2(1, -1);
        return pos;
    }
    
    public Vector2 GetScreenMousePos(int mouseIndex = 0, bool invertY = false)
    {
        if (input.Mice.Count <= mouseIndex || mouseIndex < 0)
            return new Vector2(0);
        Vector2 mousePos = GetMousePos(mouseIndex);
        return new Vector2(
            (int)((mousePos.x+1)/2 * parentWindow.width + (int)parentWindow.position.x), 
            (int)((1-mousePos.y)/2 * parentWindow.height + (int)parentWindow.position.y)
        );
    }
    public Vector2D<int> GetScreenMousePosIntegrated(int mouseIndex = 0, bool invertY = false)
    {
        if (input.Mice.Count <= mouseIndex || mouseIndex < 0)
            return new Vector2D<int>(0);
        Vector2 mousePos = GetMousePos(mouseIndex);
        return new Vector2D<int>(
            (int)((mousePos.x+1)/2 * parentWindow.width + (int)parentWindow.position.x), 
            (int)((1-mousePos.y)/2 * parentWindow.height + (int)parentWindow.position.y)
        );
    }
    
    public bool GetMouseButton(MouseButton button, int mouseIndex = 0)
    {
        if (input.Mice.Count <= mouseIndex || mouseIndex < 0)
            return false;
        return input.Mice[mouseIndex].IsButtonPressed(button);
    }
    
    public Vector2 GetMouseWheel(int mouseIndex = 0)
    {
        if ( input.Mice.Count <= mouseIndex || mouseIndex < 0) 
            return Vector2.Zero;
        var wheel = input.Mice[mouseIndex].ScrollWheels[0];
        return new Vector2(wheel.X, wheel.Y);
    }

    public bool GetKey(Key key, int keyboardIndex = 0)
    {
        if (input.Keyboards.Count <= keyboardIndex || keyboardIndex < 0)
            return false;
        return input.Keyboards[keyboardIndex].IsKeyPressed(key);
    }
    
}