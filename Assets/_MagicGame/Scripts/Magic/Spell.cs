using System;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public abstract class Spell : NetworkBehaviour
{
	protected int _damage, _knockback, _speed;
	protected ulong _projectileId, _spawnPlayerId;
	protected float _lifetime;
	protected bool _isLocalProjectile;
	protected Vector3 _directionNormalized, _projSpawnPoint;
	protected SpellNetworkComponent _spellNetworkComponent;
	protected BiomeType _biome;
	protected GameObject _spellGameObject;
	protected Collider2D _spellCollider;

	private void Awake()
	{
		Initialize();
	}
	
	protected void Initialize()
	{
		_spellGameObject = transform.GetChild(0).gameObject;
		_spellCollider = GetComponent<Collider2D>();
		_spellNetworkComponent = GetComponent<SpellNetworkComponent>();
	}

	private void Update()
	{
		if(!_isLocalProjectile) return;
		
		// Local projectile visibility code here
		if(Player.LocalClientInstance.CurrentBiome.Value == _biome)
		{
			_spellGameObject.SetActive(true);
			_spellCollider.enabled = true;
			_spellCollider.isTrigger = true;
		}
		else
		{
			_spellGameObject.SetActive(false);
			_spellCollider.enabled = false;
			_spellCollider.isTrigger = false;
		}
	}
	
	protected abstract void CastSpell();
	
	public void InitializeBaseSpell(BiomeType biome, int speed, int damage, Vector3 directionNormalized, ulong spawnPlayerId, int knockback, float lifeTime, ulong projectileId)
	{
		Initialize();
		_spawnPlayerId = spawnPlayerId;
		_projSpawnPoint = transform.position;
		_speed = speed;
		_damage = damage;
		_directionNormalized = directionNormalized;
		_knockback = knockback;
		_lifetime = lifeTime;
		_projectileId = projectileId;
		_biome = biome;
		_isLocalProjectile = !IsServer;
		
		if(IsServer)
		{
			_spellNetworkComponent.InitializeSpellNetwork(_biome, _spawnPlayerId, _lifetime, _projectileId);
		}
		
		CastSpell();
	}

	protected bool PlayerOwnerClientIdEqualsServerId()
	{
		return Player.LocalClientInstance.OwnerClientId == NetworkManager.ServerClientId;
	}

	
	protected bool ColliderIsSourcePlayer(Collider2D col)
	{
		return NetworkManager.ConnectedClients[_spawnPlayerId].PlayerObject == null || NetworkManager.ConnectedClients[_spawnPlayerId].PlayerObject.GetComponent<Player>().HitCollider == col;
	}
}