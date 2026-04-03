using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class ScoreVisual : MonoBehaviour
{
    [SerializeField] private CanvasGroup comboCanvasGroup;
    [SerializeField] private CanvasGroup scoreCanvasGroup;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;

    [Header("Animation Settings")]
    public float comboBounceScale = 1.3f;
    public float comboBounceDuration = 0.2f;
    public float scoreBounceScale = 1.3f;
    public float scoreBounceDuration = 0.2f;
    public float fadeDelay = 0.5f;
    public float fadeDuration = 0.4f;

    private Tween comboScaleTween;
    private Tween comboFadeTween;
    private Tween scoreScaleTween;

    void OnEnable()
    {
        ScoreHandler.OnScoreChanged += HandleScoreChanged;
        ScoreHandler.OnComboChanged += HandleComboChanged;
        NOIZEventHandler.OnMissNote += HandleMiss;
    }


    void OnDisable()
    {
        ScoreHandler.OnScoreChanged -= HandleScoreChanged;
        ScoreHandler.OnComboChanged -= HandleComboChanged;
        NOIZEventHandler.OnMissNote -= HandleMiss;
    }

    private void HandleScoreChanged(float newScore)
    {
        ShowScore($"Score :{Mathf.RoundToInt(newScore)}");
    }

    private void HandleComboChanged(float newCombo)
    {
        if (newCombo <= 0)
        {
            ShowCombo($"Incorrect");
            return;
        }

        ShowCombo($"+{Mathf.RoundToInt(newCombo)}");
    }

    private void HandleMiss()
    {
        ShowScore($"Miss -{NOIZManager.Settings.minusScorePerMiss}");
    }


    public void ShowScore(string text)
    {
        // Reset state
        scoreText.text = text;

        // // Bounce effect
        // scoreCanvasGroup.transform.localScale = Vector3.one * 0.9f;
        // scoreScaleTween = scoreCanvasGroup.transform.DOScale(scoreBounceScale, scoreBounceDuration)
        //     .SetEase(Ease.OutBack)
        //     .OnComplete(() =>
        //     {
        //         scoreCanvasGroup.transform.DOScale(1f, scoreBounceDuration).SetEase(Ease.InOutSine);
        //     });
    }

    public void ShowCombo(string text)
    {
         // Reset state
        comboText.text = text;
        //scoreText.color = color;
        comboCanvasGroup.alpha = 1f;
        comboCanvasGroup.transform.localScale = Vector3.one;

        // Kill existing tweens if any
        comboScaleTween?.Kill();
        comboFadeTween?.Kill();

        // Bounce effect
        comboCanvasGroup.transform.localScale = Vector3.one * 0.9f;
        comboScaleTween = comboCanvasGroup.transform.DOScale(comboBounceScale, comboBounceDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                comboCanvasGroup.transform.DOScale(1f, comboBounceDuration).SetEase(Ease.InOutSine);
            });

        // Fade out after delay
        comboFadeTween = comboCanvasGroup.DOFade(0f, fadeDuration)
            .SetDelay(fadeDelay)
            .SetEase(Ease.OutQuad);
    }
}
