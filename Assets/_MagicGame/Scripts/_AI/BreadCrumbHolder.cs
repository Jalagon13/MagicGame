using System;
using UnityEngine;


namespace ProjectTinker
{
	public class BreadCrumbHolder : MonoBehaviour
	{
	    private void Start()
	    {
	        GameManager.Instance.OnSpawnBreadCrumbPrefab += PlacePrefabOnHolder;
	    }

	    private void PlacePrefabOnHolder(object sender, GameManager.BreadCrumbEventArgs e)
	    {
	        e.SpawnedBreadCrumbPrefab.transform.SetParent(transform);
	    }

	    private void OnDestroy()
	    {
	        GameManager.Instance.OnSpawnBreadCrumbPrefab -= PlacePrefabOnHolder;
	    }
	}

}