using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "New Swing Spell", menuName = "Create Item/New Swing Spell")]
public class SwingSpellItemSO : SpellItemSO
{
    [field: Header("Swing Spell Paramters")]
    [field: SerializeField] public float SwingDuration { get; private set; } = 0.175f;
    [field: SerializeField] public ParticleSystem SwingVFX { get; private set; }
    [field: SerializeField] public EventReference SwingSound { get; private set; }

    public override void StartSpell(int slotIndex)
    {
        SpellManager.Instance.SubtractManaAndSetCooldown(this);
        Player.LocalClientInstance.PlayerHand.ExecuteSwing(SwingDuration, GameManager.Instance.GetItemIdFromItemSO(this));
        
        var go = Instantiate(SwingVFX, Player.LocalClientInstance.transform.position + Vector3.up * 0.5f, Quaternion.identity);
        go.transform.SetParent(Player.LocalClientInstance.transform);
    }
}
