using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

public class SupabaseMarketCreator : MonoBehaviour
{
    public const string SUPABASE_URL = "https://maxpibvwwratozsmdvyd.supabase.co";
    public string ANON_KEY;
    
    public int marketPlayerId = -1;

    void Awake()
    {
        LoadConfig();
    }

    private void LoadConfig()
    {
        string configPath = Path.Combine(Application.dataPath, "Config", "SupabaseConfig.json");
        if (File.Exists(configPath))
        {
            string jsonContent = File.ReadAllText(configPath);
            var config = JsonUtility.FromJson<SupabaseConfig>(jsonContent);
            ANON_KEY = config.ANON_KEY;
        }
        else
        {
            Debug.LogError("SupabaseConfig.json not found! Please create it from the template file.");
        }
    }

    [System.Serializable]
    private class SupabaseConfig
    {
        public string ANON_KEY;
    }

    void Start()
    {
        StartCoroutine(InitializeMarketPlayer());
    }

    IEnumerator InitializeMarketPlayer()
    {
        // First try to fetch existing Market player
        UnityWebRequest getRequest = new UnityWebRequest(SUPABASE_URL + "/rest/v1/AiPoopersSystem?Field=eq.Market", "GET");
        getRequest.downloadHandler = new DownloadHandlerBuffer();
        
        getRequest.SetRequestHeader("apikey", ANON_KEY);
        getRequest.SetRequestHeader("Authorization", "Bearer " + ANON_KEY);

        yield return getRequest.SendWebRequest();

        if (getRequest.result == UnityWebRequest.Result.Success)
        {
            string response = getRequest.downloadHandler.text;
            if (string.IsNullOrEmpty(response) || response == "[]")
            {
                // If Market player doesn't exist, create it
                yield return CreateMarketPlayer();
            }
            else
            {
                Debug.Log("Market system already exists");
                // Parse the ID from the response for future updates
                string idStr = response.Split(new[] { "\"id\":" }, System.StringSplitOptions.None)[1];
                marketPlayerId = int.Parse(idStr.Split(',')[0]);
            }
        }
        else
        {
            Debug.LogError("Error checking for Market system: " + getRequest.error);
        }
    }

    [System.Serializable]
    private class MarketDataWrapper
    {
        public MarketData[] data;
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
        public int Dollar;
    }

    IEnumerator CreateMarketPlayer()
    {
        // Initialize Market system with zero values
        string json = "{\"Field\": \"Market\", \"Blue\": 0, \"Purple\": 0, \"Yellow\": 0, \"Green\": 0, \"Dollar\": 1000000}";

        UnityWebRequest request = new UnityWebRequest(SUPABASE_URL + "/rest/v1/AiPoopersSystem", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("apikey", ANON_KEY);
        request.SetRequestHeader("Authorization", "Bearer " + ANON_KEY);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Prefer", "return=representation");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Market system created successfully!");
            string response = request.downloadHandler.text;
            Debug.Log($"Create response: {response}");
            
            try
            {
                // The response comes as an array, so wrap it in an object
                string wrappedResponse = "{\"data\":" + response + "}";
                var wrapper = JsonUtility.FromJson<MarketDataWrapper>(wrappedResponse);
                
                if (wrapper.data != null && wrapper.data.Length > 0)
                {
                    marketPlayerId = wrapper.data[0].id;
                    Debug.Log($"Market system created with ID: {marketPlayerId}");
                }
                else
                {
                    Debug.LogError("No market data in response");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing market system response: {e.Message}");
                Debug.LogError($"Raw response: {response}");
            }
        }
        else
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }

    public void UpdateMarketValues(Dictionary<string, int> poopCounts)
    {
        if (marketPlayerId == -1)
        {
            Debug.LogError("Market player ID not initialized!");
            return;
        }
        
        string json = $"{{\"Blue\": {poopCounts["Blue"]}, \"Purple\": {poopCounts["Purple"]}, \"Yellow\": {poopCounts["Yellow"]}, \"Green\": {poopCounts["Green"]}}}";
        StartCoroutine(UpdateMarket(json));
    }

    private IEnumerator UpdateMarket(string json)
    {
        string url = $"{SUPABASE_URL}/rest/v1/AiPoopersSystem?id=eq.{marketPlayerId}";
        UnityWebRequest request = new UnityWebRequest(url, "PATCH");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("apikey", ANON_KEY);
        request.SetRequestHeader("Authorization", "Bearer " + ANON_KEY);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Prefer", "return=minimal");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Market values updated successfully!");
        }
        else
        {
            Debug.LogError("Error updating market: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }
}