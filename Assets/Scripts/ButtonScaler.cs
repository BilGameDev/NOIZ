using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to any UI Button or 3D object.
/// Scales up on pointer/mouse/touch down, scales back on release.
/// </summary>
public class ButtonScaler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    [SerializeField] private float pressedScale = .9f;
    [SerializeField] private float duration = 0.1f;
    [SerializeField] private Ease ease = Ease.OutQuad;
    [SerializeField] private bool originalScaleOne = true;

    private Vector3 originalScale;
    private Button button;

    void Awake()
    {
        originalScale = originalScaleOne ? Vector3.one : transform.localScale;
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button && !button.interactable) return;
        AnimateTo(originalScale * pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateTo(originalScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // if pointer leaves without releasing (drag away), reset
        AnimateTo(originalScale);
    }

    // For 3D clicks (mouse down / touch world collider)
    void OnMouseDown() => AnimateTo(originalScale * pressedScale);
    void OnMouseUp() => AnimateTo(originalScale);

    private void AnimateTo(Vector3 target)
    {
        transform.DOScale(target, duration)
                 .SetEase(ease);
    }

    public void StopAll()
    {
        DOTween.KillAll(this);
    }

    void OnDisable() => StopAll();
}
