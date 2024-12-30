using System;
using UnityEngine;
using UnityEngine.UI;

public class Lightmap : MonoBehaviour
{
	[SerializeField] private ComputeShader _lightmapComputeShader;
	[SerializeField] private int _lightmapScale = 1;
	[SerializeField] private GameObject _testLight;

	private RawImage _lightMapRawImage;
	private RenderTexture _lightmapRenderTexture;
	private RectTransform _overlayRect;
	private Vector2 _tileWorldSize = new Vector2(1f, 1f); // Assuming tiles are 1x1 units

	private void Awake()
	{
		_lightMapRawImage = transform.GetChild(0).GetComponent<RawImage>();
		_overlayRect = _lightMapRawImage.rectTransform;
	}

	private void Start()
	{
		// Subscribe to the event to update overlay bounds
		ChunkManager.Instance.OnLoadedPlayerChunksUpdated += ChunkManager_OnLoadedPlayerChunksUpdated;
	}

	private void ChunkManager_OnLoadedPlayerChunksUpdated(object sender, ChunkManager.OnActiveChunksUpdatedEventArgs e)
	{
		// Get the bounds of the loaded tiles
		Vector2Int minLoadedTilePos = e.MinLoadedTilePos;
		Vector2Int maxLoadedTilePos = e.MaxLoadedTilePos;

		// Calculate center and size in world space
		UpdateOverlayRect(minLoadedTilePos, maxLoadedTilePos);

		// Update the RenderTexture size dynamically based on the tile bounds
		UpdateRenderTexture(minLoadedTilePos, maxLoadedTilePos);

		// Set up and dispatch the compute shader
		DispatchComputeShader(minLoadedTilePos, maxLoadedTilePos);
	}

	private void UpdateOverlayRect(Vector2Int minLoadedTilePos, Vector2Int maxLoadedTilePos)
	{
		// Convert tile positions to world space
		Vector2 minWorldPos = TileToWorldPosition(minLoadedTilePos);
		Vector2 maxWorldPos = TileToWorldPosition(maxLoadedTilePos);

		// Calculate center and size in world space
		Vector2 centerWorldPos = (minWorldPos + maxWorldPos) / 2; // Center of the overlay
		Vector2 sizeWorld = new Vector2(maxWorldPos.x - minWorldPos.x, maxWorldPos.y - minWorldPos.y);

		// Update the RectTransform
		_overlayRect.position = centerWorldPos; // Center position in world space
		_overlayRect.sizeDelta = sizeWorld; // Set the scaled size in world units
		_overlayRect.localScale = Vector3.one; // Keep scale uniform
	}

