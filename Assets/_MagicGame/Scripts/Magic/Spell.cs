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
	protected ulong _sourcePlayerIdRef;

	protected bool PlayerOwnerClientIdEqualsServerId()
	{
		return Player.LocalClientInstance.OwnerClientId == NetworkManager.ServerClientId;
	}

	public void InitializeBaseSpell(BiomeType biome, int speed, int damage, Vector3 directionNormalized, ulong sourcePlayerId, int knockback, float lifeTime, ulong projectileId)
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