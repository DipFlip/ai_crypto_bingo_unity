using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System.IO;

public class LeaderboardManager : MonoBehaviour
{
    private const string SUPABASE_URL = "https://maxpibvwwratozsmdvyd.supabase.co";
    private string ANON_KEY;
    
    [SerializeField] private TMP_Text leaderboardText;
    [SerializeField] private float updateInterval = 5f; // Update every 5 seconds

    [System.Serializable]
    private class PlayerData
    {
        public int id;
        public string Player;
        public float Dollar;
        public float Blue;
        public float Purple;
        public float Yellow;
        public float Green;
        public float TotalWealth; // Calculated field
    }

    [System.Serializable]
    private class PlayerList
    {
        public PlayerData[] players;
    }

    [System.Serializable]
    private class SupabaseConfig
    {
        public string ANON_KEY;
    }

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

    void Start()
    {
        if (leaderboardText == null)
        {
            Debug.LogError("Leaderboard Text component not assigned!");
            return;
        }

        StartCoroutine(UpdateLeaderboardRoutine());
    }

    private IEnumerator UpdateLeaderboardRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(FetchAndUpdateLeaderboard());
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private IEnumerator FetchAndUpdateLeaderboard()
    {
        UnityWebRequest request = new UnityWebRequest(SUPABASE_URL + "/rest/v1/AiPoopers?select=*", "GET");
        request.downloadHandler = new DownloadHandlerBuffer();
        
        request.SetRequestHeader("apikey", ANON_KEY);
        request.SetRequestHeader("Authorization", "Bearer " + ANON_KEY);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            // Parse the JSON array manually since Unity's JsonUtility doesn't handle arrays directly
            response = "{\"players\":" + response + "}";
            
            PlayerList playerList = JsonUtility.FromJson<PlayerList>(response);
            
            // Filter out Market and Food players, and calculate total wealth
            var players = playerList.players
                .Where(p => p.Player != "Market" && p.Player != "Food")
                .ToList();

            // Calculate total wealth for each player based on current market rates
            foreach (var player in players)
            {
                player.TotalWealth = player.Dollar +
                    (player.Blue * Market.Instance.GetBlueRate()) +
                    (player.Purple * Market.Instance.GetPurpleRate()) +
                    (player.Yellow * Market.Instance.GetYellowRate()) +
                    (player.Green * Market.Instance.GetGreenRate());
            }

            // Sort by total wealth and take top 10
            var top10 = players
                .OrderByDescending(p => p.TotalWealth)
                .Take(10)
                .ToList();

            // Update the leaderboard text
            string leaderboardContent = "Top 10 Richest Players\n\n";
            for (int i = 0; i < top10.Count; i++)
            {
                leaderboardContent += $"{i + 1}. {top10[i].Player}: ${top10[i].TotalWealth:N0}\n";
            }

            leaderboardText.text = leaderboardContent;
        }
        else
        {
            Debug.LogError("Error fetching leaderboard: " + request.error);
        }
    }
} 