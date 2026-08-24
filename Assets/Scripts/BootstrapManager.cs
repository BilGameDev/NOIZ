using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class BootstrapManager : MonoBehaviour
{
    [SerializeField] RectTransform beatTexture;
    [SerializeField] SimpleBeatDetection simpleBeatDetection;

    [Header("Beat Scale Settings")]
    public float pulseScale = 1.2f;
    public float pulseDuration = 0.1f;
    public Ease pulseEase = Ease.OutBack;
    public float returnDuration = 0.05f;
    public Ease returnEase = Ease.InOutSine;

    private Vector3 originalScale;
    private Tween currentTween;

    void Start()
    {
        originalScale = beatTexture.localScale;
    }

    void OnEnable()
    {
        simpleBeatDetection.OnBeat += MyCallbackEventHandler;
    }

    void OnDisable()
    {
        simpleBeatDetection.OnBeat -= MyCallbackEventHandler;
    }

    private void MyCallbackEventHandler()
    {
        currentTween?.Kill();
        beatTexture.localScale = originalScale;
        currentTween = beatTexture.DOScale(originalScale * pulseScale, pulseDuration)
            .SetEase(pulseEase)
            .OnComplete(() =>
            {
                beatTexture.DOScale(originalScale, returnDuration).SetEase(returnEase);
            });
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(2f);
        NOIZEventHandler.GoToMainScene();
    }
}
