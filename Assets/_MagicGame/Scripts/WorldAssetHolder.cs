using System;
using UnityEngine;

public class WorldAssetHolder : MonoBehaviour
{
    private void Start()
    {
        AssetManager.Instance.OnWorldAssetSpawned += AssetManager_OnWorldAssetSpawned;
    }

    private void AssetManager_OnWorldAssetSpawned(object sender, AssetManager.OnWorldAssetSpawnedEventArgs e)
    {
        e.WorldAssetGameObject.transform.SetParent(transform);
    }

    private void OnDestroy()
    {
        AssetManager.Instance.OnWorldAssetSpawned -= AssetManager_OnWorldAssetSpawned;
    }
}
