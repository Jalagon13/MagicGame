using UnityEngine;

[CreateAssetMenu(fileName = "New Mining Item", menuName = "Create Item/New Mining Item")]
public class MiningFocusItemSO : SpellModItemSO
{
	[field: Tooltip("Actual Prefab for the mining visual.")]
	[field: SerializeField] public GameObject MiningVisualPrefab { get; private set; }
	
	[field: Tooltip("How much per tick to break stuff")]
	[field: SerializeField] public int MiningPower { get; private set; }
	
	[field: Tooltip("Range needed to mine.")]
	[field: SerializeField] public int MiningRange { get; private set; } = 4;

	[field: Tooltip("Delay after casting the spell.")]
	[field: SerializeField] public float CastDelay { get; private set; } = 4;

	public bool PlayerInRangeOfMouse()
	{
		return Vector2.Distance(Player.LocalClientInstance.transform.position, ActionManager.MouseWorldPosition) <= MiningRange;
	}
	
	public void SpawnMiningVisuals(Vector2 pos)
	{
		Instantiate(MiningVisualPrefab, pos, Quaternion.identity);
	}
}
