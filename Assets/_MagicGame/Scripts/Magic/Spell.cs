using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Spell : NetworkBehaviour
{
	public bool Started { get; private set; }
	
	protected SyncSpellData _spellData;
	protected GameObject _spellGameObject;
	protected Collider2D _spellCollider;
	protected bool _isDead;
	protected Vector2 _finalDirection;
	
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
	
	public virtual void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
	{
		Debug.Log($"Spell Executed in Spell Script");
		if(IsServer)
		{
			transform.position = spawnPoint;
			_finalDirection = finalDirection;
			Started = true;
			_isDead = false;

			_spellTimer = new Timer(_spellData.Lifetime);
			_spellTimer.OnTimerEnd += DestroySpell;
		}
	}
	
	public void CancelSpell()
	{
		Debug.Log($"Spell Canceled");
		
		if (IsServer)
		{
			NetworkObject.Despawn();
		}
		
		Destroy(gameObject);
	}
	
	public void SetSpellData(SyncSpellData spellData)
	{
		_spellData = spellData;
		GetComponent<SpellNetworkComponent>().InitializeSpellNetwork(spellData);
		Debug.Log($"SpellData Set");
	}
	
	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			_serverSpellData.Value = _spellData;
		}

		foreach (int modifierIndex in _serverSpellData.Value.ModifierArray)
		{
			SpellModItemSO modifier = GameManager.Instance.GetItemSOFromItemId(modifierIndex) as SpellModItemSO;
			Instantiate(modifier.SpellModifierPrefab.gameObject, _spellModifierTf);
		}
		
		Debug.Log($"NetworkSpawn() ID: {NetworkObjectId}");
	}
	
    private void DestroySpell(object sender, EventArgs e)
    {
		Started = false;

		if (IsServer)
		{
			NetworkObject.Despawn();
		}
		Debug.Log($"DestroySpell()");
		Destroy(gameObject);
	}

	protected virtual void FixedUpdate()
	{
		if ((IsServer /* || _isLocalSpell */) && Started)
		{
			_spellTimer.Tick(Time.fixedDeltaTime);
		}
	}
}