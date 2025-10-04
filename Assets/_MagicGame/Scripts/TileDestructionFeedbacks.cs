using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace ProjectWizard
{
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
            // Assign sprites to gibfabs
            for (int i = 0; i < _gibfabs.Count; i++)
            {
                _gibfabs[i].SetSprite(previousTile.GetRandomMiningParticleSprite());
            }

            // Play destruction feedbacks for the previous tile
            float startHeight = 0.35f;
            float heightStep = 0.15f;
            float currentHeight = startHeight;

            int gibCount = _gibfabs.Count;
            float baseAngleStep = 360f / gibCount; // evenly spaced angles

            for (int i = 0; i < gibCount; i++)
            {
                // Calculate angle in radians, add the random offset
                float randomOffset = Random.Range(30f, 60f); // same offset applied to all
                float angle = (baseAngleStep * i + randomOffset) * Mathf.Deg2Rad;

                // Unit direction based on angle
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

                // Random velocity magnitude and initial upward speed
                float randomVelocityMagnitude = Random.Range(2f, 5.25f);
                float randomInitialUpwardSpeed = Random.Range(0f, 0.135f);

                // Launch gib with steadily increasing height
                _gibfabs[i].LaunchGib(randomInitialUpwardSpeed, currentHeight, direction * randomVelocityMagnitude);

                currentHeight += heightStep;
            }

            // Configure particle system's texture sheet animation module to use quadrant sprites
            var tsa = _destroyParticles.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Sprites;
            tsa.RemoveSprite(0);
            for (int i = 0; i < 6; i++)
            {
                tsa.AddSprite(previousTile.GetRandomMiningParticleSprite());
            }
            _destroyParticles.Play();

            Destroy(gameObject, 4f); // Destroy this feedbacks object after 4 seconds
        }
    }
}
