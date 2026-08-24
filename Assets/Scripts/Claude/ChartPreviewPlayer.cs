using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmGen
{
    /// <summary>
    /// Quick-and-dirty chart tester: builds the chart, plays the song,
    /// and scrolls spawned notes down the lanes synced to dspTime.
    /// Notes are purely visual (no scoring/input) so you can eyeball
    /// whether the chart feels hand-made.
    /// </summary>
    public class ChartPreviewPlayer : MonoBehaviour
    {
        public ChartBuilder chartBuilder;
        public AudioSource audioSource;
        public RectTransform lanesRoot;
        public RectTransform hitMarker;
        public float approachTime = 2f;

        public Color[] laneColors =
        {
            new Color(0.95f, 0.35f, 0.45f),
            new Color(0.35f, 0.85f, 0.95f),
            new Color(0.55f, 0.9f, 0.45f),
            new Color(0.95f, 0.75f, 0.35f),
        };

        struct ActiveNote
        {
            public RectTransform rt;
            public NoteData note;
        }

        readonly List<ActiveNote> activeNotes = new List<ActiveNote>(256);
        int nextIndex;
        double dspStart;
        bool playing;
        float pixelsPerSecond;
        float hitY;

        void Start()
        {
            if (chartBuilder == null || audioSource == null || lanesRoot == null || hitMarker == null)
            {
                Debug.LogError("ChartPreviewPlayer: missing references.");
                return;
            }

            if (chartBuilder.Chart == null || chartBuilder.Chart.notes.Count == 0)
                chartBuilder.Build();

            if (chartBuilder.Chart == null || chartBuilder.Chart.notes.Count == 0)
            {
                Debug.LogError("ChartPreviewPlayer: chart empty, nothing to preview.");
                return;
            }

            audioSource.clip = chartBuilder.song;
            audioSource.Play();
            dspStart = AudioSettings.dspTime;
            playing = true;

            hitY = hitMarker.anchoredPosition.y;
            float spawnY = lanesRoot.rect.height - 40f;
            pixelsPerSecond = (spawnY - hitY) / approachTime;
        }

        void Update()
        {
            if (!playing) return;

            var chart = chartBuilder.Chart;
            double songTime = AudioSettings.dspTime - dspStart;

            while (nextIndex < chart.notes.Count &&
                   chart.notes[nextIndex].time <= songTime + approachTime)
            {
                Spawn(chart.notes[nextIndex]);
                nextIndex++;
            }

            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                var an = activeNotes[i];
                double delta = an.note.time - songTime;

                if (delta < -0.75)
                {
                    Destroy(an.rt.gameObject);
                    activeNotes.RemoveAt(i);
                    continue;
                }

                float y = hitY + (float)delta * pixelsPerSecond;
                float laneW = lanesRoot.rect.width / Mathf.Max(1, chart.laneCount);
                float x = -lanesRoot.rect.width * 0.5f + laneW * (an.note.lane + 0.5f);
                an.rt.anchoredPosition = new Vector2(x, y);
            }
        }

        void Spawn(NoteData n)
        {
            var go = new GameObject("Note", typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(lanesRoot, false);

            float laneW = lanesRoot.rect.width / Mathf.Max(1, chartBuilder.Chart.laneCount);
            float w = laneW * 0.72f;
            float h = n.type == NoteType.Hold
                ? Mathf.Max(30f, n.holdDuration * pixelsPerSecond)
                : 30f;

            rt.sizeDelta = new Vector2(w, h);
            rt.pivot = new Vector2(0.5f, 0f);

            var img = go.GetComponent<Image>();
            img.color = laneColors[Mathf.Clamp(n.lane, 0, laneColors.Length - 1)];
            if (n.type == NoteType.Hold)
                img.color *= 0.8f;

            activeNotes.Add(new ActiveNote { rt = rt, note = n });
        }
    }
}
