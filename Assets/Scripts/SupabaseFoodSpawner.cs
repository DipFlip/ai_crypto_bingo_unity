using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class SupabaseFoodSpawner : MonoBehaviour
{
    private const string SUPABASE_URL = "https://maxpibvwwratozsmdvyd.supabase.co";
    private string ANON_KEY;
    
    public int foodPlayerId = -1;

    [Header("Spawn Locations")]
    [SerializeField] private Transform blueSpawnPoint;
    [SerializeField] private Transform purpleSpawnPoint;
    [SerializeField] private Transform yellowSpawnPoint;
    [SerializeField] private Transform greenSpawnPoint;

    [Header("Food Prefab")]
    [SerializeField] private GameObject foodPrefab;

    private float lastBlue = 0f;
    private float lastPurple = 0f;
    private float lastYellow = 0f;
    private float lastGreen = 0f;

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
        StartCoroutine(InitializeFoodPlayer());
        StartCoroutine(CheckForFoodSpawns());
    }

    IEnumerator InitializeFoodPlayer()
    {
        // First try to fetch existing Food player
        UnityWebRequest getRequest = new UnityWebRequest(SUPABASE_URL + "/rest/v1/AiPoopers?Player=eq.Food", "GET");
        getRequest.downloadHandler = new DownloadHandlerBuffer();
        
        getRequest.SetRequestHeader("apikey", ANON_KEY);
        getRequest.SetRequestHeader("Authorization", "Bearer " + ANON_KEY);

        yield return getRequest.SendWebRequest();

        if (getRequest.result == UnityWebRequest.Result.Success)
        {
            string response = getRequest.downloadHandler.text;
            if (string.IsNullOrEmpty(response) || response == "[]")
            {
                // If Food player doesn't exist, create it
                yield return CreateFoodPlayer();
            }
            else
            {
                Debug.Log("Food player already exists");
                // Parse the ID from the response for future updates
                string idStr = response.Split(new[] { "\"id\":" }, System.StringSplitOptions.None)[1];
                foodPlayerId = int.Parse(idStr.Split(',')[0]);
            }
        }
        else
        {
            Debug.LogError("Error checking for Food player: " + getRequest.error);
        }
    }

    IEnumerator CreateFoodPlayer()
    {
        string json = "{\"Player\": \"Food\", \"Dollar\": 0, \"Blue\": 0.0, \"Purple\": 0.0, \"Yellow\": 0.0, \"Green\": 0.0}";

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
            Debug.Log("Food player created successfully!");
            // After creating, fetch the player to get its ID
            yield return InitializeFoodPlayer();
        }
        else
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }

    IEnumerator CheckForFoodSpawns()
    {
        while (true)
        {
            if (foodPlayerId != -1)
            {
                UnityWebRequest request = new UnityWebRequest(SUPABASE_URL + $"/rest/v1/AiPoopers?id=eq.{foodPlayerId}", "GET");
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("apikey", ANON_KEY);
                request.SetRequestHeader("Authorization", "Bearer " + ANON_KEY);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    if (!string.IsNullOrEmpty(response) && response != "[]")
                    {
                        // Parse the values and check for changes
                        var wrapper = JsonUtility.FromJson<FoodDataWrapper>("{\"items\":" + response + "}");
                        var data = wrapper.items[0];
                        
                        if (data.Blue > lastBlue && blueSpawnPoint != null)
                        {
                            SpawnFood(blueSpawnPoint.position);
                            lastBlue = data.Blue;
                        }
                        if (data.Purple > lastPurple && purpleSpawnPoint != null)
                        {
                            SpawnFood(purpleSpawnPoint.position);
                            lastPurple = data.Purple;
                        }
                        if (data.Yellow > lastYellow && yellowSpawnPoint != null)
                        {
                            SpawnFood(yellowSpawnPoint.position);
                            lastYellow = data.Yellow;
                        }
                        if (data.Green > lastGreen && greenSpawnPoint != null)
                        {
                            SpawnFood(greenSpawnPoint.position);
                            lastGreen = data.Green;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(1f); // Check every second
        }
    }

    private void SpawnFood(Vector3 position)
    {
        if (foodPrefab != null)
        {
            Instantiate(foodPrefab, position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("Food prefab not assigned!");
        }
    }

    [System.Serializable]
    private class FoodData
    {
        public int id;
        public string Player;
        public float Dollar;
        public float Blue;
        public float Purple;
        public float Yellow;
        public float Green;
    }

    [System.Serializable]
    private class FoodDataWrapper
    {
        public FoodData[] items;
    }
} 