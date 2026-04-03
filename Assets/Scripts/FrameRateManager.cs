using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System;
using UnityEngine.InputSystem.Utilities;

public class FrameRateManager : MonoBehaviour
{
    public enum FrameRateMode
    {
        Unlimited,   // No cap, best performance
        High,        // 120 FPS
        Balanced,    // 60 FPS
        PowerSave    // 30 FPS
    }
    
    [Header("Settings")]
    [SerializeField] private FrameRateMode defaultMode = FrameRateMode.High;
    [SerializeField] private bool enableIdleReduction = true;
    [SerializeField] private float idleThreshold = 30f;
    [SerializeField] private int idleFPS = 30;
    
    private FrameRateMode currentMode;
    private float lastInputTime;
    private bool isIdle = false;
    private IDisposable inputListener;
    
    private static FrameRateManager instance;
    public static FrameRateManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("FrameRateManager");
                instance = go.AddComponent<FrameRateManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        LoadSettings();
        ApplyFrameRate(currentMode);
        StartInputListening();
    }
    
    void StartInputListening()
    {
        // Listen for any button press using the correct API
        inputListener = InputSystem.onAnyButtonPress
            .CallOnce(OnAnyButtonPressed);
    }
    
    void OnAnyButtonPressed(InputControl control)
    {
        if (!enableIdleReduction) return;
        
        lastInputTime = Time.unscaledTime;
        
        if (isIdle)
        {
            ExitIdleMode();
        }
        
        // Re-subscribe for next button press
        StartInputListening();
    }
    
    void OnDestroy()
    {
        inputListener?.Dispose();
    }
    
    void Update()
    {
        if (!enableIdleReduction || currentMode != FrameRateMode.High) return;
        
        if (!isIdle && Time.unscaledTime - lastInputTime >= idleThreshold)
        {
            EnterIdleMode();
        }
    }
    
    private void EnterIdleMode()
    {
        isIdle = true;
        Application.targetFrameRate = idleFPS;
        Debug.Log($"Frame rate reduced to {idleFPS} FPS (idle)");
    }
    
    private void ExitIdleMode()
    {
        isIdle = false;
        ApplyFrameRate(currentMode);
    }
    
    public void SetMode(FrameRateMode mode)
    {
        currentMode = mode;
        SaveSettings();
        ApplyFrameRate(mode);
        
        if (isIdle)
        {
            isIdle = false;
        }
    }
    
    public void SetMode(int modeIndex)
    {
        if (modeIndex >= 0 && modeIndex < Enum.GetValues(typeof(FrameRateMode)).Length)
        {
            SetMode((FrameRateMode)modeIndex);
        }
    }
    
    public void CycleMode()
    {
        int nextMode = ((int)currentMode + 1) % Enum.GetValues(typeof(FrameRateMode)).Length;
        SetMode((FrameRateMode)nextMode);
    }
    
    private void ApplyFrameRate(FrameRateMode mode)
    {
        QualitySettings.vSyncCount = 0;
        
        int targetFPS = GetTargetFPS(mode);
        Application.targetFrameRate = targetFPS;
        
        SetScreenRefreshRate(targetFPS);
        
        Debug.Log($"Frame rate set to: {mode} ({targetFPS} FPS)");
    }
    
    private int GetTargetFPS(FrameRateMode mode)
    {
        switch (mode)
        {
            case FrameRateMode.Unlimited:
                return -1;
            case FrameRateMode.High:
                return 120;
            case FrameRateMode.Balanced:
                return 60;
            case FrameRateMode.PowerSave:
                return 30;
            default:
                return 60;
        }
    }
    
    private void SetScreenRefreshRate(int targetFPS)
    {
        if (targetFPS <= 0) return;
        
        var currentResolution = Screen.currentResolution;
        var refreshRate = new RefreshRate
        {
            numerator = (uint)targetFPS,
            denominator = 1
        };
        
        if (targetFPS <= currentResolution.refreshRateRatio.value)
        {
            Screen.SetResolution(currentResolution.width, currentResolution.height, 
                Screen.fullScreenMode, refreshRate);
        }
    }
    
    private void LoadSettings()
    {
        int savedMode = PlayerPrefs.GetInt("FrameRateMode", (int)defaultMode);
        currentMode = (FrameRateMode)savedMode;
        lastInputTime = Time.unscaledTime;
    }
    
    private void SaveSettings()
    {
        PlayerPrefs.SetInt("FrameRateMode", (int)currentMode);
        PlayerPrefs.Save();
    }
    
    public FrameRateMode GetCurrentMode() => currentMode;
    public int GetCurrentFPS() => Application.targetFrameRate;
    public bool IsIdle() => isIdle;
}