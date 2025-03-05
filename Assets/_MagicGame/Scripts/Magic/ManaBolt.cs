using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ManaBolt : Spell
{
	[SerializeField] private ParticleSystem _hitParticles;
	[SerializeField] private ParticleSystem _trailParticles;
	[SerializeField] private WallColliderDetector _wallDetectorCollider;
	[SerializeField] private float _velocityDecay = 5f; 
	
	private Rigidbody2D _rigidbody2D;
	private Vector2 _velocity;

	private void Awake()
	{
		_rigidbody2D = GetComponent<Rigidbody2D>();
		_wallDetectorCollider.OnTouchingWall += OnWallCollide;
		
	}

	public override void OnNetworkSpawn()
	{
		// if(IsServer)
		// {
		// 	_trailParticles.gameObject.SetActive(false);
		// }
	
		base.OnNetworkSpawn();
	}

	private void FixedUpdate()
	{
		if(IsServer /* || _isLocalProjectile */)
		{
			
		}

		_velocity = Vector2.Lerp(_velocity, Vector2.zero, _velocityDecay * Time.fixedDeltaTime);
		_rigidbody2D.linearVelocity = _velocity;
	}

	private void OnWallCollide(object sender, WallColliderDetector.WallCollisionEventArgs e)
	{
		PlayHitParticles();
		_spellNetworkComponent.StopProjectile();
	}
	
	public override void CastSpell()
	{
		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		_velocity = _spellData.Direction * _spellData.Speed;
		Debug.Log($"Velocity: {_velocity}, Direction : {_spellData.Direction}, Speed : {_spellData.Speed}");
	}

	private void OnTriggerEnter2D(Collider2D collider)
	{
		if (ColliderIsSourcePlayer(collider)) return;
		
		if (collider.TryGetComponent(out IHasHealth npcToDamage))
		{
			if(collider.TryGetComponent(out Player player) && !player.PvpEnabled.Value) return;
		
			PlayHitParticles();
		
			if(_spellData.SpawnPlayerId == Player.LocalClientInstance.OwnerClientId)
			{
				npcToDamage.ApplyDamage(_spellData.Damage, _spellData.SpawnPoint, _spellData.Knockback);
			}
			
			if(IsServer)
			{
				_spellNetworkComponent.StopProjectile();
			}
		}
	}
	
	private void PlayHitParticles()
	{
		if(_spellGameObject.activeInHierarchy)
		{
			var go = Instantiate(_hitParticles.gameObject, transform.position, Quaternion.identity);
			go.GetComponent<ParticleSystem>().Play();
		}
	}
	
	public override void OnDestroy()
	{
		_wallDetectorCollider.OnTouchingWall -= OnWallCollide;

		_trailParticles.gameObject.transform.parent = null;
		var main = _trailParticles.main;
		main.loop = false;
		main.stopAction = ParticleSystemStopAction.Destroy;

		base.OnDestroy();
	}
}
