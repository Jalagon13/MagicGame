using System;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public abstract class Spell : NetworkBehaviour
{
	protected int _speed;
	protected int _damage;
	protected int _knockback;
	protected float _lifetime;
	protected Vector3 _directionNormalized;
	protected Vector3 _projSpawnPoint;
	protected SpellNetworkComponent _spellNetworkComponent;
	protected ulong _projectileId;
	protected BiomeType _biome;
	protected NetworkVariable<ulong> _sourcePlayerId = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	protected ulong _sourcePlayerIdRef;
	private bool _isLocalProjectile;
	private GameObject _spellGameObject;
	private Collider2D _spellCollider;

	public override void OnNetworkSpawn()
	{
		// transform.GetChild(0).gameObject.SetActive(false);
		base.OnNetworkSpawn();
	}

	void Update()
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

	protected bool PlayerOwnerClientIdEqualsServerId()
	{
		return Player.LocalClientInstance.OwnerClientId == NetworkManager.ServerClientId;
	}

	public void Initialize(BiomeType biome, int speed, int damage, Vector3 directionNormalized, ulong sourcePlayerId, int knockback, float lifeTime, ulong projectileId)
	{
		_spellGameObject = transform.GetChild(0).gameObject;
		_spellCollider = GetComponent<Collider2D>();
	
		_sourcePlayerIdRef = sourcePlayerId;
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
			// Real projectile code here
			_spellNetworkComponent = GetComponent<SpellNetworkComponent>();
			_spellNetworkComponent.InitializeSpellNetwork(_biome, _sourcePlayerIdRef, _lifetime, _projectileId);
			
			if(sourcePlayerId != NetworkManager.ServerClientId)
			{
				NetworkObject.NetworkHide(_sourcePlayerIdRef);
			}
		}
	}
	
	public abstract void CastSpell();
	
	protected bool ColliderIsSourcePlayer(Collider2D col)
	{
		return NetworkManager.ConnectedClients[_sourcePlayerIdRef].PlayerObject == null || NetworkManager.ConnectedClients[_sourcePlayerIdRef].PlayerObject.GetComponent<Player>().HitCollider == col;
	}
}