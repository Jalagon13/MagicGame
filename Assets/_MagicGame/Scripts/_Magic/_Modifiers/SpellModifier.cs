using Unity.Netcode;
using UnityEngine;

public abstract class SpellModifier : NetworkBehaviour
{
    public abstract SyncSpellData ModifiySpellData(SyncSpellData spellData, ServerSpell serverSpell = null);
}
