using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Spell : NetworkBehaviour
{
	protected SyncSpellData _spellData;
	protected GameObject _spellGameObject;
	protected Collider2D _spellCollider;
	protected bool _started;
	protected bool _isDead;
	protected bool _isLocalSpell;
	
	private NetworkVariable<SyncSpellData> _serverSpellData = new NetworkVariable<SyncSpellData>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private Transform _spellModifierTf;
	private Timer _spellTimer;

    protected virtual void Awake()
    {
		Debug.Log($"Spell Awake");
		_spellGameObject = transform.GetChild(0).gameObject;
		_spellCollider = GetComponent<Collider2D>();
		_spellModifierTf = transform.GetChild(0).GetChild(0);
	}
	
	public void InitializeLocalSpell(SyncSpellData spellData)
	{
		_spellData = spellData;
		_isLocalSpell = true;
		_started = true;

		_spellTimer = new Timer(_spellData.Lifetime);
		_spellTimer.OnTimerEnd += DestroySpell;

		foreach (int modifierIndex in _spellData.ModifierArray)
		{
			SpellModItemSO modifier = GameManager.Instance.GetItemSOFromItemId(modifierIndex) as SpellModItemSO;
			Instantiate(modifier.SpellModifierPrefab.gameObject, _spellModifierTf);
		}
		Debug.Log($"Local Spell Initialized");

		SpellSetUp();
	}
	
	public void InitializeServerSpell(SyncSpellData spellData)
	{
		_spellData = spellData;

		Debug.Log($"Server Spell Initialized");
	}
	
	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			_serverSpellData.Value = _spellData;

			_started = true;
			_isDead = false;

			_spellTimer = new Timer(_spellData.Lifetime);
			_spellTimer.OnTimerEnd += DestroySpell;
		}

		foreach (int modifierIndex in _serverSpellData.Value.ModifierArray)
		{
			SpellModItemSO modifier = GameManager.Instance.GetItemSOFromItemId(modifierIndex) as SpellModItemSO;
			Instantiate(modifier.SpellModifierPrefab.gameObject, _spellModifierTf);
		}

		Debug.Log($"NetworkSpawn() ID: {NetworkObjectId}");

		SpellSetUp();
	}
	
	protected virtual void SpellSetUp() { }

    private void DestroySpell(object sender, EventArgs e)
    {
		_started = false;

		if (IsServer)
		{
			NetworkObject.Despawn();
		}
		Debug.Log($"DestroySpell()");
		Destroy(gameObject);
	}

	protected virtual void FixedUpdate()
	{
		if ((IsServer || _isLocalSpell) && _started)
		{
			_spellTimer.Tick(Time.fixedDeltaTime);
		}
	}
}