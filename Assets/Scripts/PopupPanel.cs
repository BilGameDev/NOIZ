using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PopupPanel : MonoBehaviour
{
    [SerializeField] protected RectTransform mainPanel;
    [SerializeField] protected Button backButton;

    protected Vector3 mainCurrentPosition;
    protected Vector3 mainCurrentScale;

    protected virtual void Awake()
    {
        mainCurrentPosition = mainPanel.anchoredPosition;
        mainCurrentScale = mainPanel.localScale;
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
        mainPanel.anchoredPosition = new Vector2(mainPanel.anchoredPosition.x, -3000);
        mainPanel.DOAnchorPos(mainCurrentPosition, .2f).SetEase(Ease.OutCubic);
    }

    public virtual void ClosePanel()
    {
        NOIZEventHandler.ClosePopup();
        mainPanel.DOAnchorPosY(-3000, .2f).SetEase(Ease.InCubic).OnComplete(() => Destroy(gameObject));
    }
}
