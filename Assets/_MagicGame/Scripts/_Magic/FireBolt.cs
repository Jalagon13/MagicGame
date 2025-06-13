using System.Collections;
using UnityEngine;

public class FireBolt : ServerSpell
{
    [SerializeField] 
    private float _velocityDecay = 5f;

    private Rigidbody2D _rigidbody2D;
    private Vector2 _velocity;

    public override void OnSpellInitialize()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    protected override void OnSpellExecute()
    {
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        _velocity = _finalDirection * SpellData.Value.Speed;
    }

    protected override void OnFixedUpdateSpell()
    {
        _velocity = Vector2.Lerp(_velocity, Vector2.zero, _velocityDecay * Time.fixedDeltaTime);
        _rigidbody2D.linearVelocity = _velocity;
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

    public override void ClientSpellStart(ClientSpell clientSpell)
    {
        SpellItemSO spellItemSO = GameManager.Instance.GetItemSOFromItemId(SpellData.Value.SpellItemId) as SpellItemSO;
        SoundManager.Instance.PlayOneShot(spellItemSO.SpellCastSound, transform.position);
        clientSpell.Visualization.SetActive(true);
    }
    
    public override void ClientSpellUpdate(ClientSpell clientSpell)
    {
        
    }
    
    public override void ClientSpellStop(ClientSpell clientSpell)
    {
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
