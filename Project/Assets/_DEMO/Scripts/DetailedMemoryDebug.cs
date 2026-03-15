using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class DetailedMemoryDebug : MonoBehaviour
{
    // Smoothing & update control
    private float deltaTime = 0.0f;
    private float accumDelta = 0f;
    private int frameCount = 0;
    private float updateInterval = 0.5f;   // refresh text every 0.5s for readability

    // For GC alloc delta tracking
    private long lastMonoUsed = 0;
    private long monoAllocThisInterval = 0;

    // For stable 5-second average FPS
    private const float AVG_WINDOW_SECONDS = 5f;
    private List<float> frameDurations = new List<float>(200); // ~200 frames for 5s @40fps, plenty
    private float totalTimeInWindow = 0f;
    private float avgFps = 0f;

    private GUIStyle style;
    private bool showOverlay = true;
    private KeyCode toggleKey = KeyCode.F1;  // press F1 to hide/show

    void Start()
    {
        style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = Mathf.Max(24, Screen.height / 60);
        style.normal.textColor = Color.white;
        style.richText = true;  // for color coding

        lastMonoUsed = GetMonoUsed();
    }

    void Update()
    {
        float unscaledDt = Time.unscaledDeltaTime;

        deltaTime += (unscaledDt - deltaTime) * 0.1f;  // smoothed instant delta

        accumDelta += unscaledDt;
        frameCount++;

        // Track GC alloc over interval
        if (accumDelta >= updateInterval)
        {
            long currentMono = GetMonoUsed();
            monoAllocThisInterval = currentMono - lastMonoUsed;
            lastMonoUsed = currentMono;

            accumDelta = 0f;
            frameCount = 0;
        }

        // Rolling 5-second FPS average
        frameDurations.Add(unscaledDt);
        totalTimeInWindow += unscaledDt;

        // Remove old frames until window is ~5 seconds
        while (totalTimeInWindow > AVG_WINDOW_SECONDS && frameDurations.Count > 0)
        {
            totalTimeInWindow -= frameDurations[0];
            frameDurations.RemoveAt(0);
        }

        // Compute average FPS (frames / time)
        if (totalTimeInWindow > 0.001f) // avoid div by zero
        {
            avgFps = frameDurations.Count / totalTimeInWindow;
        }

        if (Input.GetKeyDown(toggleKey))
            showOverlay = !showOverlay;
    }

    void OnGUI()
    {
        if (!showOverlay) return;

        float instantFps = 1.0f / deltaTime;
        float frameMs = deltaTime * 1000f;

        long gcUsed     = GC.GetTotalMemory(false) / 1048576;
        long monoHeap   = Profiler.GetMonoHeapSizeLong() / 1048576;
        long monoUsed   = Profiler.GetMonoUsedSizeLong() / 1048576;
        long totalAlloc = GetSafe(Profiler.GetTotalAllocatedMemoryLong) / 1048576;
        long totalRes   = GetSafe(Profiler.GetTotalReservedMemoryLong)  / 1048576;
        long gfxDriver  = GetSafe(Profiler.GetAllocatedMemoryForGraphicsDriver) / 1048576;

        // Dynamic color for FPS line (using avgFps for stability)
        string colorFps = avgFps >= 60 ? "<color=lime>" : (avgFps >= 30 ? "<color=yellow>" : "<color=red>");

        string text =
            colorFps + "Avg FPS (5s): " + avgFps.ToString("F0") + "</color>   " +
            colorFps + "Inst FPS: " + instantFps.ToString("F1") + "</color>   " +
            "<color=white>Frame: " + frameMs.ToString("F1") + " ms</color>\n" +
            "<color=green>GC Used:     " + gcUsed.ToString("N0").PadLeft(6) + " MB</color>   (managed live objects)\n" +
            "<color=green>Mono Heap:   " + monoHeap.ToString("N0").PadLeft(6) + " MB</color>   (total managed heap)\n" +
            "<color=green>Mono Used:   " + monoUsed.ToString("N0").PadLeft(6) + " MB</color>   (~ GC used)\n" +
            "<color=yellow>Alloc Delta: " + (monoAllocThisInterval / 1024f).ToString("F1") + " KB</color>   (over last " + updateInterval + "s)\n" +
            "<color=orange>Total Alloc: " + totalAlloc.ToString("N0").PadLeft(6) + " MB</color>   (Unity used from OS)\n" +
            "<color=orange>Total Resvd: " + totalRes.ToString("N0").PadLeft(6) + " MB</color>   (reserved incl. fragmentation)\n" +
            "<color=white>GFX Driver:  " + gfxDriver.ToString("N0").PadLeft(6) + " MB</color>   (approx graphics allocations)\n" +
            "<color=white>F1 to toggle • Editor/Dev Build for full accuracy</color>";

        float w = Screen.width * 0.35f;
        float h = Screen.height * 0.30f;

        // Black semi-transparent background
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(5, 5, w + 20, h + 20), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(10, 10, w, h), text, style);
    }

    private long GetMonoUsed() => Profiler.GetMonoUsedSizeLong();

    private long GetSafe(System.Func<long> getter)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return getter();
#else
        return 0;
#endif
    }
}