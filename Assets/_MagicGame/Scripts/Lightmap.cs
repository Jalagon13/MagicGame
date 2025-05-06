using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class Lightmap : MonoBehaviour
{
	public static Lightmap Instance { get; private set; }

	[SerializeField] private Gradient _dayLightGradient;
	[SerializeField] private ComputeShader _lightmapComputeShader;
	[SerializeField] private int _lightmapScale = 1;
	[SerializeField] private bool _usePointFilter;
	[SerializeField] private bool _enableDayNightCycle;

	private RawImage _lightMapRawImage;
	private RenderTexture _lightmapRenderTexture;
	private RectTransform _overlayRect;
	private Vector2 _tileWorldSize = new Vector2(1f, 1f); // Assuming tiles are 1x1 units
	private Vector2Int _minLoadedTilePos;
	private Vector2Int _maxLoadedTilePos;
	private List<LightSource> _lightSources = new List<LightSource>();

	private void Awake()
	{
		Instance = this;
	
		_lightMapRawImage = transform.GetChild(0).GetComponent<RawImage>();
		_overlayRect = _lightMapRawImage.rectTransform;
	}

	private void Start()
	{
		WorldManager.Instance.OnTick += WorldManager_OnTick;
	}

	private void WorldManager_OnTick(object sender, WorldManager.OnTickEventArgs e)
	{
		float ratio = e.CurrentDayRatio;
		
		if(Player.LocalClientInstance == null) return;
		
		// NTFS: Does not do anything rn
		Color dayLightColor = _dayLightGradient.Evaluate(ratio);
		
		if(_enableDayNightCycle)
		{
			_lightMapRawImage.color = Player.LocalClientInstance.CurrentPlayerBiome.Value == BiomeType.Forest ? SetColorBasedOnBrightness(dayLightColor) : Color.white;
		}
	}
	
	// Method to set color and adjust opacity
	public Color SetColorBasedOnBrightness(Color color)
	{
		// Calculate the brightness of the color (0 = darkest, 1 = brightest)
		float brightness = CalculateBrightness(color);

		// Map brightness to opacity (alpha) - you can adjust this formula
		float opacity = 1 - brightness;

		// Set the sprite renderer's color with the new alpha value
		Color spriteColor = Color.white;
		spriteColor.a = Mathf.Clamp(opacity, 0, 1f);
		
		return spriteColor;
	}
	
	// Method to calculate perceived brightness
	private float CalculateBrightness(Color color)
	{
		// The formula for perceived brightness:
		// 0.2126 * R + 0.7152 * G + 0.0722 * B
		// These weights are based on human perception of color brightness
		return 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
	}

	public void RegisterLightSource(LightSource lightSource)
	{
		if(!_lightSources.Contains(lightSource))
		{
			_lightSources.Add(lightSource);
			
			UpdateLightMap();
		}
	}
	
	public void DeregisterLightSource(LightSource lightSource)
	{
		if(_lightSources.Contains(lightSource))
		{
			_lightSources.Remove(lightSource);
			
			UpdateLightMap();
		}
	}
	
	public void UpdateLightMap()
	{
		if(_lightmapRenderTexture == null || WorldManager.Instance.IsLoadingBiome) return;

		UpdateOverlayRect();
		UpdateRenderTexture();
		DispatchComputeShader();
	}
	
	public void UpdateLightMapBounds(Vector2Int minLoadedTilePos, Vector2Int maxLoadedTilePos)
	{
		if(!WorldManager.Instance.IsTicking()) return;
		
		_minLoadedTilePos = minLoadedTilePos;
		_maxLoadedTilePos = maxLoadedTilePos;
		
		UpdateOverlayRect();
		UpdateRenderTexture();
		DispatchComputeShader();
	}

	private void UpdateOverlayRect()
	{
		// Convert tile positions to world space
		Vector2 minWorldPos = TileToWorldPosition(_minLoadedTilePos);
		Vector2 maxWorldPos = TileToWorldPosition(_maxLoadedTilePos);

		// Calculate center and size in world space
		Vector2 centerWorldPos = (minWorldPos + maxWorldPos) / 2; // Center of the overlay
		Vector2 sizeWorld = new Vector2(maxWorldPos.x - minWorldPos.x, maxWorldPos.y - minWorldPos.y);

		// Update the RectTransform
		_overlayRect.position = centerWorldPos; // Center position in world space
		_overlayRect.sizeDelta = sizeWorld; // Set the scaled size in world units
		_overlayRect.localScale = Vector3.one; // Keep scale uniform
	}

	private void UpdateRenderTexture()
	{
		int renderTextureWidth = (_maxLoadedTilePos.x - _minLoadedTilePos.x) * _lightmapScale;
		int renderTextureHeight = (_maxLoadedTilePos.y - _minLoadedTilePos.y) * _lightmapScale;

		// Release old render texture if it exists
		if (_lightmapRenderTexture != null)
		{
			_lightmapRenderTexture.Release();
		}

		// Create a new render texture
		_lightmapRenderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 1)
		{
			enableRandomWrite = true,
			filterMode = _usePointFilter ? FilterMode.Point : FilterMode.Bilinear,
		};
		
		_lightmapRenderTexture.Create();
	}

	private void DispatchComputeShader()
	{
		int renderTextureWidth = _lightmapRenderTexture.width;
		int renderTextureHeight = _lightmapRenderTexture.height;
		int kernelIndex = _lightmapComputeShader.FindKernel("CSMain");

		// Create a list to hold the light data for all light sources
		List<Vector4> lightSourceList = CreateLightSourceGPUData();

		// Create and set structured buffer for light sources if there are any
		ComputeBuffer lightSourceBuffer = new ComputeBuffer(lightSourceList.Count == 0 ? 1 : lightSourceList.Count, sizeof(float) * 4);
		lightSourceBuffer.SetData(lightSourceList.ToArray());
		_lightmapComputeShader.SetBuffer(kernelIndex, "LightSources", lightSourceBuffer);

		// Set up the tile visibility array and compute buffer
		TileVisibility[] tileVisibilityArray = new TileVisibility[renderTextureWidth * renderTextureHeight];
		PopulateTileVisibilityArray(_minLoadedTilePos, _maxLoadedTilePos, _lightmapScale, tileVisibilityArray, renderTextureWidth);
		
		// Create and set the compute buffer for tile visibility
		ComputeBuffer tileDataBuffer = new ComputeBuffer(tileVisibilityArray.Length, sizeof(uint));
		tileDataBuffer.SetData(tileVisibilityArray);
		_lightmapComputeShader.SetBuffer(kernelIndex, "TileData", tileDataBuffer);

		// Set shader parameters
		_lightmapComputeShader.SetInt("Width", renderTextureWidth);
		_lightmapComputeShader.SetInt("Height", renderTextureHeight);
		_lightmapComputeShader.SetInt("OpaqueTileTolerance", _lightmapScale / 2);
		_lightmapComputeShader.SetInt("NumLights", lightSourceList.Count);
		_lightmapComputeShader.SetVector("BaseLight", GetBaseLight());
		// Set the output texture
		_lightmapComputeShader.SetTexture(kernelIndex, "Result", _lightmapRenderTexture);

		// Dispatch the compute shader
		int threadGroupsX = Mathf.CeilToInt((float)renderTextureWidth / 8f);
		int threadGroupsY = Mathf.CeilToInt((float)renderTextureHeight / 8f);
		_lightmapComputeShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);

		// Release buffers after use
		tileDataBuffer.Release();
		lightSourceBuffer.Release();

		// Set the texture on the RawImage component
		_lightMapRawImage.texture = _lightmapRenderTexture;
	}

	private Vector3 GetBaseLight()
	{
		// For now, hard code base environment for forest to be slightly dark and cave to be completely dark
		switch(Player.LocalClientInstance.CurrentPlayerBiome.Value)
		{
			case BiomeType.Forest:
				return new Vector3(0.01f, 0.01f, 0.01f);
			case BiomeType.Cave:
				// return new Vector3(0.01f, 0.01f, 0.01f);
				return new Vector3(0.0f, 0.0f, 0.0f);
			default:
				return new Vector3(0.01f, 0.01f, 0.01f);
		}
	}

	private List<Vector4> CreateLightSourceGPUData()
	{
		List<Vector4> lightSourceList = new List<Vector4>();
	
		// Iterate over all light sources and populate the lightSourceList
		foreach (var lightSource in _lightSources)
		{
			// Convert world position to texture coordinates
			Vector2 worldPosition = new Vector2(lightSource.transform.position.x, lightSource.transform.position.y);
			Vector2 lightTextureCoord = WorldToRenderTextureCoords(worldPosition);

			// Adjust light radius based on the lightmap scale (invert the scale to keep the radius consistent in world space)
			float adjustedLightRadius = lightSource.LightRadius * _lightmapScale;

			// Create light data (x, y position, intensity, adjusted radius)
			Vector4 lightData = new Vector4(lightTextureCoord.x, lightTextureCoord.y, lightSource.LightIntensity, adjustedLightRadius);

			// Add to the list
			lightSourceList.Add(lightData);
		}
		
		return lightSourceList;
	}
	
	public Vector2 WorldToRenderTextureCoords(Vector2 worldPos)
	{
		// Map world position to tile indices
		Vector2 tilePos = new Vector2(worldPos.x / _tileWorldSize.x, worldPos.y / _tileWorldSize.y);

		// Normalize to render texture coordinates
		float x = (tilePos.x - _minLoadedTilePos.x) / (_maxLoadedTilePos.x - _minLoadedTilePos.x);
		float y = (tilePos.y - _minLoadedTilePos.y) / (_maxLoadedTilePos.y - _minLoadedTilePos.y);

		// Scale to render texture dimensions
		Vector2 renderTextureCoord = new Vector2(x * _lightmapRenderTexture.width, y * _lightmapRenderTexture.height);

		// Snap to the nearest pixel
		renderTextureCoord.x = Mathf.Round(renderTextureCoord.x);
		renderTextureCoord.y = Mathf.Round(renderTextureCoord.y);

		return renderTextureCoord;
	}

	private void PopulateTileVisibilityArray(Vector2Int minLoadedTilePos, Vector2Int maxLoadedTilePos, int scale, TileVisibility[] tileVisibilityArray, int renderTextureWidth)
	{
		Tilemap wallTm = TileRenderManager.Instance.WallTm;

		// Build local visibility dictionary
		Dictionary<Vector3Int, TileVisibility> localVisibilityDict = new Dictionary<Vector3Int, TileVisibility>();

		for (int x = minLoadedTilePos.x; x < maxLoadedTilePos.x; x++)
		{
			for (int y = minLoadedTilePos.y; y < maxLoadedTilePos.y; y++)
			{
				Vector3Int tilePosition = new Vector3Int(x, y, 0);
				localVisibilityDict[tilePosition] = new TileVisibility(wallTm.HasTile(tilePosition) ? 1 : 0);
			}
		}

		// Define the bounds based on minLoadedTilePos and maxLoadedTilePos
		Vector2 minWorldPos = TileToWorldPosition(minLoadedTilePos);
		Vector2 maxWorldPos = TileToWorldPosition(maxLoadedTilePos);
		Rect bounds = new Rect(minWorldPos, maxWorldPos - minWorldPos); // Create a rectangle from the bounds

		// Use Physics2D.OverlapAreaAll to get colliders within the bounds
		Collider2D[] colliders = Physics2D.OverlapAreaAll(bounds.min, bounds.max);

		// Loop through all the colliders within the bounds
		foreach (Collider2D collider in colliders)
		{
			if(collider.TryGetComponent(out WorldObject worldObject) && !worldObject.PassThrough)
			{
				Vector3Int tilePosition = new Vector3Int(Mathf.FloorToInt(worldObject.transform.position.x), Mathf.FloorToInt(worldObject.transform.position.y), 0);
				localVisibilityDict[tilePosition] = new TileVisibility(1);
			}
		}

		foreach (var kvp in localVisibilityDict)
		{
			Vector3Int tilePosition = kvp.Key;
			TileVisibility visibility = kvp.Value;

			int relativeX = (tilePosition.x - minLoadedTilePos.x) * scale;
			int relativeY = (tilePosition.y - minLoadedTilePos.y) * scale;

			for (int y = 0; y < scale; y++)
			{
				for (int x = 0; x < scale; x++)
				{
					int index = (relativeY + y) * renderTextureWidth + (relativeX + x);
					if (index >= 0 && index < tileVisibilityArray.Length)
					{
						tileVisibilityArray[index] = visibility;
					}
				}
			}
		}
	}

	private Vector2 TileToWorldPosition(Vector2Int tilePos)
	{
		return new Vector2(tilePos.x * _tileWorldSize.x, tilePos.y * _tileWorldSize.y);
	}
	
	public RenderTexture GetRenderTexture()
	{
		return _lightmapRenderTexture;
	}
	
	public int GetLightmapScale()
	{
		return _lightmapScale;
	}

	private void OnDestroy()
	{
		WorldManager.Instance.OnTick -= WorldManager_OnTick;
	}
}