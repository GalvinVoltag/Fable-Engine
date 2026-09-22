using FableEngine.Debugging;

namespace FableEngine;

public static class Time
{
    private static float _rawDeltaTime;
    private static int temporalLayer;
    private static int frameCount = 0;
    private static float rawTimeSinceStart = 0;

    public static void OverrideRawDelta(float newDelta)
    {
        _rawDeltaTime = newDelta;
        frameCount++;
        rawTimeSinceStart += newDelta;
    }
    public static void OverrideTemporalLayer(int layer) { temporalLayer = layer; }
    private class TemporalLayer(float timeScale)
    {
        public float _timeScale = timeScale;
        public float _fixedTimeRate = 0.02f;
        public float _fixedTimeAccumulation = 0;
    }
    
    private static Dictionary<int, TemporalLayer> temporalLayers = new Dictionary<int, TemporalLayer>()
    {
        { 0, new TemporalLayer(1f)}
    };

    public static float fixedTimeAccumulation
    {
        get => temporalLayers[temporalLayer]._fixedTimeAccumulation;
        set => temporalLayers[temporalLayer]._fixedTimeAccumulation = value;
    }
    public static float deltaTime => _rawDeltaTime * temporalLayers[temporalLayer]._timeScale;

    public static float fixedTime => temporalLayers[temporalLayer]._fixedTimeRate * temporalLayers[temporalLayer]._timeScale;

    public static float rawDeltaTime => _rawDeltaTime;

    public static float rawFixedTime => temporalLayers[temporalLayer]._fixedTimeRate;

    public static float timeScale
    {
        get => temporalLayers[temporalLayer]._timeScale;
        set => temporalLayers[temporalLayer]._timeScale = value;
    }
    public static float fixedTimeRate
    {
        get => temporalLayers[temporalLayer]._fixedTimeRate;
        set => temporalLayers[temporalLayer]._fixedTimeRate = value;
    }

    public static void CreateTemporalLayer(int layer, float _timeScale = 1, float _fixedTimeRate = 0.02f)
    {
        if (temporalLayers.TryGetValue(layer, out var tlayer))
            tlayer._timeScale = _timeScale;
        else
            temporalLayers.Add(layer, new TemporalLayer(_timeScale));
        temporalLayers[layer]._fixedTimeRate = _fixedTimeRate;
    }

    public static void RemoveTemporalLayer(int layer)
    {
        if (layer != 0 && !temporalLayers.ContainsKey(layer)) return;
        temporalLayers.Remove(layer);
    }
    public static void SetTimescale(int layer, float time)
    {
        if (temporalLayers.TryGetValue(layer, out var tlayer))
            tlayer._timeScale = time;
        else
            Debug.Log("Trying to access uninstantiated temporal layer: " + layer + "!", ConsoleColor.Red);
    }
    public static float GetTimescale(int layer)
    {
        if (temporalLayers.TryGetValue(layer, out var tlayer))
            return tlayer._timeScale;
        Debug.Log("Trying to access uninstantiated temporal layer: " + layer + "!", ConsoleColor.Red);
        return 1;
    }
    public static float GetDeltaTime(int layer)
    {
        if (temporalLayers.TryGetValue(layer, out var tlayer))
            return _rawDeltaTime * tlayer._timeScale;
        Debug.Log("Trying to access uninstantiated temporal layer: " + layer + "!", ConsoleColor.Red);
        return 1;
    }
    public static void SetFixedTimeRate(int layer, float time)
    {
        if (temporalLayers.TryGetValue(layer, out var tlayer))
            tlayer._fixedTimeRate = time;
        Debug.Log("Trying to access uninstantiated temporal layer: " + layer + "!", ConsoleColor.Red);
    }
    public static float GetFixedTimeRate(int layer)
    {
        if (temporalLayers.TryGetValue(layer, out var tlayer))
            return tlayer._fixedTimeRate;
        Debug.Log("Trying to access uninstantiated temporal layer: " + layer + "!", ConsoleColor.Red);
        return 1;
    }
    public static float GetFixedTime(int layer)
    {
        if (temporalLayers.TryGetValue(layer, out var tlayer))
            return tlayer._timeScale * tlayer._fixedTimeRate;
        Debug.Log("Trying to access uninstantiated temporal layer: " + layer + "!", ConsoleColor.Red);
        return 1;
    }
}
