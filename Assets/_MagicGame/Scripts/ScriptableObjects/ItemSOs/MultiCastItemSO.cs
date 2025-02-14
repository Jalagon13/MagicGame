using UnityEngine;

[CreateAssetMenu(fileName = "New Multi-cast item", menuName = "Create Item/New Multi-cast item")]
public class MultiCastItemSO : MagicItemSO
{
	[field: Tooltip("Number of spells to cast")]
	[field: SerializeField] public int MultiCastAmount { get; private set; }
}
