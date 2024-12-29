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

    private Vector3 startPosition;
    private float baseHeight;
    private Tween hoverTween;
    private Tween moveTween;
    private float nextMoveTime;
    private float sphereRadius;

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
        sphereRadius = sphereCollider.radius * boundarySphere.transform.localScale.x; // Adjust for scaling

        StartHoverEffect();
        MoveToNewPosition();
    }

    void Update()
    {
        if (Time.time >= nextMoveTime)
        {
            MoveToNewPosition();
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

        // Animate the robot's movement
        moveTween = transform.DOMove(targetPos, duration).SetEase(Ease.InOutSine);

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
}
