using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using System.Linq;

public class RoboMove : MonoBehaviour
{
    private const string SUPABASE_URL = "https://maxpibvwwratozsmdvyd.supabase.co";
    private string ANON_KEY;

    [System.Serializable]
    private class SupabaseConfig
    {
        public string ANON_KEY;
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

    [Header("Hover Settings")]
    [SerializeField] private float hoverAmplitude = 0.5f;
    [SerializeField] private float hoverFrequency = 1f;

    [Header("Movement Settings")]
    [SerializeField] private float moveInterval = 3f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private GameObject boundarySphere; // Reference to the BoundarySphere GameObject

    [Header("Food Settings")]
    [SerializeField] private float foodSeekDuration = 10f; // Duration to stay at food
    [SerializeField] private float foodEatingDistance = 1f; // Distance considered close enough to eat

    [Header("Poop Settings")]
    [SerializeField] private float minPoopInterval = 5f;
    [SerializeField] private float maxPoopInterval = 15f;
    [SerializeField] private float poopScaler = 0.2f;
    [SerializeField] private float poopScaleDuration = 0.2f;

    [Header("Effects")]
    [SerializeField] private GameObject poopParticlePrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip[] poopSounds;

    [Header("Area References")]
    [SerializeField] private Collider blueArea;   // was area1
    [SerializeField] private Collider purpleArea; // was area2
    [SerializeField] private Collider yellowArea; // was area3
    [SerializeField] private Collider greenArea;  // was area4

    private Vector3 startPosition;
    private float baseHeight;
    private Tween hoverTween;
    private Tween moveTween;
    private float nextMoveTime;
    private float nextPoopTime;
    private string currentCubeName = "";
    private Dictionary<string, int> poopCounts = new Dictionary<string, int>();
    [SerializeField] private TMP_Text statsText; // Drag your TextMeshPro component here
    private List<string> currentAreas = new List<string>();

    // Food-related variables
    private GameObject targetFood;
    private float foodSeekEndTime;
    private bool isSeekingFood;

    void Start()
    {
        LoadConfig();
        startPosition = transform.position;
        baseHeight = startPosition.y;

        if (boundarySphere == null)
        {
            Debug.LogError("BoundarySphere is not assigned!");
            return;
        }

        // Get the radius of the BoundarySphere from its SphereCollider
        SphereCollider sphereCollider = boundarySphere.GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            Debug.LogError("BoundarySphere does not have a SphereCollider!");
            return;
        }

        StartHoverEffect();
        MoveToNewPosition();
        ScheduleNextPoop();

        // Initialize dictionary with area names
        poopCounts = new Dictionary<string, int>();
        poopCounts[blueArea.name] = 0;
        poopCounts[purpleArea.name] = 0;
        poopCounts[yellowArea.name] = 0;
        poopCounts[greenArea.name] = 0;
        
        // Fetch current market values
        StartCoroutine(FetchCurrentMarketValues());
    }

