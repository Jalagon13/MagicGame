using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum SpellState
{
    None,
    Charging, // Just appeared in the world, staring visuals
    Casting, // Spell actively being cast, ie fireball flying in the air or flamebreath continuously flowing while primary is being held
    Stopping // Spell is over and lots of cleanup stuff happening with 
}

public abstract class ServerSpell : NetworkBehaviour
{
    protected NetworkVariable<SyncSpellData> _spellData = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public SyncSpellData SpellData => _spellData.Value;
    
    public NetworkVariable<SpellState> SpellStateNV { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public int CollisionMask { get; private set; }
    public int NpcLayer { get; private set; }
    public int WallMask { get; private set; }

    protected Vector2 _finalDirection;

    public NetworkObject SpellCasterNetworkObject
    {
        get
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_spellData.Value.CasterNetworkObjectId, out NetworkObject inflicterNetworkObj))
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
            CollisionMask = LayerMask.GetMask(new[] { "LocalWall", "Npc" });
            WallMask = LayerMask.NameToLayer("LocalWall");
            NpcLayer = LayerMask.NameToLayer("Npc");

            SpellManager.Instance.OnExecuteSpells += ExecuteSpellStart;
            SpellManager.Instance.OnCancelSpells += CancelSpellCharge;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            SpellManager.Instance.OnExecuteSpells -= ExecuteSpellStart;
            SpellManager.Instance.OnCancelSpells -= CancelSpellCharge;
        }   
    }

    public override void OnDestroy()
    {
        
    }

    private void CancelSpellCharge(object sender, EventArgs e)
    {
        if (_spellData.Value.IsContinuousCast)
        {
            SpellManager.Instance.IsContinuouslyCasting = false;
        }
        
        SpellCanceled();
        SpellStateNV.Value = SpellState.Stopping;
        NetworkObject.DontDestroyWithOwner = true;
        NetworkObject.Despawn();
    }

    public void ExecuteSpellStart(object sender, SpellManager.ExecuteSpellsEventArgs e)
    {
        transform.position = e.SpawnPoint;
        _finalDirection = e.Direction;
        
        StartCoroutine(SpellLifetimeRoutine());
    }

    private IEnumerator SpellLifetimeRoutine()
    {
        SpawnSpell();
        yield return new WaitForSeconds(_spellData.Value.Lifetime);
        EndSpell();
    }
    
    public void EndSpell()
    {
        StartCoroutine(EndSpellRoutinue());
    }
    
    private IEnumerator EndSpellRoutinue()
    {
        SpellStateNV.Value = SpellState.Stopping;
        yield return OnSpellEnd(); // yield any cleanup animations
        NetworkObject.Despawn();
    }

    public void Initialize(SyncSpellData spellData)
    {
        _spellData.Value = spellData;
        SpellStateNV.Value = SpellState.None;
    }
    
    protected abstract void StartSpell();
    protected abstract void UpdateSpell();
    protected abstract IEnumerator OnSpellEnd();
    protected virtual void SpawnSpell() { }
    protected virtual void SpellCanceled() { }
    
    public virtual void ClientSpellStart() { }
    public virtual void ClientSpellUpdate() { }
    public virtual void ClientSpellStop() { }
}
