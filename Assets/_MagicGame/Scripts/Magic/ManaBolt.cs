using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ManaBolt : Spell
{
	[SerializeField] private ParticleSystem _hitParticles;
	[SerializeField] private ParticleSystem _trailParticles;
	[SerializeField] private WallDetectorCollider _wallDetectorCollider;
	[SerializeField] private float _velocityDecay = 5f; 
	
	private Rigidbody2D _rigidbody2D;
	private Vector2 _velocity;

	private void Awake()
	{
		_rigidbody2D = GetComponent<Rigidbody2D>();
		_wallDetectorCollider.OnWallCollide += OnWallCollide;
		_trailParticles.gameObject.transform.parent = null;
		
		Initialize();
	}

	public override void OnNetworkSpawn()
	{
		if(IsServer)
		{
			_trailParticles.gameObject.SetActive(false);
		}
	
		base.OnNetworkSpawn();
	}

	private void FixedUpdate()
	{
		if(IsServer || _isLocalProjectile)
		{
			_velocity = Vector2.Lerp(_velocity, Vector2.zero, _velocityDecay * Time.fixedDeltaTime);
			_rigidbody2D.MovePosition(_rigidbody2D.position + _velocity * Time.fixedDeltaTime);
		}
		
		_trailParticles.gameObject.SetActive(_spellGameObject.activeInHierarchy);
		_trailParticles.transform.SetPositionAndRotation(_rigidbody2D.position, Quaternion.identity);
	}

	private void OnWallCollide(object sender, WallDetectorCollider.WallCollisionEventArgs e)
	{
		PlayHitParticles();
		_spellNetworkComponent.StopProjectile();
	}
	
	protected override void CastSpell()
	{
		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		_velocity = _directionNormalized * _speed;
	}

	void OnTriggerEnter2D(Collider2D collider)
	{
		if (ColliderIsSourcePlayer(collider)) return;
		
		if (collider.TryGetComponent(out IHasHealth npcToDamage))
		{
			if(collider.TryGetComponent(out Player player) && !player.PvpEnabled.Value) return;
		
			PlayHitParticles();
		
			if(_spawnPlayerId == Player.LocalClientInstance.OwnerClientId)
			{
				npcToDamage.ApplyDamage(_damage, _projSpawnPoint, _knockback);
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
		_wallDetectorCollider.OnWallCollide -= OnWallCollide;

		var main = _trailParticles.main;
		main.loop = false;
		main.stopAction = ParticleSystemStopAction.Destroy;

		base.OnDestroy();
	}
}
