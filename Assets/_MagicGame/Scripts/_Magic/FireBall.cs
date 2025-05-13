using UnityEngine;

public class FireBall : Spell
{
    [field: Header("Fireball")]
    [field: SerializeField] public float VelocityDecay { get; private set; } = 5f;
    
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

    public override void OnOwnerSpellEnd()
    {
        base.OnOwnerSpellEnd();
        Debug.Log($"Stopping particles");
        foreach (Transform child in Visualization.transform)
        {
            ParticleSystem ps = child.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
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
