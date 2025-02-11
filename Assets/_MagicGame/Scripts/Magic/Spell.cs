using System;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Spell : NetworkBehaviour
{
	protected int _speed;
	protected int _damage;
	protected int _knockback;
	protected float _lifeTime;
	protected Vector3 _directionNormalized;
	protected Vector3 _damagerPosition;
	protected ulong _sourcePlayerId;

	public virtual void Initialize(BiomeType biome, int speed, int damage, Vector3 directionNormalized, ulong sourcePlayerId, int knockback, float lifeTime)
	{
		_sourcePlayerId = sourcePlayerId;
		_damagerPosition = transform.position;
		_speed = speed;
		_damage = damage;
		_directionNormalized = directionNormalized;
		_knockback = knockback;
		_lifeTime = lifeTime;
	}
}