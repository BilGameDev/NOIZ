using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PopupPanel : MonoBehaviour
{
    [SerializeField] protected RectTransform mainPanel;
    [SerializeField] protected Button backButton;

    protected Vector3 mainCurrentPosition;
    protected Vector3 mainCurrentScale;
    CanvasGroup canvasGroup;

    protected virtual void Awake()
    {
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        //mainCurrentPosition = mainPanel.anchoredPosition;
        mainCurrentScale = mainPanel.localScale;
        mainPanel.localScale = Vector3.zero;
        backButton.onClick.AddListener(ClosePanel);

        OpenPanel();
    }

    protected virtual void OnDestroy()
    {
        backButton.onClick.RemoveListener(ClosePanel);
    }

    public virtual void OpenPanel()
    {
        mainPanel.gameObject.SetActive(true);
        mainPanel.DOScale(1, .5f).SetEase(Ease.OutQuad);
        //mainPanel.anchoredPosition = new Vector2(mainPanel.anchoredPosition.x, -3000);
        //mainPanel.DOAnchorPos(mainCurrentPosition, .2f).SetEase(Ease.OutCubic);
        canvasGroup.DOFade(1, 1);
    }

    public virtual void ClosePanel()
    {
        NOIZEventHandler.ClosePopup();
        canvasGroup.DOFade(1, 0).OnComplete(() => Destroy(gameObject));
        //mainPanel.DOAnchorPosY(-3000, .2f).SetEase(Ease.InCubic).OnComplete(() => Destroy(gameObject));
    }
}
