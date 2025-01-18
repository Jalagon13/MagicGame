using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
	public static FMODEvents Instance { get; private set; }
	
	[field: Header("Player SFX")]
	[field: SerializeField] public EventReference PlayerFootsteps { get; private set; }
	[field: SerializeField] public EventReference PlayerMeleeSwing { get; private set; }
	[field: SerializeField] public EventReference ItemPickup { get; private set; }
	[field: SerializeField] public EventReference InventorySlotClicked { get; private set; }
	[field: SerializeField] public EventReference FocusSlotChanged { get; private set; }
	
	[field: Header("Weapon SFX")]
	[field: SerializeField] public EventReference MeleeHit { get; private set; }
	
	[field: Header("NPC SFX")]
	[field: SerializeField] public EventReference PixieDamaged { get; private set; }
	
	private void Awake()
	{
		Instance = this;
	}
}
