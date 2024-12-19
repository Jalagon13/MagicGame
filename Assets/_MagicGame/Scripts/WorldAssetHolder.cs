using System;
using UnityEngine;

public class WorldAssetHolder : MonoBehaviour
{
	private void Start()
	{
		AssetManager.Instance.OnWorldAssetSpawned += AssetManager_OnWorldAssetSpawned;
		AssetManager.Instance.OnClearAllEnvironmentObjects += AssetManager_OnClearAllEnvironmentObjects;
	}

	private void AssetManager_OnClearAllEnvironmentObjects(object sender, EventArgs e)
	{
		// Loop through all the children of this GameObject
		for (int i = transform.childCount - 1; i >= 0; i--)
		{
			// Get the child GameObject
			Transform child = transform.GetChild(i);
        
			// Try to get the WorldObject component
			WorldObject worldObject = child.GetComponent<WorldObject>();
			if (worldObject != null)
			{
				// Call the DestroySelf() method
				worldObject.DestroySelf();
			}
			else
			{
				Debug.LogWarning($"Child {child.name} does not have a WorldObject component and was not destroyed.");
			}
		}
	}

	private void AssetManager_OnWorldAssetSpawned(object sender, AssetManager.OnWorldAssetSpawnedEventArgs e)
	{
		e.WorldAssetGameObject.transform.SetParent(transform);
	}

	private void OnDestroy()
	{
		AssetManager.Instance.OnWorldAssetSpawned -= AssetManager_OnWorldAssetSpawned;
		AssetManager.Instance.OnClearAllEnvironmentObjects += AssetManager_OnClearAllEnvironmentObjects;
	}
}
