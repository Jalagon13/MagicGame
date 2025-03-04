using UnityEngine;

public abstract class SpellModifier : MonoBehaviour
{
    protected Spell _spell;

    protected virtual void Awake()
    {
        _spell = transform.root.GetComponent<Spell>();
    }

    public abstract void Apply();
}