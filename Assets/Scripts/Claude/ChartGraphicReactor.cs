using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RhythmGen
{
    /// <summary>
    /// Scales and colors a Graphic based on ChartBuilder's music analysis.
    /// Attach to any UI element (Image, RawImage, Text, etc.) with a Graphic component.
    /// Assign a ChartBuilder and call Activate() after the chart is built.
    /// </summary>
    public class ChartGraphicReactor : MonoBehaviour
    {
        [Header("References")]
        public ChartBuilder chartBuilder;
        public Graphic targetGraphic;

        [Header("Scale")]
        public float baseScale = 1f;
        public float maxScaleMultiplier = 1.5f;
        public float scaleSmoothing = 8f;

        [Header("Color")]
        public Color lowBandColor = new Color(0.3f, 0.5f, 1f);
        public Color midBandColor = new Color(0.9f, 0.3f, 0.5f);
        public Color highBandColor = new Color(1f, 0.85f, 0.2f);
        public float colorSmoothing = 6f;

        [Header("Beat Pulse")]
        public float beatPulseScale = 1.2f;
        public float beatPulseDuration = 0.12f;
        public Ease beatPulseEase = Ease.OutBack;

        ChartData chart;
        float[] beatTimes;
        int nextBeatIndex;
        double dspStart;
        float currentScale;
        Color currentColor;
        Tween beatTween;

        void Awake()
        {
            if (targetGraphic == null)
                targetGraphic = GetComponent<Graphic>();

            currentScale = baseScale;
            currentColor = targetGraphic != null ? targetGraphic.color : Color.white;
        }

        public void Activate()
        {
            if (chartBuilder == null || chartBuilder.Chart == null)
            {
                Debug.LogError("ChartGraphicReactor: ChartBuilder or chart is null.");
                return;
            }

            chart = chartBuilder.Chart;
            beatTimes = GetBeatGrid();
            nextBeatIndex = 0;
            dspStart = AudioSettings.dspTime;
        }

        void Update()
        {
            if (chart == null || targetGraphic == null) return;

            double songTime = AudioSettings.dspTime - dspStart;

            // Find the most recent note approaching or at current time
            float nearestStrength = 0f;
            int nearestBand = 0;
            float closestDist = float.MaxValue;

            for (int i = 0; i < chart.notes.Count; i++)
            {
                float dist = Mathf.Abs((float)(songTime - chart.notes[i].time));
                if (dist < closestDist && dist < 0.3f)
                {
                    closestDist = dist;
                    nearestStrength = chart.notes[i].strength;
                    nearestBand = chart.notes[i].lane;
                }
            }

            // Scale based on note strength
            float targetScale = baseScale + nearestStrength * (maxScaleMultiplier - baseScale);
            currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * scaleSmoothing);
            transform.localScale = Vector3.one * currentScale;

            // Color based on frequency band
            Color targetColor = GetBandColor(nearestBand, nearestStrength);
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorSmoothing);
            targetGraphic.color = currentColor;

            // Beat pulse
            if (beatTimes != null)
            {
                while (nextBeatIndex < beatTimes.Length && beatTimes[nextBeatIndex] <= songTime)
                {
                    PulseBeat();
                    nextBeatIndex++;
                }
            }
        }

        void PulseBeat()
        {
            beatTween?.Kill();
            transform.localScale = Vector3.one * baseScale;
            beatTween = transform.DOScale(baseScale * beatPulseScale, beatPulseDuration)
                .SetEase(beatPulseEase)
                .OnComplete(() =>
                {
                    transform.DOScale(baseScale, beatPulseDuration * 0.5f);
                });
        }

        Color GetBandColor(int band, float strength)
        {
            float t = strength;
            Color c;
            if (band <= 1)
                c = Color.Lerp(currentColor, lowBandColor, t);
            else if (band == 2)
                c = Color.Lerp(currentColor, midBandColor, t);
            else
                c = Color.Lerp(currentColor, highBandColor, t);
            return c;
        }

        float[] GetBeatGrid()
        {
            if (chart.bpm <= 0f) return null;
            float beatDur = 60f / chart.bpm;
            int count = Mathf.Max(0, Mathf.FloorToInt((chart.notes.Count > 0
                ? chart.notes[chart.notes.Count - 1].time + 2f
                : 10f) / beatDur) + 1);
            var beats = new float[count];
            for (int i = 0; i < count; i++)
                beats[i] = chart.beatOffset + i * beatDur;
            return beats;
        }

        void OnDestroy()
        {
            beatTween?.Kill();
        }
    }
}
