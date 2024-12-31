using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;

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
    private float blueRate = BASE_RATE;
    private float purpleRate = BASE_RATE;
    private float yellowRate = BASE_RATE;
    private float greenRate = BASE_RATE;

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

    public void UpdateMarketValues(Dictionary<string, int> poopCounts)
    {
        // Update the rates based on poop counts
        blueRate = BASE_RATE * Mathf.Pow(1.1f, poopCounts["Blue"]);
        purpleRate = BASE_RATE * Mathf.Pow(1.1f, poopCounts["Purple"]);
        yellowRate = BASE_RATE * Mathf.Pow(1.1f, poopCounts["Yellow"]);
        greenRate = BASE_RATE * Mathf.Pow(1.1f, poopCounts["Green"]);

        // Update the counts in Supabase
        if (marketCreator != null)
        {
            marketCreator.UpdateMarketValues(poopCounts);
        }
    }

    // Getters for current rates
    public float GetBlueRate() => blueRate;
    public float GetPurpleRate() => purpleRate;
    public float GetYellowRate() => yellowRate;
    public float GetGreenRate() => greenRate;
} 