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

    private Timer _castTimer;
    public Timer CastTimer => _castTimer;

    private List<NetworkObjectReference> _activeSpellNetObjs = new();
    private List<ServerSpell> _spellsWithEndCallbacks = new();

    private bool _cancelCast;

    private NetworkVariable<bool> _isCasting = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> IsCasting => _isCasting;

    [SerializeField]
    private Transform _spellSpawnTransform;
    public Transform SpellSpawnTransform => _spellSpawnTransform;

    private Func<(Vector3 spawnPoint, Vector3 direction)> _getExecutionParams;

    private Vector2 _castingPoint;
    public Vector2 CastingPoint => _castingPoint;

    private SpellItemSO _currentSpell;
    public SpellItemSO CurrentSpell => _currentSpell;

    private List<NetworkObjectReference> _pendingSpellsToExecute = new();

    private void Awake()
    {
        _castTimer = new Timer(0f);
    }

    private void Update()
    {
        if (!IsOwner) return;
        _castTimer?.Tick(Time.deltaTime);
    }

    public void SetCastingPoint(Vector2 castPoint)
    {
        _castingPoint = castPoint;
    }

    public void TryCastSpell(SpellItemSO spellItemSO, Func<(Vector3 spawnPoint, Vector3 direction)> getExecutionParams)
    {
        if (_castTimer.IsRunning || _isCasting.Value) return;

        Reset();

        _currentSpell = spellItemSO;
        _getExecutionParams = getExecutionParams;
        _isCasting.Value = true;
        _pendingSpellsToExecute.Clear();

        SpawnSpellServerRpc(spellItemSO.GetSyncSpellData(NetworkObjectId, _serverCharacter.CurrentBiome)); // pre-spawn all spells

        _castTimer = new Timer(spellItemSO.CastTime);
        _castTimer.OnTimerEnd += OnCastTimerEnd;
    }

    public void TryToCancelCast()
    {
        if (_isCasting.Value)
        {
            _isCasting.Value = false;

            foreach (var spellRef in new List<NetworkObjectReference>(_activeSpellNetObjs))
            {
                if (spellRef.TryGet(out NetworkObject spellObj) && spellObj.TryGetComponent(out ServerSpell serverSpell))
                {
                    if(serverSpell.SpellStateNV.Value == SpellState.Charging)
                    {
                        serverSpell.CancelSpellCharge();
                    }
                    else if (serverSpell.SpellStateNV.Value == SpellState.Casting)
                    {
                        serverSpell.EndSpellExternally();
                    }
                }
            }

            Reset();
        }
    }

    private void OnCastTimerEnd(object sender, EventArgs e)
    {
        _castTimer.OnTimerEnd -= OnCastTimerEnd;

        // Default: cast all spells at once
        foreach (var spellRef in _pendingSpellsToExecute)
        {
            ExecuteSpell(spellRef);
        }

        Reset();
    }

    private void ExecuteSpell(NetworkObjectReference spellNetObj)
    {
        if (spellNetObj.TryGet(out NetworkObject actualSpell))
        {
            if (actualSpell.TryGetComponent(out ServerSpell serverSpell))
            {
                var (finalSpawnPoint, finalDirection) = _getExecutionParams != null
                    ? _getExecutionParams.Invoke()
                    : (transform.position, transform.forward);

                serverSpell.ExecuteSpellStart(finalSpawnPoint, finalDirection);

                if (serverSpell.SpellData.Value.HoldToCast)
                {
                    serverSpell.SpellStateNV.OnValueChanged += CheckForSpellEnd;
                    _spellsWithEndCallbacks.Add(serverSpell);
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

    private void CheckForSpellEnd(SpellState previousValue, SpellState newValue)
    {
        if (previousValue == SpellState.Casting && newValue == SpellState.Stopping)
        {
            for (int i = _spellsWithEndCallbacks.Count - 1; i >= 0; i--)
            {
                var spell = _spellsWithEndCallbacks[i];
                if (spell.SpellStateNV.Value == SpellState.Stopping)
                {
                    spell.SpellStateNV.OnValueChanged -= CheckForSpellEnd;
                    _spellsWithEndCallbacks.RemoveAt(i);
                    break;
                }
            }
            
            Reset();
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

        SendSpellRefToCasterRpc(spellNetObj.NetworkObjectId, RpcTarget.Single(casterNetObj.OwnerClientId, RpcTargetUse.Persistent));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SendSpellRefToCasterRpc(ulong spellNetObjId, RpcParams rpcParams)
    {
        StartCoroutine(WaitForSpellNetObjAndHandle(spellNetObjId));
    }

    private IEnumerator WaitForSpellNetObjAndHandle(ulong spellNetObjId)
    {
        float timeout = 2f;
        float elapsed = 0f;
        NetworkObject spellNetObj = null;

        while (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(spellNetObjId, out spellNetObj))
        {
            if (elapsed > timeout)
            {
                Debug.LogError($"Timeout waiting for SpellNetObj with ID {spellNetObjId} on client {NetworkManager.Singleton.LocalClientId}.");
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (_cancelCast)
        {
            if (spellNetObj.TryGetComponent(out ServerSpell serverSpell))
            {
                serverSpell.CancelSpellCharge();
            }
            yield break;
        }

        var spellRef = new NetworkObjectReference(spellNetObj);
        _pendingSpellsToExecute.Add(spellRef);
        _activeSpellNetObjs.Add(spellRef);
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
        _castTimer.Reset();

        foreach (var spell in _spellsWithEndCallbacks)
        {
            spell.SpellStateNV.OnValueChanged -= CheckForSpellEnd;
        }

        _spellsWithEndCallbacks.Clear();
        _pendingSpellsToExecute.Clear();
        _activeSpellNetObjs.Clear();
        _cancelCast = false;
        _isCasting.Value = false; // centralized here
    }
}