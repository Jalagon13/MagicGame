using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

public class TileDestructionFeedbacks : MonoBehaviour
{
    [SerializeField] 
    private ParticleSystem _destroyParticles;

    [Space(15)]
    [SerializeField] 
    private List<Gibfab> _gibfabs;

    public void PlayDestroyFeedbacks(TileDataSO previousTile)
    {
        // Play destruction feedbacks for the previous tile
        Debug.Log($"Spawning destroy feedbacks for {previousTile.name} at {transform.position}");
        float startHeight = 0.35f;
        float heightStep = 0.15f;
        float currentHeight = startHeight;

        int gibCount = _gibfabs.Count;
        float baseAngleStep = 360f / gibCount; // evenly spaced angles
        float randomOffset = Random.Range(30f, 60f); // same offset applied to all

        for (int i = 0; i < gibCount; i++)
        {
            // Calculate angle in radians, add the random offset
            float angle = (baseAngleStep * i + randomOffset) * Mathf.Deg2Rad;

            // Unit direction based on angle
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

            // Random velocity magnitude and initial upward speed
            float randomVelocityMagnitude = Random.Range(1f, 2f);
            float randomInitialUpwardSpeed = Random.Range(0f, 0.075f);

            // Launch gib with steadily increasing height
            _gibfabs[i].LaunchGib(randomInitialUpwardSpeed, currentHeight, direction * randomVelocityMagnitude);

            currentHeight += heightStep;
        }

        _destroyParticles.Play();

        Destroy(gameObject, 2f); // Destroy this feedbacks object after 2 seconds
    }
}