    private IEnumerator FetchCurrentMarketValues()
    {
        UnityWebRequest request = new UnityWebRequest(SUPABASE_URL + "/rest/v1/AiPoopers?Player=eq.Market", "GET");
        request.downloadHandler = new DownloadHandlerBuffer();
        
        request.SetRequestHeader("apikey", ANON_KEY);
        request.SetRequestHeader("Authorization", "Bearer " + ANON_KEY);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string response = request.downloadHandler.text;
            if (!string.IsNullOrEmpty(response) && response != "[]")
            {
                // Parse the response to get current values
                // Remove the square brackets as we expect only one market player
                response = response.Trim('[', ']');
                var marketData = JsonUtility.FromJson<MarketData>(response);
                
                // Update local poop counts
                poopCounts[blueArea.name] = marketData.Blue;
                poopCounts[purpleArea.name] = marketData.Purple;
                poopCounts[yellowArea.name] = marketData.Yellow;
                poopCounts[greenArea.name] = marketData.Green;
                
                UpdateStatsDisplay();
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

    void Update()
    {
        if (isSeekingFood && targetFood != null)
        {
            // Check if the seeking duration has expired
            if (Time.time >= foodSeekEndTime)
            {
                isSeekingFood = false;
                targetFood = null;
                MoveToNewPosition(); // Resume normal movement
            }
            else
            {
                // Always move towards food when seeking
                MoveToFood();
            }
        }
        else if (!isSeekingFood)
        {
            if (Time.time >= nextMoveTime)
            {
                MoveToNewPosition();
            }
        }

        if (Time.time >= nextPoopTime)
        {
            Poop();
        }
    }

    void OnDestroy()
    {
        hoverTween?.Kill();
        moveTween?.Kill();
    }

    private void StartHoverEffect()
    {
        hoverTween?.Kill();

        // Hover around the base height
        hoverTween = DOTween.To(
            () => transform.position.y,
            (y) => transform.position = new Vector3(transform.position.x, y, transform.position.z),
            baseHeight + hoverAmplitude,
            1f / hoverFrequency)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(UpdateType.Fixed);
    }

    private void MoveToNewPosition()
    {
        if (boundarySphere == null) return;

        moveTween?.Kill();

        // Get the SphereCollider and calculate its true center and radius in world space
        SphereCollider sphereCollider = boundarySphere.GetComponent<SphereCollider>();
        if (sphereCollider == null) return;

        Vector3 sphereWorldCenter = boundarySphere.transform.position + boundarySphere.transform.TransformVector(sphereCollider.center);
        float sphereWorldRadius = sphereCollider.radius * Mathf.Max(
            boundarySphere.transform.localScale.x,
            boundarySphere.transform.localScale.y,
            boundarySphere.transform.localScale.z
        );

        // Generate a random position within the sphere
        Vector3 randomDirection = Random.onUnitSphere; // Random direction
        float randomDistance = Random.Range(0f, sphereWorldRadius); // Random distance within the radius
        Vector3 offset = randomDirection * randomDistance;

        Vector3 targetPos = sphereWorldCenter + offset;

        // Keep the robot's current height
        targetPos.y = transform.position.y;

        // Ensure the robot stays within the sphere's bounds
        if (Vector3.Distance(targetPos, sphereWorldCenter) > sphereWorldRadius)
        {
            targetPos = sphereWorldCenter + (targetPos - sphereWorldCenter).normalized * sphereWorldRadius;
        }

        // Calculate movement duration based on distance
        float distance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(targetPos.x, 0, targetPos.z));
        float duration = distance / moveSpeed;

        // Only move in XZ plane, leaving Y for the hover effect
        moveTween = DOTween.To(() => transform.position,
            (pos) => transform.position = new Vector3(pos.x, transform.position.y, pos.z),
            new Vector3(targetPos.x, transform.position.y, targetPos.z),
            duration)
            .SetEase(Ease.InOutSine);

        // Orient the robot towards the target
        Vector3 lookDirection = targetPos - transform.position;
        lookDirection.y = 0;

        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.DORotateQuaternion(targetRotation, 0.2f);
        }

        // Set the next move time
        nextMoveTime = Time.time + moveInterval;
    }

    public void UpdateHoverParameters()
    {
        StartHoverEffect();
    }

