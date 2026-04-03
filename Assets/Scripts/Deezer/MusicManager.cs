using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    public int tracksToPlay = 5;
    public int maxTracks = 10;

    [SerializeField] private RawImage albumArt;
    [SerializeField] private TextMeshProUGUI trackTitle;
    [SerializeField] private TextMeshProUGUI artistNameText;
    [SerializeField] private ColorExtractor colorExtractor;

    private List<DeezerTrack> currentPlaylist = new List<DeezerTrack>();
    private int currentTrackIndex = 0;
    private bool isPlayingCustomPlaylist = false;

    void OnEnable()
    {
        NOIZEventHandler.OnGameStart += BeginGame;
    }

    void OnDisable()
    {
        NOIZEventHandler.OnGameStart -= BeginGame;
    }

    void BeginGame()
    {
        // Check if we should use saved playlist
        if (NOIZManager.Settings.currentTracks != null && NOIZManager.Settings.currentTracks.Count > 0)
        {
            PlaySavedPlaylist();
        }
        else
        {
            // Fall back to artist search
            StartCoroutine(PlayTracksSequentially(NOIZManager.Settings.artistName));
        }
    }
    
    public void PlaySavedPlaylist()
    {
        if (NOIZManager.Settings.currentTracks == null || NOIZManager.Settings.currentTracks.Count == 0)
        {
            Debug.LogWarning("No saved tracks found. Loading from API instead.");
            StartCoroutine(PlayTracksSequentially(NOIZManager.Settings.artistName));
            return;
        }
        
        isPlayingCustomPlaylist = true;
        currentPlaylist = new List<DeezerTrack>(NOIZManager.Settings.currentTracks);
        currentTrackIndex = 0;
        
        StartCoroutine(PlayPlaylistSequentially());
    }
    
    public void ShuffleAndPlayPlaylist()
    {
        if (NOIZManager.Settings.currentTracks == null || NOIZManager.Settings.currentTracks.Count == 0)
        {
            Debug.LogWarning("No saved tracks found to shuffle.");
            return;
        }
        
        isPlayingCustomPlaylist = true;
        currentPlaylist = new List<DeezerTrack>(NOIZManager.Settings.currentTracks);
        ShufflePlaylist();
        currentTrackIndex = 0;
        
        StartCoroutine(PlayPlaylistSequentially());
    }
    
    private void ShufflePlaylist()
    {
        System.Random rng = new System.Random();
        currentPlaylist = currentPlaylist.OrderBy(x => rng.Next()).ToList();
        Debug.Log($"Playlist shuffled: {currentPlaylist.Count} tracks");
    }
    
    public void PlaySpecificTrack(int index)
    {
        if (index >= 0 && index < currentPlaylist.Count)
        {
            currentTrackIndex = index;
            StartCoroutine(PlaySingleTrack(currentPlaylist[currentTrackIndex]));
        }
    }
    
    public void NextTrack()
    {
        if (currentTrackIndex + 1 < currentPlaylist.Count)
        {
            currentTrackIndex++;
            StartCoroutine(PlaySingleTrack(currentPlaylist[currentTrackIndex]));
        }
        else
        {
            Debug.Log("End of playlist reached");
            isPlayingCustomPlaylist = false;
        }
    }
    
    public void PreviousTrack()
    {
        if (currentTrackIndex - 1 >= 0)
        {
            currentTrackIndex--;
            StartCoroutine(PlaySingleTrack(currentPlaylist[currentTrackIndex]));
        }
    }
    
    IEnumerator PlayPlaylistSequentially()
    {
        while (currentTrackIndex < currentPlaylist.Count)
        {
            var track = currentPlaylist[currentTrackIndex];
            
            Debug.Log($"🎵 Playing track {currentTrackIndex + 1}/{currentPlaylist.Count}: {track.title}");
            
            yield return StartCoroutine(LoadAndPlayTrack(track));
            
            currentTrackIndex++;
        }
        
        Debug.Log("✅ Finished playing all tracks");
        isPlayingCustomPlaylist = false;
        
        // Optional: Go back to intro or repeat
        NOIZEventHandler.GoToMainScene();;
    }
    
    IEnumerator PlaySingleTrack(DeezerTrack track)
    {
        AudioClip clip = null;
        yield return TryStreamPreview(track.preview, result => clip = result);
        
        if (clip != null)
        {
            // Update visuals
            trackTitle.text = track.title;
            artistNameText.text = track.artist.name;
            StartCoroutine(LoadAlbumCover(track.album.cover_medium));
            
            // Send to game manager
            NOIZEventHandler.TrackReady(clip);
            
            // Wait until song ends
            yield return new WaitUntil(() => !BeatManager.isSongPlaying);
        }
        else
        {
            Debug.LogWarning($"❌ Failed to load track: {track.title}");
        }
    }
    
    IEnumerator LoadAndPlayTrack(DeezerTrack track)
    {
        AudioClip clip = null;
        yield return TryStreamPreview(track.preview, result => clip = result);
        
        if (clip != null)
        {
            // Update visuals
            trackTitle.text = track.title;
            artistNameText.text = track.artist.name;
            StartCoroutine(LoadAlbumCover(track.album.cover_medium));
            
            // Send to game manager
            NOIZEventHandler.TrackReady(clip);
            
            // Wait until song ends
            yield return new WaitUntil(() => !BeatManager.isSongPlaying);
        }
        else
        {
            Debug.LogWarning($"❌ Failed to load track: {track.title}");
        }
    }

    IEnumerator PlayTracksSequentially(string artist)
    {
        string query = UnityWebRequest.EscapeURL(artist);
        string url = $"https://api.deezer.com/search?q={query}&limit={maxTracks}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Deezer request failed: " + request.error);
                yield break;
            }

            string json = request.downloadHandler.text;
            DeezerChartResponse trackList = JsonUtility.FromJson<DeezerChartResponse>(json);

            if (trackList.data == null || trackList.data.Length == 0)
            {
                NOIZEventHandler.GoToMainScene();
                Debug.LogWarning("No tracks found for artist: " + artist);
                yield break;
            }

            DeezerTrack[] tracks = trackList.data;
            Shuffle(tracks);

            int playedCount = 0;

            foreach (var track in tracks)
            {
                if (playedCount >= tracksToPlay)
                    break;

                Debug.Log($"🎵 Trying: {track.title} by {track.artist.name}");

                AudioClip clip = null;
                yield return TryStreamPreview(track.preview, result => clip = result);

                if (clip != null)
                {
                    Debug.Log($"✅ Now Playing: {track.title}");

                    // Send to game manager
                    NOIZEventHandler.TrackReady(clip);

                    // Update visuals
                    trackTitle.text = track.title;
                    artistNameText.text = track.artist.name;
                    StartCoroutine(LoadAlbumCover(track.album.cover_medium));

                    playedCount++;

                    // Wait until current song ends
                    yield return new WaitUntil(() => !BeatManager.isSongPlaying);
                }
                else
                {
                    Debug.LogWarning($"❌ Skipping: {track.title}");
                }
            }

            NOIZEventHandler.GoToMainScene();
            Debug.Log($"✅ Finished streaming {playedCount} tracks.");
        }
    }

    IEnumerator TryStreamPreview(string url, System.Action<AudioClip> onSuccess)
    {
        onSuccess(null);

        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip != null && clip.length > 0f)
                {
                    onSuccess(clip);
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Failed to stream: {request.error}");
            }
        }
    }

    IEnumerator LoadAlbumCover(string imageUrl)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load album image: " + request.error);
                yield break;
            }

            Texture2D srcTex = DownloadHandlerTexture.GetContent(request);

            // Create readable texture
            Texture2D readableTex = new Texture2D(srcTex.width, srcTex.height, srcTex.format, true);
            RenderTexture rt = RenderTexture.GetTemporary(srcTex.width, srcTex.height, 0);
            Graphics.Blit(srcTex, rt);
            RenderTexture.active = rt;

            readableTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            readableTex.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            if (readableTex == null)
            {
                yield break;
            }

            albumArt.transform.parent.gameObject.SetActive(true);
            albumArt.texture = readableTex;
            colorExtractor.ExtractPalette(readableTex);
        }
    }

    void Shuffle<T>(T[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int rand = UnityEngine.Random.Range(i, array.Length);
            (array[i], array[rand]) = (array[rand], array[i]);
        }
    }
    
    public List<DeezerTrack> GetCurrentPlaylist()
    {
        return currentPlaylist;
    }
    
    public bool IsPlayingCustomPlaylist()
    {
        return isPlayingCustomPlaylist;
    }
    
    void OnDestroy()
    {

    }
}