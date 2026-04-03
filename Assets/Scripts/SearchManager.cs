using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class SearchManager : MonoBehaviour
{
    [Header("Search UI")]
    public TMP_InputField searchInput;
    public GameObject loadingIndicator;
    public TextMeshProUGUI searchStatusText;

    [Header("Results UI")]
    public Transform resultsContainer;
    public GameObject artistResultPrefab;
    public ScrollRect scrollView;
    public int maxResults = 5;

    [Header("Search Settings")]
    public float searchDelay = 0.5f; // Delay before searching while typing
    public int minSearchLength = 2;   // Minimum characters before searching

    private List<ArtistData> currentResults = new List<ArtistData>();
    private Coroutine currentCoroutine;
    private Coroutine delayedSearchCoroutine;
    private string lastSearchQuery = "";

    void Start()
    {
        // Setup input field for typing search
        searchInput.onValueChanged.AddListener(OnSearchInputChanged);
        //searchInput.onSubmit.AddListener(delegate { OnSearchClicked(); });

        // Setup placeholder text
        if (searchInput.placeholder != null)
        {
            var placeholder = searchInput.placeholder as TextMeshProUGUI;
            if (placeholder != null)
                placeholder.text = $"Search artists... (min {minSearchLength} chars)";
        }

        // Hide panels initially
        loadingIndicator.SetActive(false);
        if (searchStatusText != null)
            searchStatusText.gameObject.SetActive(false);
    }

    // Called every time the input text changes
    void OnSearchInputChanged(string query)
    {
        // Cancel any pending delayed search
        if (delayedSearchCoroutine != null)
            StopCoroutine(delayedSearchCoroutine);

        string trimmedQuery = query.Trim();

        // Clear results if query is empty or too short
        if (string.IsNullOrEmpty(trimmedQuery) || trimmedQuery.Length < minSearchLength)
        {
            ClearResults();
            if (searchStatusText != null)
            {
                if (trimmedQuery.Length > 0 && trimmedQuery.Length < minSearchLength)
                    searchStatusText.text = $"Type at least {minSearchLength} characters...";
                else
                    searchStatusText.gameObject.SetActive(false);
            }
            return;
        }

        // Don't search if query hasn't changed
        if (trimmedQuery == lastSearchQuery)
            return;

        lastSearchQuery = trimmedQuery;

        // Show typing indicator
        if (searchStatusText != null)
        {
            searchStatusText.text = $"Searching for \"{trimmedQuery}\"...";
            searchStatusText.gameObject.SetActive(true);
        }

        // Start delayed search
        delayedSearchCoroutine = StartCoroutine(DelayedSearch(trimmedQuery));
    }

    IEnumerator DelayedSearch(string query)
    {
        // Wait for the specified delay to allow more typing
        yield return new WaitForSeconds(searchDelay);

        // Start the actual search
        StartSearch(query);
        delayedSearchCoroutine = null;
    }

    void OnSearchClicked()
    {
        // Cancel any pending delayed search
        if (delayedSearchCoroutine != null)
        {
            StopCoroutine(delayedSearchCoroutine);
            delayedSearchCoroutine = null;
        }

        string query = searchInput.text.Trim();
        if (string.IsNullOrEmpty(query) || query.Length < minSearchLength)
        {
            if (searchStatusText != null)
                searchStatusText.text = $"Please enter at least {minSearchLength} characters";
            return;
        }

        StartSearch(query);
    }

    void StartSearch(string query)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        ClearResults();
        loadingIndicator.SetActive(true);

        if (searchStatusText != null)
        {
            searchStatusText.text = $"Searching for \"{query}\"...";
            searchStatusText.gameObject.SetActive(true);
        }

        currentCoroutine = StartCoroutine(DeezerAPI.Instance.SearchArtist(query, OnSearchComplete, OnSearchError));
    }

    void OnSearchComplete(List<ArtistData> artists)
    {
        loadingIndicator.SetActive(false);

        if (artists == null || artists.Count == 0)
        {
            if (searchStatusText != null)
            {
                searchStatusText.text = $"No artists found for \"{lastSearchQuery}\"";
                searchStatusText.gameObject.SetActive(true);
                // Hide status after 3 seconds
                StartCoroutine(HideStatusAfterDelay(3f));
            }
            return;
        }

        // Limit results to maxResults
        int totalFound = artists.Count;
        int displayCount = Mathf.Min(maxResults, artists.Count);
        currentResults = artists.GetRange(0, displayCount);

        DisplayResults(currentResults);

        if (searchStatusText != null)
        {
            if (totalFound > maxResults)
                searchStatusText.text = $"Found {totalFound} artists. Showing top {maxResults}.";
            else
                searchStatusText.text = $"Found {displayCount} artist{(displayCount != 1 ? "s" : "")}";

            searchStatusText.gameObject.SetActive(true);
            StartCoroutine(HideStatusAfterDelay(2f));
        }
    }

    IEnumerator HideStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (searchStatusText != null && searchStatusText.gameObject.activeSelf)
        {
            // Fade out effect (optional)
            searchStatusText.CrossFadeAlpha(0f, 0.3f, false);
            yield return new WaitForSeconds(0.3f);
            searchStatusText.gameObject.SetActive(false);
            searchStatusText.CrossFadeAlpha(1f, 0f, false);
        }
    }

    void OnSearchError(string error)
    {
        loadingIndicator.SetActive(false);
        if (searchStatusText != null)
        {
            searchStatusText.text = $"Search error: {error}";
            searchStatusText.gameObject.SetActive(true);
            StartCoroutine(HideStatusAfterDelay(3f));
        }
        Debug.LogError($"Search error: {error}");
    }

    void DisplayResults(List<ArtistData> artists)
    {
        ClearResults();

        foreach (var artist in artists)
        {
            // Instantiate and setup
            GameObject resultItem = Instantiate(artistResultPrefab, resultsContainer);
            resultItem.SetActive(true);

            ArtistResultItem item = resultItem.GetComponent<ArtistResultItem>();
            if (item != null)
            {
                item.Setup(artist, -1, OnArtistSelected);
            }
            else
            {
                Debug.LogError("ArtistResultItem component missing on prefab!");
            }
        }

        // Refresh scroll view
        Canvas.ForceUpdateCanvases();
        if (scrollView != null)
            scrollView.verticalNormalizedPosition = 1f;
    }

    void ClearResults()
    {
        if (resultsContainer == null) return;

        // Clear children safely
        foreach (Transform child in resultsContainer)
        {
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    void OnArtistSelected(ArtistData artist)
    {
        // Save selected artist info if needed
        NOIZEventHandler.SelectArtist(artist);
    }
    public void ClearSearch()
    {
        searchInput.text = "";
        ClearResults();
        lastSearchQuery = "";
        if (searchStatusText != null)
            searchStatusText.gameObject.SetActive(false);

        // Cancel any ongoing searches
        if (delayedSearchCoroutine != null)
        {
            StopCoroutine(delayedSearchCoroutine);
            delayedSearchCoroutine = null;
        }
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
        loadingIndicator.SetActive(false);
    }

    void OnDestroy()
    {
        if (searchInput != null)
        {
            searchInput.onValueChanged.RemoveListener(OnSearchInputChanged);
            searchInput.onSubmit.RemoveAllListeners();
        }

        if (delayedSearchCoroutine != null)
            StopCoroutine(delayedSearchCoroutine);

        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
    }
}