    void OnDrawGizmosSelected()
    {
        if (boundarySphere != null)
        {
            Gizmos.color = Color.yellow;

            SphereCollider sphereCollider = boundarySphere.GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                // Calculate the correct center in world space
                Vector3 worldCenter = boundarySphere.transform.position + boundarySphere.transform.TransformVector(sphereCollider.center);

                // Adjust the radius for the GameObject's scale
                float radius = sphereCollider.radius * Mathf.Max(
                    boundarySphere.transform.localScale.x,
                    boundarySphere.transform.localScale.y,
                    boundarySphere.transform.localScale.z
                );

                Gizmos.DrawWireSphere(worldCenter, radius);
            }
        }
    }

    private void ScheduleNextPoop()
    {
        nextPoopTime = Time.time + Random.Range(minPoopInterval, maxPoopInterval);
    }

    private void Poop()
    {
        // Prevent multiple poops while scaling animation is in progress
        if (transform.localScale != Vector3.one) return;

        // Scale down to simulate squeezing
        transform.DOScale(new Vector3(poopScaler, poopScaler, poopScaler), poopScaleDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                // Spawn particle effect at ground level under the robot
                if (poopParticlePrefab != null)
                {
                    // Cast a ray downward to find the ground
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, Vector3.down, out hit))
                    {
                        // Spawn slightly above the hit point to avoid clipping
                        Vector3 spawnPosition = hit.point + Vector3.up * 0.05f;
                        SpawnPoopParticle(spawnPosition);
                    }
                    else
                    {
                        // Fallback if no ground is found
                        Vector3 spawnPosition = transform.position;
                        spawnPosition.y = 0; // Default to world ground level
                        SpawnPoopParticle(spawnPosition);
                    }

                    // Play random poop sound if any are assigned
                    if (poopSounds != null && poopSounds.Length > 0)
                    {
                        AudioClip randomSound = poopSounds[Random.Range(0, poopSounds.Length)];
                        SoundManager.Instance.PlaySFX(randomSound);
                    }
                }

                // Flash color
                FlashColor(Color.red, 0.1f);

                // Log poop location for all current areas
                if (currentAreas.Count > 0)
                {
                    // Create a dictionary just for the areas where we pooped
                    Dictionary<string, int> poopedAreas = new Dictionary<string, int>();
                    
                    foreach (string areaName in currentAreas)
                    {
                        if (poopCounts.ContainsKey(areaName))
                        {
                            poopCounts[areaName]++;
                            poopedAreas[areaName] = 1; // Just indicate that we pooped here
                        }
                    }
                    
                    // Only send the areas where we just pooped
                    if (poopedAreas.Count > 0)
                    {
                        Market.Instance.UpdateMarketValues(poopedAreas);
                    }
                    
                    UpdateStatsDisplay();
                }

                // Scale back to normal size with a bounce effect
                transform.DOScale(Vector3.one, 0.2f)
                    .SetEase(Ease.OutBounce);

                // Schedule the next poop
                ScheduleNextPoop();
            });
    }

    private void SpawnPoopParticle(Vector3 position)
    {
        GameObject particleObj = Instantiate(poopParticlePrefab, position, Quaternion.identity);

        ParticleSystem particleSystem = particleObj.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            float duration = particleSystem.main.duration;
            Destroy(particleObj, duration);
        }
    }

    private void FlashColor(Color flashColor, float duration)
    {
    Renderer robotRenderer = GetComponent<Renderer>();
    if (robotRenderer != null)
        {
            Color originalColor = robotRenderer.material.color;

            // Change to the flash color
            robotRenderer.material.DOColor(flashColor, duration)
                .SetEase(Ease.Flash)
                .OnComplete(() =>
                {
                    // Revert to the original color
                    robotRenderer.material.DOColor(originalColor, duration).SetEase(Ease.OutFlash);
                });
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        string areaName = other.gameObject.name;
        if (!currentAreas.Contains(areaName))
        {
            currentAreas.Add(areaName);
            
            // Initialize count for this area if we haven't seen it before
            if (!poopCounts.ContainsKey(areaName))
            {
                poopCounts[areaName] = 0;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        string areaName = other.gameObject.name;
        currentAreas.Remove(areaName);
    }

    private void UpdateStatsDisplay()
    {
        string displayText = "Crypto rates\n";
        displayText += $"Blue: {Market.Instance.GetBlueRate():F1}\n";
        displayText += $"Purple: {Market.Instance.GetPurpleRate():F1}\n";
        displayText += $"Yellow: {Market.Instance.GetYellowRate():F1}\n";
        displayText += $"Green: {Market.Instance.GetGreenRate():F1}";
        statsText.text = displayText;
    }

    // New method to handle food targeting
    public void SetTargetFood(GameObject food)
    {
        targetFood = food;
        if (food != null)
        {
            isSeekingFood = true;
            foodSeekEndTime = Time.time + foodSeekDuration;
            moveTween?.Kill(); // Kill any existing movement
            MoveToFood();
        }
        else
        {
            isSeekingFood = false;
        }
    }

    // New method to move towards food
    private void MoveToFood()
    {
        if (targetFood == null) return;

        moveTween?.Kill();

        Vector3 targetPos = targetFood.transform.position;
        targetPos.y = transform.position.y; // Maintain current height

        // Calculate movement this frame
        Vector3 direction = (targetPos - transform.position).normalized;
        Vector3 newPosition = transform.position + direction * moveSpeed * Time.deltaTime;
        transform.position = newPosition;

        // Orient towards food
        Vector3 lookDirection = targetPos - transform.position;
        lookDirection.y = 0;

        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    public void ResetGame()
    {
        StartCoroutine(ResetGameRoutine());
    }

    private IEnumerator ResetGameRoutine()
    {
        
        // Direct UPDATE query to set all color values to 0
        UnityWebRequest request = new UnityWebRequest(SUPABASE_URL + "/rest/v1/AiPoopers?Player=eq.Market", "PATCH");
        string json = "{\"Blue\":0,\"Purple\":0,\"Yellow\":0,\"Green\":0}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        request.SetRequestHeader("apikey", ANON_KEY);
        request.SetRequestHeader("Authorization", "Bearer " + ANON_KEY);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Prefer", "return=representation");  // This will return the updated row

        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Reset successful. Updated values: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"Reset failed: {request.error}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
            yield break;
        }

        // Reset local poop counts
        foreach (var key in poopCounts.Keys.ToList())
        {
            poopCounts[key] = 0;
        }

        // Wait a frame to ensure Supabase update is complete
        yield return null;

        // Force Market to refresh with empty counts
        Dictionary<string, int> emptyMarket = new Dictionary<string, int>
        {
            ["Blue"] = 0,
            ["Purple"] = 0,
            ["Yellow"] = 0,
            ["Green"] = 0
        };
        Market.Instance.UpdateMarketValues(emptyMarket);
        
        // Update the display
        UpdateStatsDisplay();
        
        Debug.Log("Reset completed!");
    }
}

