using UnityEngine;
using DG.Tweening;
using static SwipeDirectionHelper;

public class NoteVisual : MonoBehaviour
{
    [SerializeField] private Transform arrowGraphic;
    [SerializeField] private Renderer noteRenderer;

    [Header("Wrong Direction Feedback")]
    [SerializeField] private Color wrongDirectionFlashColor = Color.red;
    [SerializeField] private float wrongDirectionFlashDuration = 0.15f;

    private Material arrowMaterial;
    private Color originalArrowColor;

    private void Start()
    {
        // Cache materials and colors
        arrowMaterial = noteRenderer.material;
        originalArrowColor = arrowMaterial.color;
    }

    public void SetDirection(CutDirection dir)
    {
        float z = 0f;

        switch (dir)
        {
            case CutDirection.Up: z = 0f; break;
            case CutDirection.Left: z = -90f; break;
            case CutDirection.Down: z = 180f; break;
            case CutDirection.Right: z = 90f; break;
        }

        arrowGraphic.localRotation = Quaternion.Euler(0f, 0f, z);
    }

    public void PlayWrongDirectionFlash()
    {
        // Flash the arrow
        if (arrowMaterial != null)
        {
            arrowMaterial.DOKill();
            Sequence arrowFlash = DOTween.Sequence();
            arrowFlash.Append(arrowMaterial.DOColor(wrongDirectionFlashColor, wrongDirectionFlashDuration / 2));
            arrowFlash.Append(arrowMaterial.DOColor(originalArrowColor, wrongDirectionFlashDuration / 2));
            arrowFlash.Play();
        }
    }
}