using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public abstract class Spell : NetworkBehaviour
{
	protected SyncSpellData _spellData;
	protected SpellNetworkComponent _spellNetworkComponent;
	protected GameObject _spellGameObject;
	protected Collider2D _spellCollider;
	
	private Transform _spellModifierTf;

    private void Awake()
    {
		_spellGameObject = transform.GetChild(0).gameObject;
		_spellCollider = GetComponent<Collider2D>();
		_spellNetworkComponent = GetComponent<SpellNetworkComponent>();
	}
	
	public override void OnNetworkSpawn()
	{
		Debug.Log($"Test");
		base.OnNetworkSpawn();
	}
	
	public abstract void CastSpell();
	
	public void InitializeSpell(SyncSpellData spellData)
	{
		_spellGameObject = transform.GetChild(0).gameObject;
		_spellCollider = GetComponent<Collider2D>();
		_spellNetworkComponent = GetComponent<SpellNetworkComponent>();
		_spellData = spellData;

		if (IsServer)
		{
			_spellNetworkComponent.InitializeSpellNetwork(_spellData);
		}
		
		_spellModifierTf = transform.GetChild(0).GetChild(0);

		foreach (int modifierIndex in _spellData.ModifierArray)
		{
			SpellModItemSO modifier = GameManager.Instance.GetItemSOFromItemId(modifierIndex) as SpellModItemSO;
			Instantiate(modifier.SpellModifierPrefab.gameObject, _spellModifierTf);
		}
		
		CastSpell();
	}
	
	protected bool PlayerOwnerClientIdEqualsServerId()
	{
		return Player.LocalClientInstance.OwnerClientId == NetworkManager.ServerClientId;
	}

	
	protected bool ColliderIsSourcePlayer(Collider2D col)
	{
		return NetworkManager.ConnectedClients[_spellData.SpawnPlayerId].PlayerObject == null || NetworkManager.ConnectedClients[_spellData.SpawnPlayerId].PlayerObject.GetComponent<Player>().HitCollider == col;
	}
}