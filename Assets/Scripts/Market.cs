using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;

public class Market : MonoBehaviour
{
    private static Market _instance;
    public static Market Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<Market>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("Market");
                    _instance = go.AddComponent<Market>();
                }
            }
            return _instance;
        }
    }

    private SupabaseMarketCreator marketCreator;

    // Base rates that will be multiplied by 1.1^poopCount
    private const float BASE_RATE = 20f;

    // Current rates calculated from poop counts
    private float blueRate;
    private float purpleRate;
    private float yellowRate;
    private float greenRate;
    private bool ratesInitialized = false;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        marketCreator = FindObjectOfType<SupabaseMarketCreator>();
        if (marketCreator == null)
        {
            Debug.LogError("SupabaseMarketCreator not found!");
        }
    }

    public void UpdateMarketValues(Dictionary<string, int> newPoopCounts)
    {
        StartCoroutine(UpdateMarketValuesRoutine(newPoopCounts));
    }

    private IEnumerator UpdateMarketValuesRoutine(Dictionary<string, int> newPoopCounts)
    {
        // First fetch current values from Supabase
        Dictionary<string, int> currentPoopCounts = new Dictionary<string, int>();
        yield return StartCoroutine(FetchCurrentMarketValues(currentPoopCounts));

        // Merge the current values with new values
        foreach (var kvp in newPoopCounts)
        {
            if (currentPoopCounts.ContainsKey(kvp.Key))
            {
                currentPoopCounts[kvp.Key] = kvp.Value;
            }
        }

        // Update the rates based on current poop counts
        blueRate = BASE_RATE * Mathf.Pow(1.1f, currentPoopCounts["Blue"]);
        purpleRate = BASE_RATE * Mathf.Pow(1.1f, currentPoopCounts["Purple"]);
        yellowRate = BASE_RATE * Mathf.Pow(1.1f, currentPoopCounts["Yellow"]);
        greenRate = BASE_RATE * Mathf.Pow(1.1f, currentPoopCounts["Green"]);
        ratesInitialized = true;

        // Update the counts in Supabase
        if (marketCreator != null)
        {
            marketCreator.UpdateMarketValues(currentPoopCounts);
        }
    }

    private IEnumerator FetchCurrentMarketValues(Dictionary<string, int> poopCounts)
    {
        if (marketCreator == null || marketCreator.marketPlayerId == -1)
        {
            Debug.LogError("Market player not initialized!");
            yield break;
        }

        UnityWebRequest request = new UnityWebRequest(SupabaseMarketCreator.SUPABASE_URL + "/rest/v1/AiPoopers?Player=eq.Market", "GET");
        request.downloadHandler = new DownloadHandlerBuffer();
        
        request.SetRequestHeader("apikey", marketCreator.ANON_KEY);
        request.SetRequestHeader("Authorization", "Bearer " + marketCreator.ANON_KEY);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            if (!string.IsNullOrEmpty(response) && response != "[]")
            {
                // Remove the square brackets as we expect only one market player
                response = response.Trim('[', ']');
                var marketData = JsonUtility.FromJson<MarketData>(response);
                
                // Update poop counts
                poopCounts["Blue"] = marketData.Blue;
                poopCounts["Purple"] = marketData.Purple;
                poopCounts["Yellow"] = marketData.Yellow;
                poopCounts["Green"] = marketData.Green;
            }
        }
        else
        {
            Debug.LogError("Error fetching market values: " + request.error);
        }
    }

    [System.Serializable]
    private class MarketData
    {
        public int Blue;
        public int Purple;
        public int Yellow;
        public int Green;
    }

    // Getters for current rates
    public float GetBlueRate() => ratesInitialized ? blueRate : 0f;
    public float GetPurpleRate() => ratesInitialized ? purpleRate : 0f;
    public float GetYellowRate() => ratesInitialized ? yellowRate : 0f;
    public float GetGreenRate() => ratesInitialized ? greenRate : 0f;
} 