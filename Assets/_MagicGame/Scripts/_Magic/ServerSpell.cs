using System;
using System.Collections;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public enum SpellState
{
    Charging, // Just appeared in the world, staring visuals
    Casting, // Spell actively being cast, ie fireball flying in the air or flamebreath continuously flowing while primary is being held
    Stopping // Spell is over and lots of cleanup stuff happening with 
}

public abstract class ServerSpell : NetworkBehaviour
{
    [SerializeField] 
    private ClientSpell _clientSpell;
    public ClientSpell ClientSpell => _clientSpell;

    public NetworkVariable<SyncSpellData> SpellData { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<SpellState> SpellStateNV { get; set; } = new(SpellState.Charging, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public int CollisionMask { get; private set; }
    public int NpcLayer { get; private set; }
    public int WallMask { get; private set; }
    public int FoliageLayer { get; private set; } 

    protected Vector2 _finalDirection;

    public NetworkObject SpellCasterNetworkObject
    {
        get
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(SpellData.Value.CasterNetworkObjectId, out NetworkObject inflicterNetworkObj))
            {
                return inflicterNetworkObj;
            }

            return null;
        }
    }
    
    public override void OnNetworkSpawn()
    {
        ClientSpell.Visualization.SetActive(false);

        if (IsOwner)
        {
            CollisionMask = LayerMask.GetMask(new[] { "WallCollider", "Npc" }); // Bitmask
            WallMask = LayerMask.NameToLayer("WallCollider"); // Layer int
            NpcLayer = LayerMask.NameToLayer("Npc"); // Layer int
            FoliageLayer = LayerMask.NameToLayer("Foliage"); // Layer int
        }

        OnSpellInitialize();
    }

    private void Update()
    {
        if (IsOwner && SpellStateNV.Value == SpellState.Casting)
        {
            OnUpdateSpell();
        }
    }
    
    private void FixedUpdate()
    {
        if(IsOwner && SpellStateNV.Value == SpellState.Casting)
        {
            OnFixedUpdateSpell();
        }
    }

    public void ExecuteSpellStart(Vector3 spawnPoint, Vector2 finalDirection)
    {
        transform.position = spawnPoint;
        _finalDirection = finalDirection;
        
        StartCoroutine(SpellLifetimeRoutine());
    }

    private IEnumerator SpellLifetimeRoutine()
    {
        ClientSpell.Visualization.SetActive(true);
        SpellStateNV.Value = SpellState.Casting;
        
        OnSpellExecute();
        
        if(SpellData.Value.IsContinuousCast)
        {
            while(SpellCasterNetworkObject.GetComponent<SpellCaster>().IsCasting.Value)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(SpellData.Value.Lifetime);
        }
        
        SpellStateNV.Value = SpellState.Stopping;
        
        yield return OnSpellEnd(); // yield any cleanup animations

        SpellCasterNetworkObject.GetComponent<SpellCaster>().DespawnSpellServerRpc(NetworkObject);
        gameObject.SetActive(false); // Disable after RPC
    }

    

    public void CancelSpellCharge()
    {
        if (SpellData.Value.IsContinuousCast)
        {
            SpellCasterNetworkObject.GetComponent<SpellCaster>().IsCasting.Value = false;
        }

        OnSpellCanceled();
        
        SpellStateNV.Value = SpellState.Stopping;

        SpellCasterNetworkObject.GetComponent<SpellCaster>().DespawnSpellServerRpc(NetworkObject);
        gameObject.SetActive(false); // Disable after RPC
    }

    // Owner Methods
    protected abstract void OnSpellExecute();
    protected abstract IEnumerator OnSpellEnd();

    protected virtual void OnUpdateSpell() { }
    protected virtual void OnFixedUpdateSpell() { }
    public virtual void OnSpellInitialize() { }
    protected virtual void OnSpellCanceled() { }
    
    // Client Methods
    public virtual void ClientSpellStart(ClientSpell clientSpell) { }
    public virtual void ClientSpellUpdate(ClientSpell clientSpell) { }
    public virtual void ClientSpellStop(ClientSpell clientSpell) { }

    // NTFS: *maybe* put this in a util class idk we'll see
    public bool IsValidNpcHit(Collider2D collider, out DamageReceiver damageReceiver)
    {
        damageReceiver = null;

        if (collider.gameObject.layer != NpcLayer)
            return false;

        if (!collider.TryGetComponent(out NpcNetworkVisibility npcNet))
            return false;

        if (!npcNet.SameBiomeAs(SpellData.Value.SpawnBiome))
            return false;

        damageReceiver = npcNet.GetComponent<DamageReceiver>();
        return damageReceiver != null;
    }
}
