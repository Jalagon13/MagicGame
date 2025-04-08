using FMODUnity;
using UnityEngine;

public class LightDagger : Spell
{
    [field: SerializeField] public SpriteRenderer DaggerSprite { get; private set; }

    private Rigidbody2D _rigidbody2D;


    protected override void Awake()
    {
        base.Awake();

        _rigidbody2D = GetComponent<Rigidbody2D>();
        if (Random.value < 0.5f)
        {
            DaggerSprite.transform.localScale = new Vector3(-DaggerSprite.transform.localScale.x, DaggerSprite.transform.localScale.y, DaggerSprite.transform.localScale.z);
        }
    }

    public override void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
    {
        base.ExecuteSpellStart(finalDirection, spawnPoint);

        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;

        if (IsOwner)
        {
            Velocity.Value = _finalDirection * SpellData.Value.Speed;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (Velocity.Value != Vector2.zero)
        {
            DaggerSprite.transform.up = Velocity.Value.normalized;
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!Started.Value || !IsOwner || _isDead) return; //don't do anything before OnNetworkSpawn has run.

        _rigidbody2D.linearVelocity = Velocity.Value;
    }
}
