using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Threading.Tasks;
using System.IO;

public class SupabaseMarketCreator : MonoBehaviour
{
    private const string SUPABASE_URL = "https://maxpibvwwratozsmdvyd.supabase.co";
    private string ANON_KEY;
    
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
        UnityWebRequest getRequest = new UnityWebRequest(SUPABASE_URL + "/rest/v1/AiPoopers?Player=eq.Market", "GET");
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
                Debug.Log("Market player already exists");
                // Parse the ID from the response for future updates
                // Assuming response is in format [{"id": 123, ...}]
                string idStr = response.Split(new[] { "\"id\":" }, System.StringSplitOptions.None)[1];
                marketPlayerId = int.Parse(idStr.Split(',')[0]);
            }
        }
        else
        {
            Debug.LogError("Error checking for Market player: " + getRequest.error);
        }
    }

    IEnumerator CreateMarketPlayer()
    {
        string json = "{\"Player\": \"Market\", \"Dollar\": 1000000, \"Blue\": 10.0, \"Purple\": 20.0, \"Yellow\": 30.0, \"Green\": 40.0}";

        UnityWebRequest request = new UnityWebRequest(SUPABASE_URL + "/rest/v1/AiPoopers", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("apikey", ANON_KEY);
        request.SetRequestHeader("Authorization", "Bearer " + ANON_KEY);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Prefer", "resolution=merge-duplicates");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Market player created successfully!");
            // Parse the ID from the response
            string response = request.downloadHandler.text;
            string idStr = response.Split(new[] { "\"id\":" }, System.StringSplitOptions.None)[1];
            marketPlayerId = int.Parse(idStr.Split(',')[0]);
        }
        else
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }

    public void UpdateMarketValues(float dollar, float blue, float purple, float yellow, float green)
    {
        if (marketPlayerId == -1)
        {
            Debug.LogError("Market player ID not initialized!");
            return;
        }
        Debug.Log("Updating market values: Dollar: " + dollar + ", Blue: " + blue + ", Purple: " + purple + ", Yellow: " + yellow + ", Green: " + green);
        string json = $"{{\"Dollar\": {dollar}, \"Blue\": {blue}, \"Purple\": {purple}, \"Yellow\": {yellow}, \"Green\": {green}}}";
        StartCoroutine(UpdateMarket(json));
    }

    private IEnumerator UpdateMarket(string json)
    {
        string url = $"{SUPABASE_URL}/rest/v1/AiPoopers?id=eq.{marketPlayerId}";
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