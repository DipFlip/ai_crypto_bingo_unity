using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ResetButton : MonoBehaviour
{
    private const string SUPABASE_URL = "https://maxpibvwwratozsmdvyd.supabase.co";
    private string ANON_KEY;

    void Awake()
    {
        LoadConfig();
    }

    private void LoadConfig()
    {
        string configPath = System.IO.Path.Combine(Application.dataPath, "Config", "SupabaseConfig.json");
        if (System.IO.File.Exists(configPath))
        {
            string jsonContent = System.IO.File.ReadAllText(configPath);
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

    // This method can be connected to a button in the Unity Inspector
    public void OnResetGameClick()
    {
        StartCoroutine(ResetGameRoutine());
    }

    private IEnumerator ResetGameRoutine()
    {
        Debug.Log("Starting full game reset...");

        // First reset the market
        Market.Instance.ResetMarket();
        yield return new WaitForSeconds(0.5f); // Give time for market reset to complete

        // Reset Food values
        string foodJson = "{\"Dollar\": 0, \"Blue\": 0, \"Purple\": 0, \"Yellow\": 0, \"Green\": 0}";
        UnityWebRequest foodRequest = new UnityWebRequest(SUPABASE_URL + "/rest/v1/AiPoopers?Player=eq.Food", "PATCH");
        byte[] foodBodyRaw = System.Text.Encoding.UTF8.GetBytes(foodJson);
        foodRequest.uploadHandler = new UploadHandlerRaw(foodBodyRaw);
        foodRequest.downloadHandler = new DownloadHandlerBuffer();
        
        foodRequest.SetRequestHeader("apikey", ANON_KEY);
        foodRequest.SetRequestHeader("Authorization", "Bearer " + ANON_KEY);
        foodRequest.SetRequestHeader("Content-Type", "application/json");
        foodRequest.SetRequestHeader("Prefer", "return=minimal");

        yield return foodRequest.SendWebRequest();

        if (foodRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Food reset failed: {foodRequest.error}");
        }

        // Reset all other players to starting budget
        string playersJson = "{\"Dollar\": 100, \"Blue\": 0, \"Purple\": 0, \"Yellow\": 0, \"Green\": 0}";
        UnityWebRequest playersRequest = new UnityWebRequest(SUPABASE_URL + "/rest/v1/AiPoopers?Player=neq.Market&Player=neq.Food", "PATCH");
        byte[] playersBodyRaw = System.Text.Encoding.UTF8.GetBytes(playersJson);
        playersRequest.uploadHandler = new UploadHandlerRaw(playersBodyRaw);
        playersRequest.downloadHandler = new DownloadHandlerBuffer();
        
        playersRequest.SetRequestHeader("apikey", ANON_KEY);
        playersRequest.SetRequestHeader("Authorization", "Bearer " + ANON_KEY);
        playersRequest.SetRequestHeader("Content-Type", "application/json");
        playersRequest.SetRequestHeader("Prefer", "return=minimal");

        yield return playersRequest.SendWebRequest();

        if (playersRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Players reset failed: {playersRequest.error}");
        }

        Debug.Log("Full game reset completed!");
    }
} 