using UnityEngine;
using FMODUnity;

namespace ProjectWizard
{
    public class FMODEvents : MonoBehaviour
    {
        public static FMODEvents Instance { get; private set; }

        [field: Header("Player SFX")]
        [field: SerializeField] public EventReference PlayerFootsteps { get; private set; }
        [field: SerializeField] public EventReference PlayerMeleeSwing { get; private set; }
        [field: SerializeField] public EventReference ItemPickup { get; private set; }
        [field: SerializeField] public EventReference GoldPickup { get; private set; }
        [field: SerializeField] public EventReference InventorySlotClicked { get; private set; }
        [field: SerializeField] public EventReference FocusSlotChanged { get; private set; }
        [field: SerializeField] public EventReference InventoryOpen { get; private set; }
        [field: SerializeField] public EventReference InventoryClose { get; private set; }
        [field: SerializeField] public EventReference PlayerDamaged { get; private set; }

        [field: Header("Tool SFX")]
        [field: SerializeField] public EventReference WandCast { get; private set; }
        [field: SerializeField] public EventReference MeleeHit { get; private set; }

        [field: Header("Environment SFX")]
        [field: SerializeField] public EventReference Ambience { get; private set; }
        [field: SerializeField] public EventReference MobSquash { get; private set; }


        private void Awake()
        {
            Instance = this;
        }
    }
}
