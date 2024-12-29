using UnityEngine;
using DG.Tweening;

public class RoboMove : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverAmplitude = 0.5f;
    [SerializeField] private float hoverFrequency = 1f;

    [Header("Movement Settings")]
    [SerializeField] private float moveInterval = 3f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private GameObject boundarySphere; // Reference to the BoundarySphere GameObject

    [Header("Poop Settings")]
    [SerializeField] private float minPoopInterval = 5f;
    [SerializeField] private float maxPoopInterval = 15f;

    [Header("Effects")]
    [SerializeField] private GameObject poopParticlePrefab;

    private Vector3 startPosition;
    private float baseHeight;
    private Tween hoverTween;
    private Tween moveTween;
    private float nextMoveTime;
    private float nextPoopTime;
    private string currentCubeName = "";

    void Start()
    {
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
        // sphereRadius = sphereCollider.radius * boundarySphere.transform.localScale.x; // Adjust for scaling

        StartHoverEffect();
        MoveToNewPosition();
        ScheduleNextPoop();
    }

    void Update()
    {
        if (Time.time >= nextMoveTime)
        {
            MoveToNewPosition();
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
    // Scale down to simulate squeezing
    transform.DOScale(new Vector3(0.8f, 0.8f, 0.8f), 0.2f)
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
            }

            // Flash color
            FlashColor(Color.red, 0.1f);

            // Log poop location
            if (!string.IsNullOrEmpty(currentCubeName))
            {
                Debug.Log($"Robot pooped in {currentCubeName}!");
            }
            else
            {
                Debug.Log("Robot pooped outside of any cube!");
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
        // Update the current cube we're in
        currentCubeName = other.gameObject.name;
        Debug.Log($"Entered cube: {currentCubeName}");
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"Exited cube: {other.gameObject.name}");
        if (currentCubeName == other.gameObject.name)
        {
            currentCubeName = "";
        }
    }
}
