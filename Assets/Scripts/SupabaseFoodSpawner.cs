using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class SupabaseFoodSpawner : MonoBehaviour
{
    private const string SUPABASE_URL = "https://maxpibvwwratozsmdvyd.supabase.co";
    private string ANON_KEY;
    
    public int foodId = -1;

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
        StartCoroutine(InitializeFoodSystem());
        StartCoroutine(CheckForFoodSpawns());
    }

    IEnumerator InitializeFoodSystem()
    {
        // First try to fetch existing Food system
        UnityWebRequest getRequest = new UnityWebRequest(SUPABASE_URL + "/rest/v1/AiPoopersSystem?Field=eq.Food", "GET");
        getRequest.downloadHandler = new DownloadHandlerBuffer();
        
        getRequest.SetRequestHeader("apikey", ANON_KEY);
        getRequest.SetRequestHeader("Authorization", "Bearer " + ANON_KEY);

        yield return getRequest.SendWebRequest();

        if (getRequest.result == UnityWebRequest.Result.Success)
        {
            string response = getRequest.downloadHandler.text;
            if (string.IsNullOrEmpty(response) || response == "[]")
            {
                // If Food system doesn't exist, create it
                yield return CreateFoodSystem();
            }
            else
            {
                Debug.Log("Food system already exists");
                // Parse the ID from the response for future updates
                string idStr = response.Split(new[] { "\"id\":" }, System.StringSplitOptions.None)[1];
                foodId = int.Parse(idStr.Split(',')[0]);
            }
        }
        else
        {
            Debug.LogError("Error checking for Food system: " + getRequest.error);
        }
    }

    IEnumerator CreateFoodSystem()
    {
        string json = "{\"Field\": \"Food\", \"Dollar\": 0, \"Blue\": 0.0, \"Purple\": 0.0, \"Yellow\": 0.0, \"Green\": 0.0}";

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
            Debug.Log("Food system created successfully!");
            string response = request.downloadHandler.text;
            Debug.Log($"Create response: {response}");
            
            try
            {
                // The response comes as an array, so wrap it in an object
                string wrappedResponse = "{\"items\":" + response + "}";
                var wrapper = JsonUtility.FromJson<FoodDataWrapper>(wrappedResponse);
                
                if (wrapper.items != null && wrapper.items.Length > 0)
                {
                    foodId = wrapper.items[0].id;
                    Debug.Log($"Food system created with ID: {foodId}");
                }
                else
                {
                    Debug.LogError("No food data in response");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing food system response: {e.Message}");
                Debug.LogError($"Raw response: {response}");
            }
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
            if (foodId != -1)
            {
                UnityWebRequest request = new UnityWebRequest(SUPABASE_URL + $"/rest/v1/AiPoopersSystem?id=eq.{foodId}", "GET");
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
        public string Field;
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