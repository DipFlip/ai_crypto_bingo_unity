using UnityEngine;
using DG.Tweening;

public class RoboMove : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverAmplitude = 0.5f;  // How far up/down the robot moves
    [SerializeField] private float hoverFrequency = 1f;    // How fast the hover cycle completes
    
    private Vector3 startPosition;
    private Tween hoverTween;

    void Start()
    {
        startPosition = transform.position;
        StartHoverEffect();
    }

    void OnDestroy()
    {
        // Clean up the tween when the object is destroyed
        hoverTween?.Kill();
    }

    private void StartHoverEffect()
    {
        // Kill any existing tween
        hoverTween?.Kill();

        // Create an infinite hover loop
        hoverTween = transform.DOMoveY(startPosition.y + hoverAmplitude, 1f / hoverFrequency)
            .SetEase(Ease.InOutSine)  // Smooth easing for natural movement
            .SetLoops(-1, LoopType.Yoyo)  // -1 means infinite loops, Yoyo means it goes back and forth
            .SetUpdate(UpdateType.Fixed);  // Use FixedUpdate for consistent physics
    }

    // Call this method when amplitude or frequency values are changed in the inspector
    public void UpdateHoverParameters()
    {
        StartHoverEffect();
    }
}
