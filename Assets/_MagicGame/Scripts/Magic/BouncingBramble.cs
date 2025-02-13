using System.Collections.Generic;
using UnityEngine;

public class BouncingBramble : Spell
{
	[SerializeField] private float _damageTimer = 0.16f;
	[SerializeField] private float _rotationSpeedMultiplier = 50f;
	[SerializeField] private SpriteRenderer _projectileSr;
	private Rigidbody2D _rigidbody2D;
	private CircleCollider2D _collider;
	private Dictionary<IHasHealth, float> _hitNpcList = new();

	private void Awake()
	{
		_rigidbody2D = GetComponent<Rigidbody2D>();
		_collider = GetComponent<CircleCollider2D>();
	}
	
	private void FixedUpdate()
	{
		// Rotate based on velocity magnitude
		float spinSpeed = _rigidbody2D.linearVelocity.magnitude * _rotationSpeedMultiplier;
		_projectileSr.transform.Rotate(0, 0, spinSpeed * Time.fixedDeltaTime);
    
		if (IsServer)
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

			Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _collider.radius);
			foreach (var collider in hitColliders)
			{
				if (NetworkManager.ConnectedClients[_sourcePlayerId].PlayerObject == null || 
					NetworkManager.ConnectedClients[_sourcePlayerId].PlayerObject.GetComponent<Player>().HitCollider == collider) 
					continue;
            
				if (collider.TryGetComponent(out IHasHealth npcToDamage))
				{
					if (_hitNpcList.ContainsKey(npcToDamage)) continue;
            
					npcToDamage.ApplyDamage(_damage, transform.position, _knockback + Mathf.RoundToInt(_rigidbody2D.linearVelocity.magnitude * 0.5f));
					_hitNpcList.Add(npcToDamage, _damageTimer);
                
					var velocity = _rigidbody2D.linearVelocity;
					var direction = (transform.position - collider.transform.position).normalized;
					_rigidbody2D.linearVelocity = Vector2.zero;

					_rigidbody2D.AddForce(direction * (velocity.magnitude * 0.5f), ForceMode2D.Impulse);
				}
			}
		}
	}
	
	public override void Initialize(BiomeType biome, int speed, int damage, Vector3 directionNormalized, ulong sourcePlayerId, int knockback, float lifetime)
	{
		base.Initialize(biome, speed, damage, directionNormalized, sourcePlayerId, knockback, lifetime);
		
		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		_rigidbody2D.AddForce(_directionNormalized * _speed, ForceMode2D.Impulse);
	}
}
