using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpellCaster : NetworkBehaviour
{
    [SerializeField] 
    private ServerCharacter _serverCharacter;
    
    private Dictionary<int, Timer> _spellCoolDownTimers = new();
    
    private Timer _castTimer;
    public Timer CastTimer => _castTimer;
    
    private NetworkObject _spellNetObj;
    private bool _pendingCast, _cancelCast;
    private Vector2? _spellSpawnPoint;
    private Vector2? _spellExecuteDirection;

    private void Update()
    {
        _castTimer?.Tick(Time.deltaTime);
        
        foreach (int key in new List<int>(_spellCoolDownTimers.Keys)) // To avoid modifying the collection while iterating
        {
            Timer spellCdTimer = _spellCoolDownTimers[key];
            spellCdTimer.Tick(Time.deltaTime);

            if (spellCdTimer.RemainingSeconds <= 0)
            {
                _spellCoolDownTimers.Remove(key);
            }
        }

        if (_spellCoolDownTimers.Count > 0)
        {
            // OnSpellCooldownTimersUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    // Play this from external source while cast time is running
    public void SetSpellExecuteParameters(Vector2 spawnPoint, Vector2 executeDirection)
    {
        _spellSpawnPoint = spawnPoint;
        _spellExecuteDirection = executeDirection;
    }
    
    public void TryCastSpell(SpellItemSO spellItemSO)
    {
        if (_spellCoolDownTimers.ContainsKey(GameManager.Instance.GetItemIdFromItemSO(spellItemSO)) || _castTimer.IsRunning)
        {
            return; // If spell is on cooldown || cast timer still going
        }

        var spellData = spellItemSO.GetSpellDataForLocalClientInstance(NetworkObjectId, _serverCharacter.CurrentBiome);
        SpawnSpellServerRpc(spellData, OwnerClientId);
        Reset();

        _castTimer = new Timer(spellItemSO.CastTime);
        _castTimer.OnTimerEnd += OnCastTimerEnd;
    }
    
    public void TryToCancelCast()
    {
        if(_castTimer.IsRunning)
        {
            _cancelCast = _spellNetObj == null; // if it equals null, then RTT isn't done yet, set it to true so when it arrives, it is canceled not executed
            if(_spellNetObj != null)
            {
                _spellNetObj.GetComponent<ServerSpell>().CancelSpellCharge();
                Reset();
            }

            // NTFS: add a cancel event here maybe
        }
    }

    private void OnCastTimerEnd(object sender, EventArgs e)
    {
        _castTimer.OnTimerEnd -= OnCastTimerEnd;
        
        TryExecuteSpell();
    }
    
    private void TryExecuteSpell()
    {
        if (_spellNetObj != null)
        {
            // ShootSpell
            ExecuteSpell(_spellNetObj);
            Reset();
        }
        else
        {
            _pendingCast = true; // Wait for SendSpellRefToCasterRpc to retry
        }
    }
    
    private void ExecuteSpell(NetworkObjectReference spellNetObj)
    {
        if (spellNetObj.TryGet(out NetworkObject actualSpell))
        {
            ServerSpell serverSpell = actualSpell.GetComponent<ServerSpell>();
            if (serverSpell != null)
            {
                serverSpell.ExecuteSpellStart(
                _spellSpawnPoint == null ? transform.position : _spellSpawnPoint.Value,
                _spellExecuteDirection == null ? Vector2.right : _spellExecuteDirection.Value);

                // Set spell cooldown
                SpellItemSO spellItemSO = GameManager.Instance.GetItemSOFromItemId(serverSpell.SpellData.SpellItemId) as SpellItemSO;
                _spellCoolDownTimers[serverSpell.SpellData.SpellItemId] = new Timer(spellItemSO.Cooldown);
            }
            else
            {
                Debug.LogWarning("Spell component missing on spawned spell!");
            }
        }
        else
        {
            Debug.LogWarning("Failed to resolve NetworkObjectReference to shoot spell.");
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void SpawnSpellServerRpc(SyncSpellData spellData, ulong senderOwnerId)
    {
        var spellPrefab = (GameManager.Instance.GetItemSOFromItemId(spellData.SpellItemId) as SpellItemSO).SpellPrefab;
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(spellData.CasterNetworkObjectId, out NetworkObject casterNetObj);
        ServerSpell spell = Instantiate(spellPrefab, casterNetObj.transform.position, Quaternion.identity);

        NetworkObject spellNetObj = spell.GetComponent<NetworkObject>();
        spellNetObj.SpawnWithObservers = false;
        spellNetObj.SpawnWithOwnership(casterNetObj.OwnerClientId, true);

        spell.Initialize(spellData);
        spell.GetComponent<SpellNetworkComponent>().InitializeSpellNetwork(spellData);
        
        SendSpellRefToCasterRpc(spellNetObj, RpcTarget.Single(senderOwnerId, RpcTargetUse.Persistent));
    }

    [Rpc(SendTo.SpecifiedInParams, RequireOwnership = false)]
    private void SendSpellRefToCasterRpc(NetworkObjectReference spellNetObj, RpcParams rpcParams = default)
    {
        _spellNetObj = spellNetObj;
        
        if(_cancelCast)
        {
            _spellNetObj.GetComponent<ServerSpell>().CancelSpellCharge();
            Reset();
            return;
        }
        
        if(_pendingCast)
        {
            TryExecuteSpell(); // Delayed shoot now that the object exists
        }
    }

    private void Reset()
    {
        _castTimer.OnTimerEnd -= OnCastTimerEnd;
        _castTimer = null;
        _spellSpawnPoint = null;
        _spellExecuteDirection = null;
        _spellNetObj = null;
        _pendingCast = false;
        _cancelCast = false;
    }
}
