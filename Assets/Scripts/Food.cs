using UnityEngine;

public class Food : MonoBehaviour
{
    [SerializeField] private ParticleSystem eatingEffect;
    [SerializeField] private AudioClip foodSound;
    private float destroyTime;
    private bool particlesPlayed = false;
    private bool startedDestroying = false;

    private void Start()
    {
        // Play spawn sound if we have one
        if (foodSound != null)
        {
            SoundManager.Instance.PlaySFX(foodSound);
        }

        // Find the robot and notify it about the food
        RoboMove robot = FindObjectOfType<RoboMove>();
        if (robot != null)
        {
            robot.SetTargetFood(gameObject);
        }

        // Set destroy time to 6 seconds from now
        destroyTime = Time.time + 6f;
    }

    private void Update()
    {
        if (Time.time >= destroyTime && !startedDestroying)
        {
            startedDestroying = true;

            // Play particles if we have them
            if (eatingEffect != null)
            {
                // Create a copy of the particle system at our position
                ParticleSystem particles = Instantiate(eatingEffect, transform.position, transform.rotation);
                // particles.Play();
                
                // Destroy the particle system after it finishes
                float particleDuration = particles.main.duration + particles.main.startLifetime.constantMax;
                Destroy(particles.gameObject, particleDuration);
            }
            
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        RoboMove robot = FindObjectOfType<RoboMove>();
        if (robot != null)
        {
            // This will make the robot stop targeting this food if it's destroyed
            robot.SetTargetFood(null);
        }
    }
} 