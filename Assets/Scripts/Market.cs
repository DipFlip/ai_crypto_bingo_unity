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

    // Current rates
    private float dollarRate = 100f;
    private float blueRate = 20f;
    private float purpleRate = 20f;
    private float yellowRate = 20f;
    private float greenRate = 20f;
    private float originalDollarRate = 100f;
    private float originalBlueRate = 20f;
    private float originalPurpleRate = 20f;
    private float originalYellowRate = 20f;
    private float originalGreenRate = 20f;

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
        UpdateMarket();
    }

    public void UpdateMarketValues(Dictionary<string, int> poopCounts)
    {
        foreach (string key in poopCounts.Keys)
        {
            Debug.Log($"Area: {key}");
        }
        dollarRate = originalDollarRate;
        blueRate = originalBlueRate * Mathf.Pow(1.1f, poopCounts["Blue"]);
        purpleRate = originalPurpleRate * Mathf.Pow(1.1f, poopCounts["Purple"]); 
        yellowRate = originalYellowRate * Mathf.Pow(1.1f, poopCounts["Yellow"]);
        greenRate = originalGreenRate * Mathf.Pow(1.1f, poopCounts["Green"]);
        UpdateMarket();
    }

    private void UpdateMarket()
    {
        if (marketCreator != null && marketCreator.marketPlayerId != -1)
        {
            marketCreator.UpdateMarketValues(dollarRate, blueRate, purpleRate, yellowRate, greenRate);
        }
    }

    // Getters for current rates
    public float GetBlueRate() => blueRate;
    public float GetPurpleRate() => purpleRate;
    public float GetYellowRate() => yellowRate;
    public float GetGreenRate() => greenRate;
} 