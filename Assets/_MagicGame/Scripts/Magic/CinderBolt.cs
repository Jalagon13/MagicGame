using System.Collections.Generic;
using UnityEngine;

public class CinderBolt : Spell
{
	[SerializeField] private int _pierceMax;
	[SerializeField] private float _explosionRadius;
	[SerializeField] private ParticleSystem _detonateParticles;
	[SerializeField] private float _damageTimer = 0.16f;

	private Rigidbody2D _rigidbody2D;
	private List<IHasHealth> _npcsFound = new();
	private int _pierceCount;
	private Dictionary<IHasHealth, float> _hitNpcList = new();

	private void Awake()
	{
		_rigidbody2D = GetComponent<Rigidbody2D>();
	}

	// private void OnWallCollide(object sender, WallDetectorCollider.WallCollisionEventArgs e)
	// {
	// 	var direction = Vector2.Reflect(_velocity, e.ContactNormal).normalized;
	// 	_velocity = direction * _velocity.magnitude;
	// }

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!PlayerOwnerClientIdEqualsServerId() || ColliderIsSourcePlayer(other)) return;
		
		if (other.TryGetComponent(out IHasHealth npcToDamage))
		{
			if(!_npcsFound.Contains(npcToDamage))
			{
				if(_pierceCount >= _pierceMax)
				{
					_spellNetworkComponent.StopProjectile(); // -> Triggers detonation
				}
				else
				{
					npcToDamage.ApplyDamage(_damage, _projSpawnPoint);
				
					_npcsFound.Add(npcToDamage);
					_pierceCount++;
				}
			}
		}
	}
	
	
	protected override void CastSpell()
	{
		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		_rigidbody2D.linearVelocity = _directionNormalized * _speed;
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

	public override void OnDestroy()
	{
		Debug.Log($"{name}");
	
		var go = Instantiate(_detonateParticles.gameObject, transform.position, Quaternion.identity);
		go.GetComponent<ParticleSystem>().Play();

		if (IsServer)
		{
			Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _explosionRadius);
			foreach (var collider in hitColliders)
			{
				if(ColliderIsSourcePlayer(collider)) continue;
			
				if (collider.TryGetComponent(out IHasHealth npcToDamage2))
				{
					npcToDamage2.ApplyDamage(_damage, transform.position, _knockback);
				}
			}
		}

		base.OnDestroy();
	}
}
