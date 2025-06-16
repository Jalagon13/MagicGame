using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpellCaster : NetworkBehaviour
{
    public event EventHandler OnSpellCooldownTimersUpdated;

    [SerializeField] 
    private ServerCharacter _serverCharacter;
    
    private Dictionary<int, Timer> _spellCoolDownTimers = new();
    public Dictionary<int, Timer> SpellCoolDownTimers => _spellCoolDownTimers;
    
    private Timer _castTimer;
    public Timer CastTimer => _castTimer;
    
    private NetworkObject _spellNetObj;
    
    private bool _pendingCast, _cancelCast;
    
    private NetworkVariable<bool> _isCasting = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> IsCasting => _isCasting; // Exposes the NetworkVariable itself
    
    private SyncSpellData _currentSpellData;
    public SyncSpellData CurrentSpellData => _currentSpellData;

    private Func<(Vector3 spawnPoint, Vector3 direction)> _getExecutionParams;

    private void Awake()
    {
        _castTimer = new Timer(0f);
    }

    private void Update()
    {
        if (!IsOwner) return; // Only the owner should update the casting state and cooldowns
    
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
            OnSpellCooldownTimersUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public void TryCastSpell(SpellItemSO spellItemSO, Func<(Vector3 spawnPoint, Vector3 direction)> getExecutionParams)
    {
        // If spell is on cooldown || cast timer still going || still casting a spell
        if (_spellCoolDownTimers.ContainsKey(GameManager.Instance.GetItemIdFromItemSO(spellItemSO)) || _castTimer.IsRunning || _isCasting.Value) return;

        Reset();

        _currentSpellData = spellItemSO.GetSpellDataForLocalClientInstance(NetworkObjectId, _serverCharacter.CurrentBiome);
        
        SpawnSpellServerRpc(_currentSpellData);
        
        _getExecutionParams = getExecutionParams;
        _isCasting.Value = true;
        _castTimer = new Timer(spellItemSO.CastTime);
        _castTimer.OnTimerEnd += OnCastTimerEnd;
    }
    
    public void TryToCancelCast()
    {
        if(_isCasting.Value)
        {
            _isCasting.Value = false;
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
            if (actualSpell.TryGetComponent<ServerSpell>(out var serverSpell))
            {
                // Get the final spawn point and direction
                var (finalSpawnPoint, finalDirection) = _getExecutionParams != null
                ? _getExecutionParams.Invoke()
                : (transform.position, transform.forward);
                
                serverSpell.ExecuteSpellStart(finalSpawnPoint, finalDirection);
                
                // Set spell cooldown
                SpellItemSO spellItemSO = GameManager.Instance.GetItemSOFromItemId(serverSpell.SpellData.Value.SpellItemId) as SpellItemSO;
                _spellCoolDownTimers[serverSpell.SpellData.Value.SpellItemId] = new Timer(spellItemSO.Cooldown);

                // Reset casting state here if not a continuous cast
                if (!serverSpell.SpellData.Value.IsContinuousCast)
                {
                    _isCasting.Value = false;
                }
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
    private void SpawnSpellServerRpc(SyncSpellData spellData, RpcParams rpcParams = default)
    {
        var spellPrefab = (GameManager.Instance.GetItemSOFromItemId(spellData.SpellItemId) as SpellItemSO).SpellPrefab;
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(spellData.CasterNetworkObjectId, out NetworkObject casterNetObj);
        ServerSpell spell = Instantiate(spellPrefab, casterNetObj.transform.position, Quaternion.identity);

        NetworkObject spellNetObj = spell.GetComponent<NetworkObject>();
        spellNetObj.SpawnWithObservers = false;
        spellNetObj.SpawnWithOwnership(casterNetObj.OwnerClientId, true);

        spell.SpellData.Value = spellData;
        spell.GetComponent<SpellNetworkVisibility>().InitializeSpellNetwork(spellData);
        
        SendSpellRefToCasterRpc(spellNetObj.NetworkObjectId, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SendSpellRefToCasterRpc(ulong spellNetObjId, RpcParams rpcParams)
    {
        StartCoroutine(WaitForSpellNetObjAndHandle(spellNetObjId));
    }
    
    private IEnumerator WaitForSpellNetObjAndHandle(ulong spellNetObjId)
    {
        float timeout = 2f; // seconds, adjust as needed
        float elapsed = 0f;
        while (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(spellNetObjId, out _spellNetObj))
        {
            if (elapsed > timeout)
            {
                Debug.LogError($"Timeout waiting for SpellNetObj with ID {spellNetObjId} on client {NetworkManager.Singleton.LocalClientId}.");
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Debug.Log($"SpellNetObj is null? {_spellNetObj == null}, on client {NetworkManager.Singleton.LocalClientId}, Elapsed time: {elapsed}s");

        if (_cancelCast)
        {
            _spellNetObj.GetComponent<ServerSpell>().CancelSpellCharge();
            Reset();
            yield break;
        }

        if (_pendingCast)
        {
            TryExecuteSpell(); // Delayed shoot now that the object exists
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void DespawnSpellServerRpc(NetworkObjectReference spellNetObjRef)
    {
        NetworkObject spellNetObj = spellNetObjRef;
        spellNetObj.Despawn();
    }

    private void Reset()
    {
        _castTimer.OnTimerEnd -= OnCastTimerEnd;
        
        _spellNetObj = null;
        _pendingCast = false;
        _cancelCast = false;
    }
}
