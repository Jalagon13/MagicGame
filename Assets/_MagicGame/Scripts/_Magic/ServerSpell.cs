using Unity.Netcode;
using UnityEngine;

public enum SpellState
{
    Charging, // Just appeared in the world, staring visuals
    Casting, // Spell actively being cast, ie fireball flying in the air or flamebreath continuously flowing while primary is being held
    Expiring // Spell is over and lots of cleanup stuff happening with 
}

public abstract class ServerSpell : NetworkBehaviour
{
    protected NetworkVariable<SyncSpellData> _spellData = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public SyncSpellData SpellData => _spellData.Value;
    
    protected NetworkVariable<SpellState> _spellState = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public SpellState SpellState => _spellState.Value;

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
        
    }

    public override void OnNetworkDespawn()
    {
        
    }
    
    public override void OnDestroy()
    {
        
    }
    
    public void Initialize(SyncSpellData spellData)
    {
        _spellData.Value = spellData;
        _spellState.Value = SpellState.Charging;
    }

    protected abstract void OnSpellSpawned();
    protected abstract void OnExecuteSpellStart();
    protected abstract void OnSpellEnd();
    protected abstract void OnSpellCanceled();
}
