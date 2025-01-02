using System;
using UnityEngine;

public class WorldObjectHolder : MonoBehaviour
{
	private void Start()
	{
		ObjectManager.Instance.OnWorldObjectSpawned += ObjectManager_OnWorldObjectSpawned;
		ObjectManager.Instance.OnClearAllEnvironmentObjects += ObjectManager_OnClearAllEnvironmentObjects;
	}

	private void ObjectManager_OnClearAllEnvironmentObjects(object sender, EventArgs e)
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

	private void ObjectManager_OnWorldObjectSpawned(object sender, ObjectManager.OnWorldAssetSpawnedEventArgs e)
	{
		e.WorldObjectGameObject.transform.SetParent(transform);
	}

	private void OnDestroy()
	{
		ObjectManager.Instance.OnWorldObjectSpawned -= ObjectManager_OnWorldObjectSpawned;
		ObjectManager.Instance.OnClearAllEnvironmentObjects += ObjectManager_OnClearAllEnvironmentObjects;
	}
}
