using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class PerformanceDebugger : MonoBehaviour
{
    [Header("Display")]
    public bool showInGameUI = true;
    public TextMeshProUGUI debugText;
    public CanvasGroup debugPanel;
    
    [Header("Monitoring")]
    public float updateInterval = 0.5f;
    public bool logToConsole = true;
    
    [Header("Detection")]
    public float lowFPSThreshold = 30f;
    public bool autoDetectIssues = true;
    
    // Performance stats
    private float deltaTime = 0f;
    private float fps = 0f;
    private float lowestFPS = 999f;
    private float highestFPS = 0f;
    private float avgFPS = 0f;
    private float[] fpsHistory = new float[60];
    private int fpsIndex = 0;
    
    // Component tracking
    private Dictionary<string, float> componentTimes = new Dictionary<string, float>();
    private Dictionary<string, int> componentCalls = new Dictionary<string, int>();
    
    // Memory stats
    private float peakMemoryUsage = 0f;
    
    // Bottleneck detection
    private bool isGPUBound = false;
    private bool isCPUBound = false;
    
    void Start()
    {
        if (showInGameUI && debugText == null)
        {
            CreateDebugUI();
        }
        
        StartCoroutine(MonitorPerformance());
        StartCoroutine(LogPerformanceSummary());
    }
    
    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fps = 1f / deltaTime;
        
        // Track FPS history
        fpsHistory[fpsIndex % fpsHistory.Length] = fps;
        fpsIndex++;
        
        // Track min/max
        if (fps < lowestFPS) lowestFPS = fps;
        if (fps > highestFPS) highestFPS = fps;
        
        // Calculate average
        avgFPS = fpsHistory.Where(f => f > 0).DefaultIfEmpty().Average();
        
        // Check for low FPS
        if (autoDetectIssues && fps < lowFPSThreshold)
        {
            DetectPerformanceIssue();
        }
        
        UpdateDisplay();
    }
    
    IEnumerator MonitorPerformance()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            
            // Monitor memory
            float memoryUsage = System.GC.GetTotalMemory(false) / 1024f / 1024f; // MB
            if (memoryUsage > peakMemoryUsage) peakMemoryUsage = memoryUsage;
            
            // Monitor component performance
            if (logToConsole && fps < lowFPSThreshold)
            {
                UnityEngine.Debug.LogWarning($"[Performance] Low FPS: {fps:F1} | Memory: {memoryUsage:F1}MB | Peak: {peakMemoryUsage:F1}MB");
            }
        }
    }
    
    IEnumerator LogPerformanceSummary()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f);
            
            if (logToConsole)
            {
                UnityEngine.Debug.Log("=== PERFORMANCE SUMMARY ===");
                UnityEngine.Debug.Log($"FPS - Min: {lowestFPS:F1} | Max: {highestFPS:F1} | Avg: {avgFPS:F1}");
                UnityEngine.Debug.Log($"Memory - Peak: {peakMemoryUsage:F1} MB");
                UnityEngine.Debug.Log($"Device: {SystemInfo.deviceModel}");
                UnityEngine.Debug.Log($"CPU: {SystemInfo.processorType}");
                UnityEngine.Debug.Log($"GPU: {SystemInfo.graphicsDeviceName}");
                UnityEngine.Debug.Log($"RAM: {SystemInfo.systemMemorySize} MB");
            }
        }
    }
    
    void DetectPerformanceIssue()
    {
        // Check for high object count
        int activeObjects = FindObjectsByType<GameObject>().Count(o => o.activeInHierarchy);
        if (activeObjects > 1000)
        {
            UnityEngine.Debug.LogWarning($"High object count: {activeObjects} active objects");
        }
        
        // Check for particle systems
        var particleSystems = FindObjectsByType<ParticleSystem>();
        int activeParticles = particleSystems.Count(p => p.isPlaying);
        if (activeParticles > 50)
        {
            UnityEngine.Debug.LogWarning($"High particle system count: {activeParticles} active");
        }
        
        // Check for heavy UI components
        var rawImages = FindObjectsByType<RawImage>();
        if (rawImages.Length > 20)
        {
            UnityEngine.Debug.LogWarning($"Many RawImages: {rawImages.Length} (each uses texture memory)");
        }
        
        // Check for excessive update calls
        var monoBehaviours = FindObjectsByType<MonoBehaviour>();
        int activeScripts = monoBehaviours.Count(m => m.isActiveAndEnabled);
        if (activeScripts > 200)
        {
            UnityEngine.Debug.LogWarning($"Many active scripts: {activeScripts}");
        }
        
        // Check for realtime lights
        var lights = FindObjectsByType<Light>();
        int realtimeLights = lights.Count(l => l.type != LightType.Rectangle);
        if (realtimeLights > 5)
        {
            UnityEngine.Debug.LogWarning($"Many realtime lights: {realtimeLights}");
        }
        
        // Check for shadow-casting objects
        var renderers = FindObjectsByType<Renderer>();
        int shadowCasters = renderers.Count(r => r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off);
        if (shadowCasters > 30)
        {
            UnityEngine.Debug.LogWarning($"Many shadow casters: {shadowCasters}");
        }
        
        // Check for animator components
        var animators = FindObjectsByType<Animator>();
        int activeAnimators = animators.Count(a => a.isActiveAndEnabled);
        if (activeAnimators > 20)
        {
            UnityEngine.Debug.LogWarning($"Many active Animators: {activeAnimators}");
        }
    }
    
    void CreateDebugUI()
    {
        var canvas = new GameObject("PerformanceDebugCanvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var canvasScaler = canvas.gameObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1080, 1920);
        
        debugPanel = canvas.gameObject.AddComponent<CanvasGroup>();
        
        var bg = new GameObject("Background").AddComponent<Image>();
        bg.transform.SetParent(canvas.transform);
        bg.color = new Color(0, 0, 0, 0.7f);
        bg.rectTransform.anchorMin = new Vector2(0, 1);
        bg.rectTransform.anchorMax = new Vector2(1, 1);
        bg.rectTransform.sizeDelta = new Vector2(0, 200);
        bg.rectTransform.anchoredPosition = new Vector2(0, 0);
        
        debugText = new GameObject("DebugText").AddComponent<TextMeshProUGUI>();
        debugText.transform.SetParent(canvas.transform);
        debugText.fontSize = 24;
        debugText.color = Color.white;
        debugText.alignment = TextAlignmentOptions.TopLeft;
        debugText.rectTransform.anchorMin = new Vector2(0, 1);
        debugText.rectTransform.anchorMax = new Vector2(1, 1);
        debugText.rectTransform.sizeDelta = new Vector2(-20, 180);
        debugText.rectTransform.anchoredPosition = new Vector2(10, -10);
    }
    
    void UpdateDisplay()
    {
        if (!showInGameUI || debugText == null) return;
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<color=yellow>FPS: {fps:F1}</color>");
        sb.AppendLine($"Min: {lowestFPS:F1} | Max: {highestFPS:F1}");
        sb.AppendLine($"Frame Time: {deltaTime * 1000:F1}ms");
        sb.AppendLine($"Memory: {System.GC.GetTotalMemory(false) / 1024f / 1024f:F1} MB");
        sb.AppendLine($"Active Objects: {FindObjectsByType<GameObject>().Count(o => o.activeInHierarchy)}");
        
        if (fps < lowFPSThreshold)
        {
            sb.AppendLine($"<color=red>⚠️ LOW FPS DETECTED!</color>");
        }
        
        debugText.text = sb.ToString();
    }
    
    // Button to toggle debug panel
    public void ToggleDebugPanel()
    {
        if (debugPanel != null)
        {
            debugPanel.alpha = debugPanel.alpha == 1 ? 0 : 1;
        }
    }
    
    // Export performance data
    public string ExportPerformanceData()
    {
        var data = new System.Text.StringBuilder();
        data.AppendLine("=== PERFORMANCE DATA ===");
        data.AppendLine($"Timestamp: {System.DateTime.Now}");
        data.AppendLine($"Device: {SystemInfo.deviceModel}");
        data.AppendLine($"OS: {SystemInfo.operatingSystem}");
        data.AppendLine($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
        data.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");
        data.AppendLine($"RAM: {SystemInfo.systemMemorySize} MB");
        data.AppendLine($"FPS Min: {lowestFPS:F1}");
        data.AppendLine($"FPS Max: {highestFPS:F1}");
        data.AppendLine($"FPS Avg: {avgFPS:F1}");
        data.AppendLine($"Peak Memory: {peakMemoryUsage:F1} MB");
        return data.ToString();
    }
}