	private void UpdateRenderTexture(Vector2Int minLoadedTilePos, Vector2Int maxLoadedTilePos)
	{
		int renderTextureWidth = (maxLoadedTilePos.x - minLoadedTilePos.x) * _lightmapScale;
		int renderTextureHeight = (maxLoadedTilePos.y - minLoadedTilePos.y) * _lightmapScale;

		// Release old render texture if it exists
		if (_lightmapRenderTexture != null)
		{
			_lightmapRenderTexture.Release();
		}

		// Create a new render texture
		_lightmapRenderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 1)
		{
			enableRandomWrite = true,
			filterMode = FilterMode.Point
		};
		_lightmapRenderTexture.Create();
	}

	private void DispatchComputeShader(Vector2Int minLoadedTilePos, Vector2Int maxLoadedTilePos)
	{
		int renderTextureWidth = _lightmapRenderTexture.width;
		int renderTextureHeight = _lightmapRenderTexture.height;

		int kernelIndex = _lightmapComputeShader.FindKernel("CSMain");

		// Convert _testLight world position to texture coordinates
		Vector2 worldPosition = new Vector2(_testLight.transform.position.x, _testLight.transform.position.y);
		Vector2 lightTextureCoord = WorldToRenderTextureCoords(worldPosition, minLoadedTilePos, maxLoadedTilePos);

		// Set up light properties
		float lightIntensity = 1.0f; // Example value
		float lightRadius = 5.0f;    // Example value

		// Adjust light radius based on the lightmap scale (invert the scale to keep the radius consistent in world space)
		float adjustedLightRadius = lightRadius * _lightmapScale;

		// Create the light data (adjusted light radius)
		Vector4 lightData = new Vector4(lightTextureCoord.x, lightTextureCoord.y, lightIntensity, adjustedLightRadius);

		// Create and set structured buffers for light sources and colors
		Vector4[] lightSourceArray = new Vector4[1] { lightData };
		ComputeBuffer lightSourceBuffer = new ComputeBuffer(lightSourceArray.Length, sizeof(float) * 4);
		lightSourceBuffer.SetData(lightSourceArray);
		_lightmapComputeShader.SetBuffer(kernelIndex, "LightSources", lightSourceBuffer);
    
		// Set up the tile visibility array and compute buffer
		TileVisibility[] tileVisibilityArray = new TileVisibility[renderTextureWidth * renderTextureHeight];
		PopulateTileVisibilityArray(minLoadedTilePos, maxLoadedTilePos, _lightmapScale, tileVisibilityArray, renderTextureWidth);

		// Create and set the compute buffer
		ComputeBuffer tileDataBuffer = new ComputeBuffer(tileVisibilityArray.Length, sizeof(uint));
		tileDataBuffer.SetData(tileVisibilityArray);
		_lightmapComputeShader.SetBuffer(kernelIndex, "TileData", tileDataBuffer);

		// Set shader parameters
		_lightmapComputeShader.SetInt("Width", renderTextureWidth);
		_lightmapComputeShader.SetInt("Height", renderTextureHeight);
		_lightmapComputeShader.SetInt("NumLights", lightSourceArray.Length);

		// Set the output texture
		_lightmapComputeShader.SetTexture(kernelIndex, "Result", _lightmapRenderTexture);

		// Dispatch the compute shader
		int threadGroupsX = Mathf.CeilToInt((float)renderTextureWidth / 8f);
		int threadGroupsY = Mathf.CeilToInt((float)renderTextureHeight / 8f);
		_lightmapComputeShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);

		// Release buffer after use
		tileDataBuffer.Release();
		lightSourceBuffer.Release();

		// Set the texture on the RawImage component
		_lightMapRawImage.texture = _lightmapRenderTexture;
	}
	
	private Vector2 WorldToRenderTextureCoords(Vector2 worldPos, Vector2Int minTilePos, Vector2Int maxTilePos)
	{
		// Map world position to tile indices
		Vector2 tilePos = new Vector2(worldPos.x / _tileWorldSize.x, worldPos.y / _tileWorldSize.y);

		// Normalize to render texture coordinates
		float x = (tilePos.x - minTilePos.x) / (maxTilePos.x - minTilePos.x);
		float y = (tilePos.y - minTilePos.y) / (maxTilePos.y - minTilePos.y);

		// Scale to render texture dimensions
		return new Vector2(x * _lightmapRenderTexture.width, y * _lightmapRenderTexture.height);
	}

	private void PopulateTileVisibilityArray(Vector2Int minLoadedTilePos, Vector2Int maxLoadedTilePos, int scale, TileVisibility[] tileVisibilityArray, int renderTextureWidth)
	{
		foreach (var kvp in Environment.Instance.GetTileVisibilityDictionary())
		{
			Vector3Int tilePosition = kvp.Key;
			TileVisibility visibility = kvp.Value;

			// Calculate the relative position in the texture grid, considering the scale
			int relativeX = (tilePosition.x - minLoadedTilePos.x) * scale;
			int relativeY = (tilePosition.y - minLoadedTilePos.y) * scale;

			// Now we need to place the tileVisibility in the correct block of the RenderTexture
			for (int y = 0; y < scale; y++)
			{
				for (int x = 0; x < scale; x++)
				{
					// Find the correct index in the 1D texture array
					int index = (relativeY + y) * renderTextureWidth + (relativeX + x);
					tileVisibilityArray[index] = visibility;
				}
			}
		}
	}

	private Vector2 TileToWorldPosition(Vector2Int tilePos)
	{
		return new Vector2(tilePos.x * _tileWorldSize.x, tilePos.y * _tileWorldSize.y);
	}

	private void OnDestroy()
	{
		// Unsubscribe from the event
		ChunkManager.Instance.OnLoadedPlayerChunksUpdated -= ChunkManager_OnLoadedPlayerChunksUpdated;
	}
}