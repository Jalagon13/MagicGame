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
	
	public override void OnNetworkSpawn()
	{
		// if(IsServer)
		// {
		// 	_sourcePlayerId.Value = _sourcePlayerIdRef;
		// }
	
		if(Player.LocalClientInstance.OwnerClientId == _sourcePlayerId.Value && Player.LocalClientInstance.OwnerClientId != NetworkManager.ServerClientId)
		{
			// On player who spawned this projectile
			Debug.Log($"On the source player's instance, disabling gameobject");
			// transform.GetChild(0).gameObject.SetActive(false);
		}
		
		base.OnNetworkSpawn();
	}
	
	protected bool PlayerOwnerClientIdEqualsServerId()
	{
		return Player.LocalClientInstance.OwnerClientId == NetworkManager.ServerClientId;
	}

	public void Initialize(BiomeType biome, int speed, int damage, Vector3 directionNormalized, ulong sourcePlayerId, int knockback, float lifeTime, ulong projectileId)
	{
		_sourcePlayerIdRef = sourcePlayerId;
		_projSpawnPoint = transform.position;
		_speed = speed;
		_damage = damage;
		_directionNormalized = directionNormalized;
		_knockback = knockback;
		_lifetime = lifeTime;
		_projectileId = projectileId;
		_biome = biome;
		
		if(IsServer)
		{
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