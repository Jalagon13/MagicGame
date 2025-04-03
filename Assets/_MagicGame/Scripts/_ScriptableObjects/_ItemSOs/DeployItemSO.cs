using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using FMODUnity;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "New Deployable", menuName = "Create Item/New Deployable")]
public class DeployItemSO : ItemSO
{
	public static bool PlacedThisFrameFlag = false;

	[SerializeField] private WorldObject _deployObjectPrefab;
	
	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		Vector2 pos = ActionManager.MouseWorldPosition;
		
		if(IsClear(pos) && PlayerInRangeOfMouse() && !TileManager.Instance.WallTm.HasTile(new(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y))))
		{
			CardinalDirection orientation = Player.LocalClientInstance.StateMachine.FacingDirection;
			Vector2Int spawnPosition = new(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
			BiomeType biome = Player.LocalClientInstance.CurrentPlayerBiome.Value;
			int id = GameManager.Instance.GetIDFromWorldObject(_deployObjectPrefab);

			ObjectManager.Instance.PlaceResourceObjectServerRpc(spawnPosition, id, biome, orientation);
			InventoryManager.Instance.RemoveItem(this, 1); // Note to future self: This implementation is bugged and will need fixing later
			SoundManager.Instance.PlayOneShot(_deployObjectPrefab.PlaceSound, Player.LocalClientInstance.transform.position);
			PlacedThisFrameFlag = true;
		}
		
		return _baseActionCooldown;
	}

    private bool PlayerInRangeOfMouse()
	{
		return Vector2.Distance(Player.LocalClientInstance.transform.position, ActionManager.MouseWorldPosition) <= 3;
	}
	
	public override string GetDescription()
	{
		StringBuilder description = new();
		description.Append($"Left Click to place<br>");
		description.Append($"{GetDescriptionBreak()}");
	
		return description.ToString();
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}
	
	private bool IsClear(Vector2 position)
	{
		Vector2 positionCheck = new(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
		var colliders = Physics2D.OverlapBoxAll(positionCheck + new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0);

		foreach(Collider2D col in colliders)
		{
			if(col.TryGetComponent(out WorldObject clickable) || col.TryGetComponent(out Npc npc)) 
				return false;
		}

		return true;
	}
	
	public WorldObject GetDeployObjectPrefab()
	{
		return _deployObjectPrefab;
	}
}