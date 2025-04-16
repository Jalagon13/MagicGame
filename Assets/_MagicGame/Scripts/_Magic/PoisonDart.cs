using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class PoisonDart : Spell
{
    [field: SerializeField] public SpriteRenderer PoisonDartSprite { get; private set; }
    [SerializeField] private float _velocityDecay = 5f;

    private Rigidbody2D _rigidbody2D;

    protected override void Awake()
    {
        base.Awake();

        _rigidbody2D = GetComponent<Rigidbody2D>();

        if (Random.value < 0.5f)
        {
            PoisonDartSprite.transform.localScale = new Vector3(-PoisonDartSprite.transform.localScale.x, PoisonDartSprite.transform.localScale.y, PoisonDartSprite.transform.localScale.z);
        }
    }

    public override void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
    {
        base.ExecuteSpellStart(finalDirection, spawnPoint);

        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;

        if (IsServer)
        {
            Velocity.Value = _finalDirection * SpellData.Value.Speed;
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!Started.Value || !IsOwner || _isDead) return; //don't do anything before OnNetworkSpawn has run.

        Velocity.Value = Vector2.Lerp(Velocity.Value, Vector2.zero, _velocityDecay * Time.fixedDeltaTime);
        _rigidbody2D.linearVelocity = Velocity.Value;
    }

    protected override void Update()
    {
        base.Update();

        if (Velocity.Value != Vector2.zero)
        {
            PoisonDartSprite.transform.up = Velocity.Value.normalized;
        }
    }
}
