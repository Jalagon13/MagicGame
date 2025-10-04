using System;
using UnityEngine;


namespace ProjectWizard
{
	public class ResourceObjectHolder : MonoBehaviour
	{
		private void Start()
		{
			ResourceManager.Instance.OnWorldObjectSpawned += ObjectManager_OnWorldObjectSpawned;
			ResourceManager.Instance.OnClearAllEnvironmentObjects += ObjectManager_OnClearAllEnvironmentObjects;
		}

		private void ObjectManager_OnClearAllEnvironmentObjects(object sender, EventArgs e)
		{
			// Loop through all the children of this GameObject
			for (int i = transform.childCount - 1; i >= 0; i--)
			{
				// Get the child GameObject
				Transform child = transform.GetChild(i);
        
				// Try to get the WorldObject component
				ResourceObject resourceObject = child.GetComponent<ResourceObject>();
				if (resourceObject != null)
				{
					// Call the DestroySelf() method
					// NTFS: This might be bugged
					Destroy(resourceObject.gameObject);
				}
				else
				{
					Debug.LogWarning($"Child {child.name} does not have a WorldObject component and was not destroyed.");
				}
			}
		}

		private void ObjectManager_OnWorldObjectSpawned(object sender, ResourceManager.OnWorldAssetSpawnedEventArgs e)
		{
			e.WorldObjectGameObject.transform.SetParent(transform);
		}

		private void OnDestroy()
		{
			ResourceManager.Instance.OnWorldObjectSpawned -= ObjectManager_OnWorldObjectSpawned;
			ResourceManager.Instance.OnClearAllEnvironmentObjects += ObjectManager_OnClearAllEnvironmentObjects;
		}
	}

}