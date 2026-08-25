using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class NOIZAudioManager : MonoBehaviour
{
    public static NOIZAudioManager Instance { get; private set; }

    [System.Serializable]
    public class ClipGroup
    {
        public string key;
        public AudioClip[] clips;
    }

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private AudioClip sfxButton;
    [SerializeField] private ClipGroup[] clipGroups;

    private Dictionary<string, AudioClip[]> clipLookup;
    private Tween humVolumeTween;
    private Tween humPitchTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);


        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        clipLookup = new Dictionary<string, AudioClip[]>();
        if (clipGroups != null)
        {
            foreach (var group in clipGroups)
            {
                if (!string.IsNullOrEmpty(group.key) && group.clips != null && group.clips.Length > 0)
                    clipLookup[group.key] = group.clips;
            }
        }
    }

    public void PlayButton()
    {
        sfxSource.PlayOneShot(sfxButton, 1);
    }

    public void PlaySFX(string key, float volumeScale = 1f)
    {
        if (string.IsNullOrEmpty(key) || clipLookup == null || !clipLookup.TryGetValue(key, out var clips))
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
            sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    private void OnDestroy()
    {
        humVolumeTween?.Kill();
        humPitchTween?.Kill();
    }
}
