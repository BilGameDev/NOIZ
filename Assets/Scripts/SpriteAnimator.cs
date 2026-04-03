using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpriteAnimator : MonoBehaviour
{
    public Image img;
    public Sprite[] frames;
    public float fps = 12f;
    public bool loop = true;

    Coroutine _co;

    void OnEnable()
    {
        if (img == null) img = GetComponent<Image>();
        Play();
    }

    public void Play()
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        if (frames == null || frames.Length == 0) yield break;

        float delay = 1f / Mathf.Max(1f, fps);
        int i = 0;

        while (true)
        {
            img.sprite = frames[i];
            i++;

            if (i >= frames.Length)
            {
                if (loop) i = 0;
                else break;
            }

            yield return new WaitForSeconds(delay);
        }
    }

    void OnDisable()
    {
        if (_co != null) StopCoroutine(_co);
        _co = null;
    }
}
