using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BouncyBattleBall : Spell
{
	[SerializeField] private WallDetectorCollider _wallDetectorCollider;
	[SerializeField] private float _velocityDecay = 5f; 
	[SerializeField] private float _damageTimer = 0.16f;
	[SerializeField] private float _rotationSpeedMultiplier = 50f;
	[SerializeField] private SpriteRenderer _projectileSr;
	
	private Rigidbody2D _rigidbody2D;
	private CircleCollider2D _collider;
	private Dictionary<IHasHealth, float> _hitNpcList = new();
	private Vector2 _velocity;

	private void Awake()
	{
		_rigidbody2D = GetComponent<Rigidbody2D>();
		_collider = GetComponent<CircleCollider2D>();
		_wallDetectorCollider.OnWallCollide += OnWallCollide;
	}

	private void OnWallCollide(object sender, WallDetectorCollider.WallCollisionEventArgs e)
	{
		var direction = Vector2.Reflect(_velocity, e.ContactNormal).normalized;
		_velocity = direction * _velocity.magnitude;
	}
	
	private void FixedUpdate()
	{
		// Rotate based on velocity magnitude
		float spinSpeed = _velocity.magnitude * _rotationSpeedMultiplier;
		_projectileSr.transform.Rotate(0, 0, spinSpeed * Time.fixedDeltaTime);
	
		_velocity = Vector2.Lerp(_velocity, Vector2.zero, _velocityDecay * Time.fixedDeltaTime);
		_rigidbody2D.MovePosition(_rigidbody2D.position + _velocity * Time.fixedDeltaTime);

		UpdateTimers();
	}

	void OnTriggerEnter2D(Collider2D collider)
	{
		if (ColliderIsSourcePlayer(collider)) return;
		
		if (collider.TryGetComponent(out IHasHealth npcToDamage))
		{
			if (_hitNpcList.ContainsKey(npcToDamage)) return;
			
			if(IsServer)
			{
				npcToDamage.ApplyDamage(_damage, _projSpawnPoint, _knockback);
				_hitNpcList.Add(npcToDamage, _damageTimer);
					
				_spellNetworkComponent.StopProjectile();
			}
		}
	}

	private void UpdateTimers()
	{
		// Update damage timers and remove expired entries
		List<IHasHealth> npcsToRemove = new();
		var hitNpcKeys = new List<IHasHealth>(_hitNpcList.Keys);
		for (int i = 0; i < hitNpcKeys.Count; i++)
		{
			var npc = hitNpcKeys[i];
			_hitNpcList[npc] -= Time.fixedDeltaTime;
			if (_hitNpcList[npc] <= 0)
				npcsToRemove.Add(npc);
		}
		for (int i = 0; i < npcsToRemove.Count; i++)
		{
			var npc = npcsToRemove[i];
			_hitNpcList.Remove(npc);
		}
	}
	
	public override void CastSpell()
	{
		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		_velocity = _directionNormalized * _speed;
	}

	public override void OnDestroy()
	{
		_wallDetectorCollider.OnWallCollide -= OnWallCollide;
		
		base.OnDestroy();
	}
}
