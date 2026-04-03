using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ColorExtractor : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI artistText;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] Material sceneMaterial;
    [SerializeField] Material skyMaterial;
    [SerializeField] Material baseMaterial;
    public int colorCount = 3;

    [Range(0, 1)]
    public float darkerAmount;

    [ContextMenu("Extract Colors")]
    public void ExtractPalette(Texture2D texture2D)
    {
        if (texture2D == null) return;

        Color[] pixels = texture2D.GetPixels();
        Dictionary<Color, int> colorFrequency = new Dictionary<Color, int>();

        foreach (Color pixel in pixels)
        {
            Color rounded = RoundColor(pixel, 0.05f); // reduces variation
            if (colorFrequency.ContainsKey(rounded))
                colorFrequency[rounded]++;
            else
                colorFrequency[rounded] = 1;
        }

        // Sort by frequency first, then by darkness (low luminance)
        var sorted = colorFrequency
            .OrderByDescending(kv => kv.Value) // frequent first
            .ThenBy(kv => GetLuminance(kv.Key)) // but prefer darker within frequency
            .Take(colorCount)
            .Select(kv => kv.Key)
            .ToList();

        StartCoroutine(TransitionSkyColors(sorted, 1f)); // 1 second transition
    }

    IEnumerator TransitionSkyColors(List<Color> targetColors, float duration)
    {
        Color topStart = skyMaterial.GetColor("_TopColor");
        Color midStart = skyMaterial.GetColor("_MiddleColor");
        Color botStart = skyMaterial.GetColor("_BottomColor");

        Color topEnd = targetColors[0];
        Color midEnd = targetColors[1];
        Color botEnd = targetColors[2];

        Color midInverted = InvertColor(midEnd);
        Color darkerBase = MakeDarker(midEnd);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            skyMaterial.SetColor("_TopColor", Color.Lerp(topStart, topEnd, t));
            skyMaterial.SetColor("_MiddleColor", Color.Lerp(midStart, midEnd, t));
            skyMaterial.SetColor("_BottomColor", Color.Lerp(botStart, botEnd, t));

            baseMaterial.SetColor("_Color", Color.Lerp(midStart, darkerBase, t));
            sceneMaterial.SetColor("_Color", Color.Lerp(midStart, midInverted, t));

            titleText.color = Color.Lerp(midStart, midInverted, t);
            artistText.color = Color.Lerp(midStart, midInverted, t);
            scoreText.color = Color.Lerp(midStart, midInverted, t);

            yield return null;
        }

        // Ensure final values are exact
        skyMaterial.SetColor("_TopColor", midEnd);
        skyMaterial.SetColor("_MiddleColor", midEnd);
        skyMaterial.SetColor("_BottomColor", botEnd);

        baseMaterial.SetColor("_Color", darkerBase);
        sceneMaterial.SetColor("_Color", midInverted);

        titleText.color = midInverted;
        artistText.color = midInverted;
        scoreText.color = midInverted;
    }

    Color InvertColor(Color color)
    {
        return new Color(1f - color.r, 1f - color.g, 1f - color.b, color.a);
    }

    Color MakeDarker(Color color)
    {
        float factor = 1f - darkerAmount; // e.g., 0.7 means 30% darker
        return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
    }

    float GetLuminance(Color c)
    {
        // Perceived brightness formula (ITU-R BT.709)
        return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    }

    Color RoundColor(Color c, float step)
    {
        float r = Mathf.Round(c.r / step) * step;
        float g = Mathf.Round(c.g / step) * step;
        float b = Mathf.Round(c.b / step) * step;
        return new Color(r, g, b);
    }
}
