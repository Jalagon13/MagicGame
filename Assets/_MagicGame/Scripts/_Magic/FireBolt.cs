using System.Collections;
using UnityEngine;

public class FireBolt : ServerSpell
{
    [SerializeField] 
    private float _velocityDecay = 5f;

    private Rigidbody2D _rigidbody2D;
    private Vector2 _velocity;

    protected override void OnSpellInitialize()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        ClientSpell.Visualization.SetActive(false);
    }

    protected override void OnSpellExecute()
    {
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        _velocity = _finalDirection * SpellData.Speed;
        ClientSpell.Visualization.SetActive(true);
    }

    protected override void OnFixedUpdateSpell()
    {
        _velocity = Vector2.Lerp(_velocity, Vector2.zero, _velocityDecay * Time.fixedDeltaTime);
        _rigidbody2D.linearVelocity = _velocity;
    }

    protected override IEnumerator OnSpellEnd()
    {
        yield return null;
    }

    public override void ClientSpellStart(ClientSpell clientSpell)
    {
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
