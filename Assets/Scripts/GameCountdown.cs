using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class GameCountdown : MonoBehaviour
{
    public CanvasGroup countDownCanvas;
    public TMP_Text preCountdownText; // shows 3,2,1,GO

    private Coroutine preCountdownCo;

    void Start()
    {
        StartCounddown();
    }

    void OnDestroy()
    {
        // Cancel any running animations / coroutines
        if (preCountdownCo != null)
        {
            StopCoroutine(preCountdownCo);
            preCountdownCo = null;
        }
        StopAllCoroutines();
    }

    private void StartCounddown()
    {
        // Defensive guard – ignore if object is disabled/destroying
        if (!this || !isActiveAndEnabled) return;

        // If something was running, stop it cleanly
        if (preCountdownCo != null)
        {
            StopCoroutine(preCountdownCo);
            preCountdownCo = null;
        }
        StopAllCoroutines();

        preCountdownCo = StartCoroutine(StartWithPreCountdown());
    }

    private IEnumerator StartWithPreCountdown()
    {
        if (countDownCanvas)
        {
            countDownCanvas.alpha = 1f;
            countDownCanvas.blocksRaycasts = true;
        }
        if (preCountdownText) preCountdownText.gameObject.SetActive(true);

        yield return AnimatePreCountdown("Get Ready!");

        for (int i = 3; i > 0; i--)
            yield return AnimatePreCountdown(i.ToString());

        NOIZEventHandler.GameStart();

        if (preCountdownText) preCountdownText.gameObject.SetActive(false);
        if (countDownCanvas)
        {
            countDownCanvas.DOFade(0f, 1f);
            countDownCanvas.blocksRaycasts = false;
        }

        preCountdownCo = null;
    }

    private IEnumerator AnimatePreCountdown(string text)
    {
        if (preCountdownText)
        {
            preCountdownText.text = text;
            preCountdownText.color = new Color(1, 1, 1, 0);
            preCountdownText.transform.localScale = Vector3.zero;

            preCountdownText.DOFade(1f, 0.4f);
            preCountdownText.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }

        yield return new WaitForSeconds(0.8f);

        if (preCountdownText)
        {
            preCountdownText.DOFade(0f, 0.2f);
            preCountdownText.transform.DOScale(1.3f, 0.2f);
        }

        yield return new WaitForSeconds(0.3f);
    }
}