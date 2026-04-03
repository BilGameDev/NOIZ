using UnityEngine;
using DG.Tweening;

public class BeatPulse : MonoBehaviour
{
    [SerializeField] Transform pulseTarget;

    [Header("Pulse Settings")]
    public float pulseScale = 1.15f;
    public float pulseDuration = 0.1f;
    public Ease pulseEase = Ease.OutBack;
    public float returnDuration = 0.05f;
    public Ease returnEase = Ease.InOutSine;
    
    private Vector3 originalScale;
    private Tween currentTween;
    
    void Start()
    {
        originalScale = pulseTarget.localScale;
    }
    
    public void Pulse()
    {
        // Kill any ongoing tweens
        currentTween?.Kill();
        
        // Scale pulse
        pulseTarget.localScale = originalScale;
        currentTween = pulseTarget.DOScale(originalScale * pulseScale, pulseDuration)
            .SetEase(pulseEase)
            .OnComplete(() =>
            {
                pulseTarget.DOScale(originalScale, returnDuration).SetEase(returnEase);
            });
    }
    
    void OnDestroy()
    {
        currentTween?.Kill();
    }
}