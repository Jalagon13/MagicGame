using UnityEngine;

public class FireBall : Spell
{
    [field: SerializeField] public float VelocityDecay { get; private set; } = 5f;
    
    private float _startTime;
    private Rigidbody2D _rigidbody2D;
    private Vector2 _finalSpeed;

    protected override void Awake()
    {
        base.Awake();

        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    public override void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
    {
        base.ExecuteSpellStart(finalDirection, spawnPoint);

        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        _startTime = Time.time;

        if (IsOwner)
        {
            _finalSpeed = _finalDirection * SpellData.Value.Speed;
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!Started.Value || !IsOwner || _isDead) return; //don't do anything before OnNetworkSpawn has run.

        float t = Mathf.Clamp01((Time.time - _startTime) / SpellData.Value.Lifetime);
        Velocity.Value = Vector2.Lerp(Vector2.zero, _finalSpeed, t);
        _rigidbody2D.linearVelocity = Velocity.Value;
    }
}
