using System;
using System.Collections;
using Unity.Netcode;
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

    public NetworkVariable<SyncSpellData> SpellData { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<SpellState> SpellStateNV { get; set; } = new(SpellState.Charging, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public int CollisionMask { get; private set; }
    public int NpcLayer { get; private set; }
    public int WallMask { get; private set; }

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
        if(IsOwner)
        {
            CollisionMask = LayerMask.GetMask(new[] { "LocalWall", "Npc" }); // Bitmask
            WallMask = LayerMask.NameToLayer("LocalWall"); // Layer int
            NpcLayer = LayerMask.NameToLayer("Npc"); // Layer int
        }
    }

    private void Update()
    {
        if(IsOwner && SpellStateNV.Value == SpellState.Casting)
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
        SpellStateNV.Value = SpellState.Casting;
        
        OnSpellExecute();
        
        yield return new WaitForSeconds(SpellData.Value.Lifetime);
        
        SpellStateNV.Value = SpellState.Stopping;
        
        yield return OnSpellEnd(); // yield any cleanup animations
        
        NetworkObject.Despawn();
    }

    public void CancelSpellCharge()
    {
        if (SpellData.Value.IsContinuousCast)
        {
            // SpellManager.Instance.IsContinuouslyCasting = false;
        }

        OnSpellCanceled();
        SpellStateNV.Value = SpellState.Stopping;
        NetworkObject.Despawn();
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
}
