using UnityEngine;

public class FireBall : Spell
{
    [field: Header("Fireball")]
    [field: SerializeField] public float VelocityDecay { get; private set; } = 5f;
    [field: SerializeField] public ParticleSystem Trail { get; private set; }
    
    private Rigidbody2D _rigidbody2D;

    protected override void Awake()
    {
        base.Awake();

        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    protected override void OnOwnerExecuteSpellStart()
    {
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;

        if (IsOwner)
        {
            Velocity.Value = _finalDirection * SpellData.Value.Speed;
        }
    }

    protected override void OnStopped()
    {
        if (Trail != null)
        {
            Trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void FixedUpdate()
    {
        if(IsOwner)
        {
            Velocity.Value = Vector2.Lerp(Velocity.Value, Vector2.zero, VelocityDecay * Time.fixedDeltaTime);
            _rigidbody2D.linearVelocity = Velocity.Value;
        }
    }
}
