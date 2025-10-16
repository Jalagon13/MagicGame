using Sirenix.OdinInspector;
using UnityEngine;

namespace ProjectTinker
{
    [CreateAssetMenu()]
    public class NoiseMapSO : ScriptableObject
    {
        [Tooltip("Width of the generated noise map in pixels.")]
        public int Width = 256;
        [Tooltip("Height of the generated noise map in pixels.")]
        public int Height = 256;
        [Tooltip("Scale of the noise map. A lower scale value will zoom in on the noise, resulting in larger, smoother regions.")]
        public float Scale = 20f;
        [Tooltip("Number of octaves used for the noise generation, controlling the detail level.")]
        [Range(1, 10)]
        public int Octaves = 1;
        [Tooltip("Persistence controls the amplitude of each octave, affecting the roughness of the noise.")]
        [Range(0.01f, 1f)]
        public float Persistence = 0.5f;
        [Tooltip("Lacunarity controls the frequency of each octave, influencing the frequency of the noise.")]
        [Range(1f, 3.5f)]
        public float Lacunarity = 2f;
        public bool UseRedistribution;
        [Range(0.01f, 10f)]
        [Tooltip("Controls the floor height of the map")]
        [ShowIf("UseRedistribution")]
        public float Redistribution = 1; // Controls how high the floor is for the map
        public bool UseFallOffFunction = false;
        [Range(0, 1f)]
        [Tooltip("Controls the blend between the noise and its fall off function")]
        [ShowIf("UseFallOffFunction")]
        public float Mix = 1; // How defined the island is according to its distance fall off function
        [Tooltip("Absolute value of perlin value to get billow effect")]
        public bool UseBillowed = false;
        [Tooltip("Whether to apply a smoothstep effect to the noise")]
        public bool UseSmoothStep = false; // New boolean for smoothstep effect

        [HideInInspector]
        public Texture2D NoiseTexture;

        private void OnValidate()
        {
            GenerateNoiseTexture("Editor Test Seed");
        }

        public void GenerateNoiseTexture(string seed)
        {
            // Initialize the noiseTexture with the specified width and height.
            NoiseTexture = new Texture2D(Width, Height);

            // Generate a random seed using the current time, ensuring different results each time the noise is generated.
            System.Random random = new System.Random(seed.GetHashCode());

            // Generate random offsets to apply to the Perlin noise, adding variety to the generated noise.
            float offsetX = (float)random.NextDouble() * 10000f;
            float offsetY = (float)random.NextDouble() * 10000f;

            // Iterate over each pixel in the texture to generate the Perlin noise values.
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < Octaves; i++)
                    {
                        float sampleX = (x + offsetX) / Scale * frequency;
                        float sampleY = (y + offsetY) / Scale * frequency;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // Normalize to -1 to 1

                        if (UseBillowed)
                        {
                            perlinValue = Mathf.Abs(perlinValue);
                        }

                        noiseHeight += perlinValue * amplitude;

                        amplitude *= Persistence;
                        frequency *= Lacunarity;
                    }

                    // Sample the Perlin noise at the calculated coordinates.
                    float sample = (noiseHeight + 1) / 2;  // Normalize to 0 to 1

                    // Apply SmoothStep if enabled
                    if (UseSmoothStep)
                    {
                        sample = SmoothStep(sample);
                    }

                    // (Optional) Calculate normalized coordinates and determine distance with a distance function
                    if (UseFallOffFunction)
                    {
                        float nx = 2f * x / Width - 1f;
                        float ny = 2f * y / Height - 1f;
                        float d = 1f - (1f - nx * nx) * (1f - ny * ny); // Square Bump distance function
                        sample = Mathf.Lerp(sample, 1f - d, Mix);
                    }

                    // (Optional) Controls "floor" height of the map
                    if (UseRedistribution)
                    {
                        sample = Mathf.Pow(sample, Redistribution);
                    }

                    // Set the pixel color at the current position to the sampled value, creating a grayscale representation of the noise.
                    NoiseTexture.SetPixel(x, y, new Color(sample, sample, sample));
                }
            }

            // Apply the changes to the texture, finalizing the noise map generation.
            NoiseTexture.Apply();
        }

        // Smoothstep function
        private float SmoothStep(float value)
        {
            return value * value * (3f - 2f * value);  // Standard Smoothstep formula
        }
    }
}