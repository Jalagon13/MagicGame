using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpellCaster : NetworkBehaviour
{
    public event EventHandler OnActiveHoldToCastSpellEnded;
    public event EventHandler<SpellExecutedEventArgs> OnSpellExecuted;
    public class SpellExecutedEventArgs : EventArgs
    {
        public SpellItemSO SpellItem { get; }

        public SpellExecutedEventArgs(SpellItemSO spellItem)
        {
            SpellItem = spellItem;
        }
    }
    
    [SerializeField]
    private ServerCharacter _serverCharacter;

    private Timer _castTimer;
    public Timer CastTimer => _castTimer;

    private NetworkObjectReference _activeSpellNetObj;
    private ServerSpell _holdToCastSpell;
    public ServerSpell HoldToCastSpell => _holdToCastSpell;

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
        if (_isCasting.Value) return;

        Reset();

        _currentSpell = spellItemSO;
        _getExecutionParams = getExecutionParams;
        _isCasting.Value = true;

        _activeSpellNetObj = default;
        _holdToCastSpell = null;

        SpawnSpellServerRpc(spellItemSO.GetSyncSpellData(NetworkObjectId, _serverCharacter.CurrentBiome)); // pre-spawn the spell

        _castTimer = new Timer(spellItemSO.CastTime);
        _castTimer.OnTimerEnd += OnCastTimerEnd;
    }

    public void TryToCancelCast()
    {
        if (_isCasting.Value)
        {
            _isCasting.Value = false;

            if (_activeSpellNetObj.TryGet(out NetworkObject spellObj) && spellObj.TryGetComponent(out ServerSpell serverSpell))
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

            Reset();
        }
    }

    private void OnCastTimerEnd(object sender, EventArgs e)
    {
        _castTimer.OnTimerEnd -= OnCastTimerEnd;
        ExecuteSpell();

        // Only reset if no HoldToCast spell is active
        if (_holdToCastSpell == null)
        {
            Reset();
        }
    }

    private void ExecuteSpell()
    {
        if (_activeSpellNetObj.TryGet(out NetworkObject actualSpell))
        {
            if (actualSpell.TryGetComponent(out ServerSpell serverSpell))
            {
                var (finalSpawnPoint, finalDirection) = _getExecutionParams != null
                    ? _getExecutionParams.Invoke()
                    : (transform.position, transform.forward);

                serverSpell.ExecuteSpellStart(finalSpawnPoint, finalDirection);
                OnSpellExecuted?.Invoke(this, new SpellExecutedEventArgs(_currentSpell));

                if (serverSpell.SpellData.Value.HoldToCast)
                {
                    serverSpell.SpellStateNV.OnValueChanged += CheckForSpellEnd;
                    _holdToCastSpell = serverSpell;
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
            // Call an event here for 
            OnActiveHoldToCastSpellEnded?.Invoke(this, EventArgs.Empty);

            if (_holdToCastSpell != null)
            {
                _holdToCastSpell.SpellStateNV.OnValueChanged -= CheckForSpellEnd;
                _holdToCastSpell = null;
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
        NetworkObject spellNetObj;

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

        _activeSpellNetObj = new NetworkObjectReference(spellNetObj);
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

        if (_holdToCastSpell != null)
        {
            _holdToCastSpell.SpellStateNV.OnValueChanged -= CheckForSpellEnd;
            _holdToCastSpell = null;
        }

        _activeSpellNetObj = default;
        _cancelCast = false;
        _isCasting.Value = false; // centralized here
    }
}