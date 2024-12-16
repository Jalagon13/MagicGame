using System.Collections;
using System.Collections.Generic;
using System.Text;
using MoreMountains.Tools;
using Pathfinding;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "New Deployable", menuName = "Create Item/New Deployable")]
public class DeployItemSO : ItemSO
{
    [SerializeField] private WorldObject _deployObjectPrefab;
    [SerializeField] private AudioClip _deploySound;
    [EnumFlag]
    [SerializeField] private WorldManager.EnvironmentID _environmentsAbleToSpawnIn;
	
    public override void ExecutePrimaryAction()
    {
        Vector2 pos = ActionManager.MouseWorldPosition;
		
        if(CanSpawnInActiveEnvironment(_environmentsAbleToSpawnIn))
        {
            if(IsClear(pos) && ActionManager.Instance.PlayerInRangeOfMouse())
            {
                Vector2Int spawnPosition = new(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
				
                AssetManager.Instance.PlaceResourceAsset(spawnPosition, _deployObjectPrefab);
			
                InventoryManager.Instance.RemoveItem(this, 1); // Note to future self: This implementation is bugged and will need fixing later
		
                MMSoundManagerSoundPlayEvent.Trigger(_deploySound, MMSoundManager.MMSoundManagerTracks.Sfx, default);
            }
        }
        else
        {
            Debug.LogWarning("Can't spawn in this environment");
        }
		
    }

    public override void ExecuteSecondaryAction()
    {
		
    }
	
    private bool CanSpawnInActiveEnvironment(WorldManager.EnvironmentID id)
    {
        return id == WorldManager.Instance.GetActiveEnvironmentID();
    }
	
    public override string GetDescription()
    {
        StringBuilder description = new();
        description.Append($"Left Click to place<br>");
        description.Append($"{GetDescriptionBreak()}");
	
        return description.ToString();
    }
	
    private bool IsClear(Vector2 position)
    {
        Vector2 positionCheck = new(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        var colliders = Physics2D.OverlapBoxAll(positionCheck + new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0);

        foreach(Collider2D col in colliders)
        {
            if(col.TryGetComponent(out ResourceObject clickable)) 
                return false;
        }

        return true;
    }
}