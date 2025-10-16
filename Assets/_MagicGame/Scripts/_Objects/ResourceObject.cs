using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using MoreMountains.Feedbacks;
using UnityEngine;


namespace ProjectTinker
{
	[SelectionBase]
	public class ResourceObject : MonoBehaviour // Base class for every "physical" asset in the world
	{	
		[SerializeField] 
		private ResourceDataSO _resourceData;
		public ResourceDataSO Data => _resourceData;
	
		[SerializeField] 
		private ResourceFeedbacks _resourceFeedback;
		public ResourceFeedbacks ResourceFeedbacks => _resourceFeedback;
	
		protected CardinalDirection _orientation;


		private void Awake()
		{
			transform.GetChild(0).gameObject.SetActive(!_resourceData.PassThrough); // Disable local collider so player can walk through it
		}
	
		public virtual void SetOrientation(CardinalDirection orientation)
		{
			_orientation = orientation;
		}
	
		protected bool PlayerInRangeOfPosition(Vector2 position)
		{
			return Vector2.Distance(Player.Instance.transform.position, position) <= ResourceDataSO.InteractDistance;
		}
	
		public void SpawnItems(Vector2 resourcePosition, BiomeType biome)
		{
			Debug.Log($"Item spawn logic local to resource object");
			LootTable.SpawnLoot(_resourceData.Table, resourcePosition + (Vector2.one * 0.5f), biome, default, 1f);
		}
	}

}