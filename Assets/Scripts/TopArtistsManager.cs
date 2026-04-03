using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TopArtistsManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform artistsContainer;
    public GameObject artistItemPrefab;
    public ScrollRect scrollView;
    public Button refreshButton;
    
    [Header("Settings")]
    public int topArtistsLimit = 50;
    public string chartType = "artists"; // artists, tracks, albums
    
    [Header("Artist Display")]
    public bool showRank = true;
    public bool showFanCount = true;
    public Color evenRowColor = new Color(0.2f, 0.2f, 0.2f);
    public Color oddRowColor = new Color(0.25f, 0.25f, 0.25f);
    
    private List<ArtistData> topArtists = new List<ArtistData>();
    private Coroutine currentCoroutine;
    private int currentPage = 0;
    private const int ITEMS_PER_PAGE = 25;
    private bool isLoadingMore = false;
    
    void Start()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshTopArtists);
        
        LoadTopArtists();
    }
    
    public void LoadTopArtists()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        
        currentCoroutine = StartCoroutine(FetchTopArtists());
    }
    
    public void RefreshTopArtists()
    {
        topArtists.Clear();
        ClearArtists();
        currentPage = 0;
        LoadTopArtists();
    }
    
    IEnumerator FetchTopArtists()
    {
        ShowStatus("Loading top artists...");
        
        // Deezer chart endpoint for artists
        string url = $"https://api.deezer.com/chart/0/artists?limit={topArtistsLimit}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                TopArtistsResponse response = JsonUtility.FromJson<TopArtistsResponse>(json);
                
                if (response != null && response.data != null && response.data.Length > 0)
                {
                    ProcessArtists(response.data);
                    ShowStatus($"Loaded {topArtists.Count} top artists");
                }
                else
                {
                    ShowStatus("No artists found");
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch top artists: {request.error}");
                ShowStatus($"Error: {request.error}");
            }
        }
    }
    
    void ProcessArtists(ArtistData[] artists)
    {
        topArtists.Clear();
        ClearArtists();
        
        for (int i = 0; i < artists.Length; i++)
        {
            var artist = artists[i];
            ArtistData data = new ArtistData
            {
                rank = i + 1,
                id = artist.id,
                name = artist.name,
                picture_medium = artist.picture_medium,
                picture_big = artist.picture_big,
                nb_fan = artist.nb_fan,
                tracklist = artist.tracklist
            };
            topArtists.Add(data);
        }
        
        DisplayArtists();
    }
    
    void DisplayArtists()
    {
        int startIndex = currentPage * ITEMS_PER_PAGE;
        int endIndex = Mathf.Min(startIndex + ITEMS_PER_PAGE, topArtists.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            var artist = topArtists[i];
            GameObject artistItem = Instantiate(artistItemPrefab, artistsContainer);
            artistItem.SetActive(true);
            
            ArtistResultItem itemUI = artistItem.GetComponent<ArtistResultItem>();
            if (itemUI != null)
            {
                itemUI.Setup(artist, i, OnArtistSelected);
                itemUI.SetBackgroundColor(i % 2 == 0 ? evenRowColor : oddRowColor);
            }
        }
    }
    
    void LoadMoreArtists()
    {
        if (isLoadingMore) return;
        if ((currentPage + 1) * ITEMS_PER_PAGE >= topArtists.Count) return;
        
        isLoadingMore = true;
        currentPage++;
        DisplayArtists();
        isLoadingMore = false;
    }
    
    void ClearArtists()
    {
        foreach (Transform child in artistsContainer)
        {
            Destroy(child.gameObject);
        }
    }
    
    void OnArtistSelected(ArtistData artist)
    {
        Debug.Log($"Selected artist: {artist.name}");
        // TODO: Show artist detail popup or load their tracks
        ShowStatus($"Selected: {artist.name}");
        NOIZEventHandler.SelectArtist(artist);
    }
    
    void ShowStatus(string message)
    {

    }
    
    void HideStatus()
    {
    }
    
    void OnDestroy()
    {
        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(RefreshTopArtists);
        
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
    }
}

// Data Models
[System.Serializable]
public class TopArtistsResponse
{
    public ArtistData[] data;
    public int total;
}
