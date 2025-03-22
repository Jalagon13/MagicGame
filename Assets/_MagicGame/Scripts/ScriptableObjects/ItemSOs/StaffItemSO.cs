using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Tilemaps;
using System.Linq;

// Wand upgrades and upgrade data need to live in here and injected into WandInventoryItem somehow
// Level system must stay in WandInventoryItem and Wand upgrade data in here
[CreateAssetMenu(fileName = "staff_", menuName = "Create Item/New Staff")]
public class StaffItemSO : ItemSO
{
	[Tooltip("Power of each mining tick")]
	[field: SerializeField] public int MiningPower { get; private set; }
	[Tooltip("Mining speed / 60 = time between mining ticks")]
	[field: SerializeField] public int MiningSpeed { get; private set; }
	[field: SerializeField] public float MiningRange { get; private set; }

	private WorldObject _resourceObjectSelected;
	
	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		if(! PlayerInRangeOfMouse()) return _baseActionCooldown;
		
		Vector2Int mousePos = Vector2Int.FloorToInt(ActionManager.MouseWorldPosition);

		if (Environment.Instance.WallTm.HasTile((Vector3Int)mousePos))
		{
			Environment.Instance.HitWallTile(Player.LocalClientInstance.CurrentPlayerBiome.Value, mousePos, MiningPower);
			SoundManager.Instance.PlayOneShot(FMODEvents.Instance.WandCast, Player.LocalClientInstance.transform.position);
			
			return MiningSpeed / 60f;
		}
		else if (ObjectManager.Instance.TryToFindWorldObject(Vector2Int.FloorToInt(ActionManager.MouseWorldPosition), out WorldObject wo))
		{
			ObjectManager.Instance.HitObject(Player.LocalClientInstance.CurrentPlayerBiome.Value, wo, MiningPower);

			return MiningSpeed / 60f;
		}
		
		return _baseActionCooldown;
	}
	
	private bool PlayerInRangeOfMouse()
	{
		return Vector2.Distance(Player.LocalClientInstance.transform.position, ActionManager.MouseWorldPosition) <= MiningRange;
	}
	
	private bool GetResourceSelected()
	{
		Collider2D[] colliders = Physics2D.OverlapPointAll(ActionManager.MouseWorldPosition);
		List<WorldObject> resourceObjectsFound = new();

		if (colliders.Count() > 0)
		{
			foreach (Collider2D c in colliders)
			{
				if (c.TryGetComponent(out WorldObject resourceObject))
				{
					resourceObjectsFound.Add(resourceObject);
				}
			}
		}

		_resourceObjectSelected = resourceObjectsFound.Count > 0 ? resourceObjectsFound.Last() : null;
		
		return _resourceObjectSelected != null;
	}
	
	public override string GetDescription()
	{
		return Description;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}
}