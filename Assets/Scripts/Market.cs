using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using System.Linq;

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
        // If we're resetting (all new counts are 0), skip fetching current values
        bool isReset = newPoopCounts.All(kvp => kvp.Value == 0);
        
        Dictionary<string, int> finalPoopCounts;
        if (isReset)
        {
            // For reset, just use zeros directly
            finalPoopCounts = new Dictionary<string, int>
            {
                ["Blue"] = 0,
                ["Purple"] = 0,
                ["Yellow"] = 0,
                ["Green"] = 0
            };
        }
        else
        {
            // For normal updates, fetch current values and add 1 for each new poop
            Dictionary<string, int> currentPoopCounts = new Dictionary<string, int>();
            yield return StartCoroutine(FetchCurrentMarketValues(currentPoopCounts));

            // For each color in newPoopCounts, if it's greater than 0, add 1 to the current count
            finalPoopCounts = new Dictionary<string, int>
            {
                ["Blue"] = currentPoopCounts["Blue"] + (newPoopCounts.ContainsKey("Blue") && newPoopCounts["Blue"] > 0 ? 1 : 0),
                ["Purple"] = currentPoopCounts["Purple"] + (newPoopCounts.ContainsKey("Purple") && newPoopCounts["Purple"] > 0 ? 1 : 0),
                ["Yellow"] = currentPoopCounts["Yellow"] + (newPoopCounts.ContainsKey("Yellow") && newPoopCounts["Yellow"] > 0 ? 1 : 0),
                ["Green"] = currentPoopCounts["Green"] + (newPoopCounts.ContainsKey("Green") && newPoopCounts["Green"] > 0 ? 1 : 0)
            };
        }

        // Update the rates based on final poop counts
        blueRate = BASE_RATE * Mathf.Pow(1.1f, finalPoopCounts["Blue"]);
        purpleRate = BASE_RATE * Mathf.Pow(1.1f, finalPoopCounts["Purple"]);
        yellowRate = BASE_RATE * Mathf.Pow(1.1f, finalPoopCounts["Yellow"]);
        greenRate = BASE_RATE * Mathf.Pow(1.1f, finalPoopCounts["Green"]);
        ratesInitialized = true;


        // Only update Supabase if this isn't a reset (since reset is handled by RoboMove)
        if (!isReset && marketCreator != null)
        {
            marketCreator.UpdateMarketValues(finalPoopCounts);
        }
    }

    private IEnumerator FetchCurrentMarketValues(Dictionary<string, int> poopCounts)
    {
        if (marketCreator == null || marketCreator.marketPlayerId == -1)
        {
            Debug.LogError("Market system not initialized!");
            yield break;
        }

        UnityWebRequest request = new UnityWebRequest(SupabaseMarketCreator.SUPABASE_URL + "/rest/v1/AiPoopersSystem?Field=eq.Market", "GET");
        request.downloadHandler = new DownloadHandlerBuffer();
        
        request.SetRequestHeader("apikey", marketCreator.ANON_KEY);
        request.SetRequestHeader("Authorization", "Bearer " + marketCreator.ANON_KEY);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            if (!string.IsNullOrEmpty(response) && response != "[]")
            {
                // Remove the square brackets as we expect only one market system
                response = response.Trim('[', ']');
                var marketData = JsonUtility.FromJson<MarketData>(response);
                
                // Update poop counts
                poopCounts["Blue"] = marketData.Blue;
                poopCounts["Purple"] = marketData.Purple;
                poopCounts["Yellow"] = marketData.Yellow;
                poopCounts["Green"] = marketData.Green;
            }
            else
            {
                Debug.LogWarning("Market Response was empty or []");
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
        public int id;
        public string Field;
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

    public void ResetMarket()
    {
        StartCoroutine(ResetMarketRoutine());
    }

    private IEnumerator ResetMarketRoutine()
    {
        Debug.Log("Starting market reset...");
        
        // Direct UPDATE query to set all color values to 0
        UnityWebRequest request = new UnityWebRequest(SupabaseMarketCreator.SUPABASE_URL + "/rest/v1/AiPoopersSystem?Field=eq.Market", "PATCH");
        string json = "{\"Blue\":0,\"Purple\":0,\"Yellow\":0,\"Green\":0}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        request.SetRequestHeader("apikey", marketCreator.ANON_KEY);
        request.SetRequestHeader("Authorization", "Bearer " + marketCreator.ANON_KEY);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Prefer", "return=representation");

        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            // Force a refresh of our local values
            Dictionary<string, int> resetCounts = new Dictionary<string, int>
            {
                ["Blue"] = 0,
                ["Purple"] = 0,
                ["Yellow"] = 0,
                ["Green"] = 0
            };
            UpdateMarketValues(resetCounts);
        }
        else
        {
            Debug.LogError($"Reset failed: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
        }
    }
} 