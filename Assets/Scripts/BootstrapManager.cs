using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class BootstrapManager : MonoBehaviour
{
    [SerializeField] RectTransform beatTexture;
    [SerializeField] SimpleBeatDetection simpleBeatDetection;
    [SerializeField] TopArtistsManager topArtistsManager;
    [SerializeField] CanvasGroup disclaimar;

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
        topArtistsManager.GetComponent<CanvasGroup>().alpha = 0;
        originalScale = beatTexture.localScale;
        StartCoroutine(DelayedStart());
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
        beatTexture.DOLocalMove(new Vector3(-800, 450, 0), 1).SetEase(Ease.InOutSine);
        beatTexture.DOSizeDelta(new Vector2(300,300), 1).SetEase(Ease.InOutSine);
        disclaimar.DOFade(0, 1).OnComplete(()=> disclaimar.gameObject.SetActive(false));
        topArtistsManager.GetComponent<CanvasGroup>().DOFade(1, 1);
        yield return new WaitForSeconds(2f);
        topArtistsManager.StartGame();
        //NOIZEventHandler.GoToMainScene();
    }
}
