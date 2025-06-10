using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class TeleportBolt : Spell
{
    [field: Header("Teleport Bolt")]
    [field: SerializeField] public float VelocityDecay { get; private set; } = 5f;
    [field: SerializeField] public ParticleSystem TeleportParticles { get; private set; }
    [field: SerializeField] public ParticleSystem Trail { get; private set; }
    [field: SerializeField] public EventReference TeleportSound { get; private set; }

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

        _vfx = Instantiate(TeleportParticles.gameObject, _spellGameObject.transform);
        _vfx.transform.localPosition = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            Velocity.Value = Vector2.Lerp(Velocity.Value, Vector2.zero, VelocityDecay * Time.fixedDeltaTime);
            _rigidbody2D.linearVelocity = Velocity.Value;
        }
    }

    protected override void OnExecuteSpellStart()
    {
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;

        if (IsOwner)
        {
            Velocity.Value = _finalDirection * SpellData.Value.Speed;
        }
    }

    protected override void OnSpellSpawned()
    {
        // Optional setup logic
    }

    protected override void OnSpellEnd()
    {
        if (Trail != null)
        {
            Trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (IsOwner)
        {
            SpawnTeleportParticlesClientRpc(NetworkManager.ConnectedClients[SpellData.Value.CasterNetworkObjectId].PlayerObject.transform.position, transform.position);
        }
    }

    protected override void OnSpellCanceled()
    {
        // Optional cancel logic
    }

    [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
    private void SpawnTeleportParticlesClientRpc(Vector2 particleSpawnPoint, Vector2 teleportPoint)
    {
        if (Player.LocalClientInstance.CurrentBiome.Value != SpellData.Value.SpawnBiome) return;

        SoundManager.Instance.PlayOneShot(TeleportSound, transform.position);
        _vfx.transform.position = particleSpawnPoint;
        _vfx.transform.parent = null;
        _vfx.GetComponent<ParticleSystem>().Play();

        if (SpellData.Value.CasterNetworkObjectId == Player.LocalClientInstance.OwnerClientId)
        {
            var playerWhoShotIt = NetworkManager.ConnectedClients[SpellData.Value.CasterNetworkObjectId].PlayerObject;
            playerWhoShotIt.transform.position = transform.position;
        }
    }
}