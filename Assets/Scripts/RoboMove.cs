using UnityEngine;
using DG.Tweening;

public class RoboMove : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverAmplitude = 0.5f;
    [SerializeField] private float hoverFrequency = 1f;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveRadius = 5f;
    [SerializeField] private float moveInterval = 3f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform centerPoint;

    private Vector3 startPosition;
    private float baseHeight;
    private Tween hoverTween;
    private Tween moveTween;
    private float nextMoveTime;

    void Start()
    {
        startPosition = transform.position;
        baseHeight = startPosition.y;
        
        if (centerPoint == null)
        {
            GameObject center = new GameObject("RoboMoveCenter");
            center.transform.position = new Vector3(startPosition.x, 0, startPosition.z); // Center point at ground level
            centerPoint = center.transform;
        }
        
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
        moveTween?.Kill();

        Vector2 randomCircle = Random.insideUnitCircle * moveRadius;
        Vector3 currentPos = transform.position;
        Vector3 targetPos = centerPoint.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Keep current height, only move in XZ plane
        targetPos.y = currentPos.y;
        
        float distance = Vector3.Distance(
            new Vector3(currentPos.x, 0, currentPos.z),
            new Vector3(targetPos.x, 0, targetPos.z)
        );
        float duration = distance / moveSpeed;

        moveTween = transform.DOMove(targetPos, duration)
            .SetEase(Ease.InOutSine);

        // Calculate direction without Y component for proper rotation
        Vector3 lookDirection = targetPos - currentPos;
        lookDirection.y = 0;
        
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.DORotateQuaternion(targetRotation, 0.2f);
        }

        nextMoveTime = Time.time + moveInterval;
    }

    public void UpdateHoverParameters()
    {
        StartHoverEffect();
    }

    void OnDrawGizmosSelected()
    {
        if (centerPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(centerPoint.position, moveRadius);
        }
    }
}
