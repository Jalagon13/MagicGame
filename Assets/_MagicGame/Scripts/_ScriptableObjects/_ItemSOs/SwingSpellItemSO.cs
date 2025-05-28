using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "New Swing Spell", menuName = "Create Item/New Swing Spell")]
public class SwingSpellItemSO : SpellItemSO
{
    [field: Header("Swing Spell Paramters")]
    [field: SerializeField] public float SwingDuration { get; private set; } = 0.175f;
    [field: SerializeField] public float DetectionBetweenHitsDuration { get; private set; } = 0.05f;
    [field: SerializeField] public float MeleeColliderLength { get; private set; } = 1f;
    [field: SerializeField] public SwingSpellVFX SwingSpellVFX { get; private set; }
    [field: SerializeField] public EventReference HitSound { get; private set; }
    [field: SerializeField] public EventReference SwingSound { get; private set; }

    public override void StartSpell(int slotIndex)
    {
        SpellManager.Instance.SubtractManaAndSetCooldown(this);
        Player.LocalClientInstance.PlayerHand.ExecuteSwing(SwingDuration, GameManager.Instance.GetItemIdFromItemSO(this));
    }
}
