using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FireflyController : MonoBehaviour
{
    [SerializeField] RectTransform[] glowTextures;
    [SerializeField] SimpleBeatDetection simpleBeatDetection;

    [Header("Movement")]
    public float moveDuration = 4f;
    public Ease moveEase = Ease.InOutSine;

    [Header("Fade")]
    public float minAlpha = 0f;
    public float maxAlpha = 0.8f;
    public float fadeDuration = 2.5f;

    [Header("Color")]
    public Color[] palette = new Color[]
    {
        new Color(0.9f, 0.2f, 0.4f),
        new Color(1f, 0.5f, 0.1f),
        new Color(1f, 0.95f, 0.3f),
        new Color(0.3f, 0.9f, 0.7f),
        new Color(0.4f, 0.6f, 1f),
        new Color(0.7f, 0.3f, 1f),
    };
    public float colorCycleDuration = 5f;
    public Ease colorEase = Ease.InOutSine;

    [Header("Beat Sync (slower)")]
    public int beatInterval = 4;
    public float beatBrightnessBoost = 0.3f;
    public float beatBoostDuration = 0.15f;
    public Color beatFlashColor = Color.white;
    public float beatFlashDuration = 0.1f;

    private int beatCount;
    private FireflyData[] fireflies;

    struct FireflyData
    {
        public Graphic graphic;
        public CanvasGroup canvasGroup;
        public Sequence moveSequence;
        public Tween fadeTween;
        public Tween colorTween;
    }

    void OnEnable()
    {
        simpleBeatDetection.OnBeat += OnBeat;
    }

    void OnDisable()
    {
        simpleBeatDetection.OnBeat -= OnBeat;
    }

    void Start()
    {
        RectTransform canvas = glowTextures[0].root as RectTransform;
        float canvasW = canvas.rect.width;
        float canvasH = canvas.rect.height;

        fireflies = new FireflyData[glowTextures.Length];

        for (int i = 0; i < glowTextures.Length; i++)
        {
            RectTransform rt = glowTextures[i];
            CanvasGroup cg = rt.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = rt.gameObject.AddComponent<CanvasGroup>();

            Graphic graphic = rt.GetComponent<Graphic>();

            float delay = i * 0.7f;

            FireflyData fd = new FireflyData();
            fd.graphic = graphic;
            fd.canvasGroup = cg;
            fd.fadeTween = CreateFadeLoop(cg, delay);
            fd.moveSequence = CreateMoveLoop(rt, canvasW, canvasH, delay);
            if (graphic != null)
                fd.colorTween = CreateColorLoop(graphic, delay);
            fireflies[i] = fd;
        }
    }

    Sequence CreateMoveLoop(RectTransform rt, float w, float h, float delay)
    {
        Sequence seq = DOTween.Sequence();
        seq.SetDelay(delay);
        seq.SetLoops(-1);

        for (int j = 0; j < 4; j++)
        {
            float randX = Random.Range(-w * 0.4f, w * 0.4f);
            float randY = Random.Range(-h * 0.4f, h * 0.4f);
            float dur = Random.Range(moveDuration * 0.7f, moveDuration * 1.3f);
            seq.Append(rt.DOAnchorPos(new Vector2(randX, randY), dur).SetEase(moveEase));
        }

        return seq;
    }

    Tween CreateFadeLoop(CanvasGroup cg, float delay)
    {
        Sequence seq = DOTween.Sequence();
        seq.SetDelay(delay);
        seq.Append(cg.DOFade(maxAlpha, fadeDuration));
        seq.Append(cg.DOFade(minAlpha, fadeDuration));
        seq.SetLoops(-1);
        return seq;
    }

    Tween CreateColorLoop(Graphic graphic, float delay)
    {
        Sequence seq = DOTween.Sequence();
        seq.SetDelay(delay);

        int count = Mathf.Max(palette.Length, 2);
        for (int j = 0; j < count; j++)
        {
            Color target = palette[Random.Range(0, palette.Length)];
            float dur = Random.Range(colorCycleDuration * 0.7f, colorCycleDuration * 1.3f);
            seq.Append(graphic.DOColor(target, dur).SetEase(colorEase));
        }

        seq.SetLoops(-1);
        return seq;
    }

    void OnBeat()
    {
        beatCount++;
        if (beatCount % beatInterval != 0)
            return;

        for (int i = 0; i < fireflies.Length; i++)
        {
            int index = i;
            CanvasGroup cg = fireflies[index].canvasGroup;
            float target = Mathf.Min(cg.alpha + beatBrightnessBoost, 1f);

            cg.DOKill();
            cg.DOFade(target, beatBoostDuration)
                .OnComplete(() =>
                {
                    if (index >= fireflies.Length) return;
                    fireflies[index].fadeTween.Kill();
                    fireflies[index].fadeTween = CreateFadeLoop(cg, 0f);
                });

            Graphic graphic = fireflies[index].graphic;
            if (graphic == null) continue;

            graphic.DOKill();
            graphic.DOColor(beatFlashColor, beatFlashDuration)
                .OnComplete(() =>
                {
                    if (index >= fireflies.Length) return;
                    fireflies[index].colorTween.Kill();
                    fireflies[index].colorTween = CreateColorLoop(graphic, 0f);
                });
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < fireflies.Length; i++)
        {
            fireflies[i].moveSequence?.Kill();
            fireflies[i].fadeTween?.Kill();
            fireflies[i].colorTween?.Kill();
        }
    }
}
