using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class TeleportBolt : Spell
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

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (IsServer)
        {
            SpawnTeleportParticlesClientRpc(NetworkManager.ConnectedClients[SpellData.Value.OwnerPlayerId].PlayerObject.transform.position, transform.position);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnTeleportParticlesClientRpc(Vector2 particleSpawnPoint, Vector2 teleportPoint)
    {
        SoundManager.Instance.PlayOneShot(_teleportSound, transform.position);
        Debug.Log("Spawning Teleport Particles");
        _vfx.transform.position = particleSpawnPoint;
        _vfx.transform.parent = null;
        _vfx.GetComponent<ParticleSystem>().Play();

        if(SpellData.Value.OwnerPlayerId == Player.LocalClientInstance.OwnerClientId)
        {
            var playerWhoShotIt = NetworkManager.ConnectedClients[SpellData.Value.OwnerPlayerId].PlayerObject;
            playerWhoShotIt.transform.position = transform.position;
            Debug.Log($"Player {playerWhoShotIt} teleported");
        }
    }

    protected override void OnOwnerExecuteSpellStart()
    {
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;

        if (IsServer)
        {
            Velocity.Value = _finalDirection * SpellData.Value.Speed;
        }
    }

    private void FixedUpdate()
    {
        if (!IsStarted.Value || !IsOwner) return; //don't do anything before OnNetworkSpawn has run.

        Velocity.Value = Vector2.Lerp(Velocity.Value, Velocity.Value, _velocityDecay * Time.fixedDeltaTime);
        _rigidbody2D.linearVelocity = Velocity.Value;
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