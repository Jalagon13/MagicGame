using System;
using UnityEngine;
using UnityEngine.UI;

public class LightmapOverlay : MonoBehaviour
{
	[SerializeField] private ComputeShader _lightmapComputeShader;

	private RawImage _lightMapRawImage;
	private RenderTexture _lightmapRenderTexture;

	private void Awake()
	{
		_lightMapRawImage = GetComponent<RawImage>();
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

		// Update the RectTransform
		RectTransform overlayRect = _lightMapRawImage.rectTransform;
		overlayRect.position = centerWorldPos; // Center position in world space
		overlayRect.sizeDelta = sizeWorld; // Set the scaled size in world units
		overlayRect.localScale = Vector3.one; // Keep scale uniform
		
		
		
		// Update the RenderTexture size dynamically based on the tile bounds
		int renderTextureWidth = (maxLoadedTilePos.x - minLoadedTilePos.x);
		int renderTextureHeight = (maxLoadedTilePos.y - minLoadedTilePos.y);
		Debug.Log($"RenderTexture size: {renderTextureWidth}x{renderTextureHeight}");
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
		
		int kernelIndex = _lightmapComputeShader.FindKernel("CSMain");
		_lightmapComputeShader.SetTexture(kernelIndex, "Result", _lightmapRenderTexture);
		
		_lightmapComputeShader.SetInt("Width", renderTextureWidth);
		_lightmapComputeShader.SetInt("Height", renderTextureHeight);
		
		int threadGroupsX = Mathf.CeilToInt((float)renderTextureWidth / 8f);
		int threadGroupsY = Mathf.CeilToInt((float)renderTextureHeight / 8f);
		_lightmapComputeShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);
		
		_lightMapRawImage.texture = _lightmapRenderTexture;
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