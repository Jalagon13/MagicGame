using UnityEngine;

public abstract class SpellModifier : MonoBehaviour
{
    public abstract SyncSpellData ModifiySpellData(SyncSpellData spellData);
}
