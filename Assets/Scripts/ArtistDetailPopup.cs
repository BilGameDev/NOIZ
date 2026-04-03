using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ArtistDetailPopup : PopupPanel
{
    [Header("Artist Details UI")]
    [SerializeField] NOIZSettings settings;
    public Image artistImage;
    public TextMeshProUGUI artistDetailText;
    public Transform tracksContainer;
    public GameObject trackItemPrefab;
    public Button playButton;
    
    [Header("Track Settings")]
    public int maxTracks = 5;

    private Coroutine currentCoroutine;
    private List<DeezerTrack> currentTracks = new List<DeezerTrack>();
    private ArtistDetails currentArtist;
    private DeezerTrack selectedTrack;
    private List<TrackItem> trackItems = new List<TrackItem>();

    protected override void Awake()
    {
        base.Awake();
        playButton.onClick.AddListener(OnPlayArtist);
    }

    private void OnPlayArtist()
    {
        if (currentTracks != null && currentTracks.Count > 0)
        {
            settings.currentTracks = currentTracks;
            NOIZEventHandler.GoToGameScene();
        }
        else
        {
            Debug.LogWarning("No tracks available to play for this artist.");
        }
    }

    public void Setup(ArtistData artist)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(LoadArtistDetails(artist));
    }

    IEnumerator LoadArtistDetails(ArtistData artist)
    {
        bool detailsLoaded = false;
        bool tracksLoaded = false;
        ArtistDetails details = null;
        List<DeezerTrack> tracks = null;

        // Load artist details
        StartCoroutine(DeezerAPI.Instance.GetArtistDetails(artist.id, (d) =>
        {
            details = d;
            detailsLoaded = true;
        }, (error) =>
        {
            Debug.LogError($"Failed to load details: {error}");
            detailsLoaded = true;
        }));

        // Load top tracks
        StartCoroutine(DeezerAPI.Instance.GetArtistTopTracks(artist.id, (t) =>
        {
            tracks = t;
            tracksLoaded = true;
        }, (error) =>
        {
            Debug.LogError($"Failed to load tracks: {error}");
            tracksLoaded = true;
        }));

        // Wait for both to load
        yield return new WaitUntil(() => detailsLoaded && tracksLoaded);

        if (details != null && tracks != null)
        {
            currentArtist = details;
            currentTracks = tracks.Take(maxTracks).ToList();
            ShowArtistDetails(details, currentTracks);
        }
        else
        {
            Debug.LogError("Failed to load artist details or tracks");
        }
    }

    void ShowArtistDetails(ArtistDetails artist, List<DeezerTrack> tracks)
    {
        // Format the artist details text with HTML-like tags for TMP
        string formattedText = $"<b><size=36>{artist.name}</size></b>\n\n" +
                               $"Fans: {FormatNumber(artist.nb_fan)}\n" +
                               $"Albums: {artist.nb_album}";
        
        artistDetailText.text = formattedText;

        // Load artist image
        if (!string.IsNullOrEmpty(artist.picture_big))
        {
            StartCoroutine(LoadImage(artist.picture_big, artistImage));
        }

        // Clear existing track items
        trackItems.Clear();
        foreach (Transform child in tracksContainer)
        {
            Destroy(child.gameObject);
        }

        // Populate tracks
        if (tracks != null && tracks.Count > 0)
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                GameObject trackItem = Instantiate(trackItemPrefab, tracksContainer);
                trackItem.SetActive(true);
                
                TrackItem item = trackItem.GetComponent<TrackItem>();
                if (item != null)
                {
                    item.Setup(track, i, OnSwapTrack);
                    trackItems.Add(item);
                }
            }
        }
        else
        {
            TextMeshProUGUI noTracksText = Instantiate(new GameObject(), tracksContainer).AddComponent<TextMeshProUGUI>();
            noTracksText.text = "No tracks available";
            noTracksText.fontSize = 14;
            noTracksText.color = Color.gray;
            noTracksText.alignment = TextAlignmentOptions.Center;
        }
    }
    private void OnSwapTrack(DeezerTrack currentTrack, int index)
    {
        StartCoroutine(SwapTrack(currentTrack, index));
    }

    IEnumerator SwapTrack(DeezerTrack currentTrack, int index)
    {
        if (currentArtist == null) yield break;
        
        // Show loading indicator on the track item
        if (trackItems[index] != null)
        {
            trackItems[index].ShowLoading(true);
        }
        
        // Get a random track from the artist's other tracks
        // We need to fetch more tracks from the API
        List<DeezerTrack> newTracks = null;
        bool tracksLoaded = false;
        
        // Fetch more tracks from the artist (limit 20)
        StartCoroutine(DeezerAPI.Instance.GetArtistTopTracks(currentArtist.id, (tracks) =>
        {
            // Filter out the current 5 tracks and get a random one
            var currentTrackIds = currentTracks.Select(t => t.id).ToHashSet();
            var availableTracks = tracks.Where(t => !currentTrackIds.Contains(t.id)).ToList();
            
            if (availableTracks.Count > 0)
            {
                // Randomly select a track from available ones
                int randomIndex = UnityEngine.Random.Range(0, availableTracks.Count);
                newTracks = new List<DeezerTrack> { availableTracks[randomIndex] };
            }
            tracksLoaded = true;
        }, (error) =>
        {
            Debug.LogError($"Failed to fetch more tracks: {error}");
            tracksLoaded = true;
        }));
        
        yield return new WaitUntil(() => tracksLoaded);
        
        if (newTracks != null && newTracks.Count > 0)
        {
            // Replace the track at the specified index
            DeezerTrack newTrack = newTracks[0];
            currentTracks[index] = newTrack;
            
            // Update the UI
            if (trackItems[index] != null)
            {
                trackItems[index].UpdateTrack(newTrack, index);
                trackItems[index].ShowLoading(false);
            }
            
        }
        else
        {
            Debug.LogWarning("No alternative tracks available to swap");
            if (trackItems[index] != null)
            {
                trackItems[index].ShowLoading(false);
            }
        }
    }

    IEnumerator LoadImage(string url, Image targetImage)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture != null && targetImage != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    targetImage.sprite = sprite;
                }
            }
            else
            {
                Debug.LogError($"Failed to load image: {request.error}");
            }
        }
    }

    private string FormatNumber(int num)
    {
        if (num >= 1000000)
            return (num / 1000000f).ToString("0.0") + "M";
        if (num >= 1000)
            return (num / 1000f).ToString("0.0") + "K";
        return num.ToString();
    }

    protected override void OnDestroy()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        playButton.onClick.RemoveListener(OnPlayArtist);
    }
}