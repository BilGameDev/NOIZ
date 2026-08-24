using System.Collections;
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

        IEnumerator Start()
        {
            if (chartBuilder == null || audioSource == null || lanesRoot == null || hitMarker == null)
            {
                Debug.LogError("ChartPreviewPlayer: missing references.");
                yield break;
            }

            if (chartBuilder.Chart == null || chartBuilder.Chart.notes.Count == 0)
                chartBuilder.Build();

            if (chartBuilder.Chart == null || chartBuilder.Chart.notes.Count == 0)
            {
                Debug.LogError("ChartPreviewPlayer: chart empty, nothing to preview.");
                yield break;
            }

            // Wait a frame so layout (anchors, layout groups, canvas scaler etc.)
            // has actually resolved before we read rect sizes / positions off it.
            // Reading these in the same frame as instantiation/enable can give
            // you stale or zeroed values.
            yield return null;
            Canvas.ForceUpdateCanvases();

            // --- hitY: convert the hit marker's position into lanesRoot's LOCAL
            // space, regardless of what its actual parent is. anchoredPosition
            // is only meaningful relative to a transform's own parent, so we
            // can't just read hitMarker.anchoredPosition.y and use it directly
            // unless hitMarker happens to share lanesRoot's exact frame.
            //
            // IMPORTANT: the camera passed to these calls must match the
            // Canvas's render mode, or the conversion silently produces
            // garbage coordinates. Screen Space - Overlay wants `null`.
            // Screen Space - Camera / World Space want the canvas's actual
            // worldCamera. Using the wrong one is the #1 cause of notes
            // ending up positioned way outside the visible/masked area.
            var canvas = lanesRoot.GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = canvas.worldCamera;

            Vector3 hitWorld = hitMarker.position;
            Vector2 hitLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                lanesRoot,
                RectTransformUtility.WorldToScreenPoint(uiCamera, hitWorld),
                uiCamera,
                out hitLocal);
            hitY = hitLocal.y;

            Debug.Log($"ChartPreviewPlayer: canvas={canvas?.renderMode}, resolved hitY={hitY:F1}, " +
                      $"lanesRoot rect yMin={lanesRoot.rect.yMin:F1} yMax={lanesRoot.rect.yMax:F1}. " +
                      "If hitY falls outside [yMin, yMax], the marker is outside lanesRoot's own " +
                      "rect and notes will clip before reaching it.");

            // --- spawnY: use rect.yMax, which correctly accounts for pivot
            // (rect.height alone assumes a (0,0) pivot, which UI elements
            // usually aren't -- default is (0.5, 0.5)).
            float spawnY = lanesRoot.rect.yMax - 40f;

            pixelsPerSecond = (spawnY - hitY) / approachTime;
            if (pixelsPerSecond <= 0f)
                Debug.LogWarning("ChartPreviewPlayer: computed pixelsPerSecond <= 0 -- " +
                                  "hit marker is not below the spawn point in lanesRoot's local space. " +
                                  "Check that hitMarker is actually positioned lower on screen than the spawn area.");

            audioSource.clip = chartBuilder.song;
            audioSource.Play();
            dspStart = AudioSettings.dspTime;
            playing = true;
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

                if (delta < -4)
                {
                    Destroy(an.rt.gameObject);
                    activeNotes.RemoveAt(i);
                    continue;
                }

                float y = hitY + (float)delta * pixelsPerSecond;
                float laneW = lanesRoot.rect.width / Mathf.Max(1, chart.laneCount);
                float x = lanesRoot.rect.xMin + laneW * (an.note.lane + 0.5f);
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
