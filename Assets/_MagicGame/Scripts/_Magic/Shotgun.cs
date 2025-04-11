using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class Shotgun : Spell
{
    [SerializeField] private ParticleSystem _hitParticles;
    [SerializeField] private ParticleSystem _trailParticles;
    [SerializeField] private ParticleSystem _teleportParticles;
    [SerializeField] private EventReference _teleportSound;
    [SerializeField] private float _velocityDecay = 5f;

    private Rigidbody2D _rigidbody2D;
    private GameObject _vfx;

    protected override void Awake()
    {
        base.Awake();

        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _vfx = Instantiate(_teleportParticles.gameObject, _spellGameObject.transform);
        _vfx.transform.localPosition = Vector3.zero;
    }

    public override void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
    {
        base.ExecuteSpellStart(finalDirection, spawnPoint);

        SpawnBlastParticlesClientRpc(spawnPoint);

        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;

        if (IsServer)
        {
            Velocity.Value = _finalDirection * SpellData.Value.Speed;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnBlastParticlesClientRpc(Vector2 spawnPoint)
    {
        _vfx.transform.position = spawnPoint;
        _vfx.GetComponent<ParticleSystem>().Play();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!Started.Value || !IsOwner || _isDead) return; //don't do anything before OnNetworkSpawn has run.

        Velocity.Value = Vector2.Lerp(Velocity.Value, Velocity.Value, _velocityDecay * Time.fixedDeltaTime);
        _rigidbody2D.linearVelocity = Velocity.Value;
        
        // Pull in enemies
        
    }

    public override void OnDestroy()
    {
        _trailParticles.gameObject.transform.parent = null;
        var main = _trailParticles.main;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        base.OnDestroy();
    }
}
