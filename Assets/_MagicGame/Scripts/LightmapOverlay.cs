using System;
using UnityEngine;
using UnityEngine.UI;

public class LightmapOverlay : MonoBehaviour
{
	[SerializeField] private RenderTexture _lightmapRenderTexture; // RenderTexture for the lightmap

	private RawImage _lightMapRawImage;

	private void Awake()
	{
		_lightMapRawImage = GetComponent<RawImage>();
	}

	private void Start()
	{
		// Create and set up the RawImage for displaying the RenderTexture
		CreateLightmapOverlay();

		// Subscribe to the event to update overlay bounds
		ChunkManager.Instance.OnLoadedPlayerChunksUpdated += ChunkManager_OnLoadedPlayerChunksUpdated;
	}

	private void CreateLightmapOverlay()
	{
		_lightMapRawImage.texture = _lightmapRenderTexture;
		_lightMapRawImage.color = new Color(1, 1, 1, 0.5f); // Semi-transparent white
	}

	private void ChunkManager_OnLoadedPlayerChunksUpdated(object sender, ChunkManager.OnActiveChunksUpdatedEventArgs e)
	{
		// Get the bounds of the loaded tiles
		Vector2Int minLoadedTilePos = e.MinLoadedTilePos;
		Vector2Int maxLoadedTilePos = e.MaxLoadedTilePos;

		// Update the RenderTexture size dynamically based on the tile bounds
		int renderTextureWidth = (maxLoadedTilePos.x + 1 - minLoadedTilePos.x) * 4;
		int renderTextureHeight = (maxLoadedTilePos.y + 1 - minLoadedTilePos.y) * 4;

		if (_lightmapRenderTexture != null)
		{
			_lightmapRenderTexture.Release();
		}

		_lightmapRenderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 1)
		{
			enableRandomWrite = true,
			filterMode = FilterMode.Point
		};
		_lightmapRenderTexture.Create();
		_lightMapRawImage.texture = _lightmapRenderTexture;

		// Convert tile positions to world space
		Vector2 tileWorldSize = GetTileWorldSize();
		Vector2 minWorldPos = TileToWorldPosition(minLoadedTilePos, tileWorldSize);
		Vector2 maxWorldPos = TileToWorldPosition(maxLoadedTilePos, tileWorldSize);

		// Calculate center and size in world space
		Vector2 centerWorldPos = (minWorldPos + maxWorldPos) / 2; // Center of the overlay
		Vector2 sizeWorld = new Vector2(
			maxWorldPos.x - minWorldPos.x,
			maxWorldPos.y - minWorldPos.y
		);

		// Multiply world size by 4 to ensure it matches the RenderTexture scaling
		Vector2 scaledWorldSize = sizeWorld * 1;

		// Update the RectTransform
		RectTransform overlayRect = _lightMapRawImage.rectTransform;
		overlayRect.position = centerWorldPos; // Center position in world space
		overlayRect.sizeDelta = scaledWorldSize; // Set the scaled size in world units
		overlayRect.localScale = Vector3.one; // Keep scale uniform
	}

	private Vector2 GetTileWorldSize()
	{
		// Replace with your actual tile size in world units
		return new Vector2(1f, 1f); // Assuming tiles are 1x1 units
	}

	private Vector2 TileToWorldPosition(Vector2Int tilePos, Vector2 tileSize)
	{
		// Convert tile coordinates to world position
		return new Vector2(tilePos.x * tileSize.x, tilePos.y * tileSize.y);
	}

	private void OnDestroy()
	{
		// Unsubscribe from the event
		ChunkManager.Instance.OnLoadedPlayerChunksUpdated -= ChunkManager_OnLoadedPlayerChunksUpdated;
	}
}