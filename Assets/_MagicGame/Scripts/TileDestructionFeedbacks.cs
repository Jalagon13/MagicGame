using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

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
        // Split the default sprite into 4 quadrants and assign them to gibfabs
        Sprite[] quads = CreateQuadrantSprites(previousTile.m_DefaultSprite);
        
        // Assign sprites to gibfabs
        for (int i = 0; i < 4 && i < _gibfabs.Count; i++)
        {
            _gibfabs[i].SetSprite(quads[i]);
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
            float randomVelocityMagnitude = Random.Range(1f, 3f);
            float randomInitialUpwardSpeed = Random.Range(0f, 0.075f);

            // Launch gib with steadily increasing height
            _gibfabs[i].LaunchGib(randomInitialUpwardSpeed, currentHeight, direction * randomVelocityMagnitude);

            currentHeight += heightStep;
        }

        // Configure particle system's texture sheet animation module to use quadrant sprites
        var tsa = _destroyParticles.textureSheetAnimation;
        tsa.enabled = true;
        tsa.mode = ParticleSystemAnimationMode.Sprites;
        tsa.RemoveSprite(0);
        for (int i = 0; i < quads.Length; i++)
        {
            tsa.AddSprite(quads[i]);
        }
        _destroyParticles.Play();

        Destroy(gameObject, 4f); // Destroy this feedbacks object after 4 seconds
    }

    private Sprite[] CreateQuadrantSprites(Sprite original)
    {
        Texture2D tex = original.texture;
        Rect rect = original.rect;

        float halfWidth = rect.width / 2f;
        float halfHeight = rect.height / 2f;

        // Create new textures for each quadrant and assign them to sprites
        Texture2D[] quadTextures = new Texture2D[4];

        // Top-left
        quadTextures[0] = new Texture2D((int)halfWidth, (int)halfHeight);
        quadTextures[0].SetPixels(tex.GetPixels((int)rect.x, (int)(rect.y + halfHeight), (int)halfWidth, (int)halfHeight));
        quadTextures[0].Apply();

        // Top-right
        quadTextures[1] = new Texture2D((int)halfWidth, (int)halfHeight);
        quadTextures[1].SetPixels(tex.GetPixels((int)(rect.x + halfWidth), (int)(rect.y + halfHeight), (int)halfWidth, (int)halfHeight));
        quadTextures[1].Apply();

        // Bottom-left
        quadTextures[2] = new Texture2D((int)halfWidth, (int)halfHeight);
        quadTextures[2].SetPixels(tex.GetPixels((int)rect.x, (int)rect.y, (int)halfWidth, (int)halfHeight));
        quadTextures[2].Apply();

        // Bottom-right
        quadTextures[3] = new Texture2D((int)halfWidth, (int)halfHeight);
        quadTextures[3].SetPixels(tex.GetPixels((int)(rect.x + halfWidth), (int)rect.y, (int)halfWidth, (int)halfHeight));
        quadTextures[3].Apply();

        // Create sprites from new textures
        Sprite[] quads = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            quads[i] = Sprite.Create(quadTextures[i], new Rect(0, 0, quadTextures[i].width, quadTextures[i].height), new Vector2(0.5f, 0.5f), original.pixelsPerUnit);
        }

        return quads;
    }
}