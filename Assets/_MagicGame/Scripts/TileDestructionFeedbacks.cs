using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

// NTFS: Maybe have it dynamically spawn gitfabs and create initialization functions for all properties of a gitfab. and then i can use this class for all dynamic gibfab use cases
// And then rename this class to something more abstract and replace the sequence code with just spawning this gameobject from the gamemanager maybe.
// Maybe just start all over with this class and make it totally abstract and use that abstract class for all gibs.
// Also do something to input the particles that will be used in the destruct particle system

// THIS: Maybe no this might not be the most high priority thing I need to do everything works well right now that would take a long time to rework this into an abstract system
// I think I'll just rework this class to specifically be useful for tile destruction and I'll think about making an abstract class later. 
public class TileDestructionFeedbacks : MonoBehaviour
{
    [SerializeField] 
    private ParticleSystem _destroyParticles;

    private List<Gibfab> _gibfabs;
    
    private void Awake()
    {
        _gibfabs = new();
        
        foreach (Transform item in transform.GetChild(0))
        {
            _gibfabs.Add(item.GetComponent<Gibfab>());
        }
    }

    public void PlayDestroyFeedbacks(TileDataSO previousTile)
    {
        // Play destruction feedbacks for the previous tile
        float startHeight = 0.35f;
        float heightStep = 0.15f;
        float currentHeight = startHeight;

        int gibCount = _gibfabs.Count;
        float baseAngleStep = 360f / gibCount; // evenly spaced angles
        
        // NTFS: Prolly not the most elegant solution but eh
        _gibfabs[0].SetSprite(previousTile.DestructionGibSprite1);
        _gibfabs[1].SetSprite(previousTile.DestructionGibSprite2);
        _gibfabs[2].SetSprite(previousTile.DestructionGibSprite3);

        for (int i = 0; i < gibCount; i++)
        {
            // Calculate angle in radians, add the random offset
            float randomOffset = Random.Range(30f, 60f); // same offset applied to all
            float angle = (baseAngleStep * i + randomOffset) * Mathf.Deg2Rad;

            // Unit direction based on angle
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

            // Random velocity magnitude and initial upward speed
            float randomVelocityMagnitude = Random.Range(1f, 3f);
            float randomInitialUpwardSpeed = Random.Range(0f, 0.075f);

            // Launch gib with steadily increasing height
            _gibfabs[i].LaunchGib(randomInitialUpwardSpeed, currentHeight, direction * randomVelocityMagnitude);

            currentHeight += heightStep;
        }

        _destroyParticles.Play();

        Destroy(gameObject, 2f); // Destroy this feedbacks object after 2 seconds
    }
}