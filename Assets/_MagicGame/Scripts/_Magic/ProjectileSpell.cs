using System.Collections;
using UnityEngine;

public abstract class ProjectileSpell : ServerSpell
{
    protected Rigidbody2D _rigidbody2D;
    
    [HideInInspector]
    public Vector2 Velocity;

    public override void OnSpellInitialize()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    protected override void OnSpellExecute()
    {
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        Velocity = _finalDirection * SpellData.Value.Speed;
    }

    protected override void OnFixedUpdateSpell()
    {
        Velocity = CalculateVelocity(Velocity);
        _rigidbody2D.linearVelocity = Velocity;
    }

    protected override IEnumerator OnSpellEnd()
    {
        float longestParticleLifetime = 0;

        foreach (Transform child in ClientSpell.Visualization.transform)
        {
            ParticleSystem ps = child.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                longestParticleLifetime = Mathf.Max(longestParticleLifetime, ps.main.startLifetime.constant);
            }
        }

        yield return new WaitForSeconds(longestParticleLifetime);
    }

    protected abstract Vector2 CalculateVelocity(Vector2 currentVelocity);

    public override void ClientSpellStart(ClientSpell clientSpell)
    {
        clientSpell.Visualization.SetActive(true);
        
        SpellItemSO spellItemSO = GameManager.Instance.GetItemSOFromItemId(SpellData.Value.SpellItemId) as SpellItemSO;
        SoundManager.Instance.PlayOneShot(spellItemSO.SpellCastSound, transform.position);
    }
    
    public override void ClientSpellUpdate(ClientSpell clientSpell)
    {
        // Implement client-side update logic if needed
    }
    
    public override void ClientSpellStop(ClientSpell clientSpell)
    {
        // Implement client-side stop logic if needed
        foreach (Transform child in clientSpell.Visualization.transform)
        {
            ParticleSystem ps = child.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}