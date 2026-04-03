using System.Collections;
using TelePresent.AudioSyncPro;
using UnityEngine;

public static class AudioUtils
{
    public static IEnumerator RestoreRoutine(AudioSourcePlus songSource, float targetVolume = 1f, float fadeDuration = 0.1f)
    {
        AudioSource source = songSource.audioSource;
        float startVolume = source.volume;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, timer / fadeDuration);
            yield return null;
        }

        source.volume = targetVolume;
    }

    public static IEnumerator DuckRoutine(AudioSource source, float dipVolume, float dipDuration, float fadeDuration)
    {
        float originalVolume = source.volume;
        source.volume = dipVolume;
        yield return new WaitForSeconds(dipDuration);

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(dipVolume, originalVolume, timer / fadeDuration);
            yield return null;
        }

        source.volume = originalVolume;
    }
}
