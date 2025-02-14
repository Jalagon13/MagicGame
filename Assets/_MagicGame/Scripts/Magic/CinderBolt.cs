using System.Collections.Generic;
using UnityEngine;

public class CinderBolt : Spell
{
	[SerializeField] private int _pierceMax;
	[SerializeField] private float _explosionRadius;
	[SerializeField] private ParticleSystem _detonateParticles;

	private Rigidbody2D _rigidbody2D;
	private List<IHasHealth> _npcsFound = new();
	private int _pierceCount;

	private void Awake()
	{
		_rigidbody2D = GetComponent<Rigidbody2D>();
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!IsServer || ColliderIsSourcePlayer(other)) return;
		
		if (other.TryGetComponent(out IHasHealth npcToDamage))
		{
			if(!_npcsFound.Contains(npcToDamage))
			{
				if(_pierceCount >= _pierceMax)
				{
					// Detonate
					GetComponent<SpellNetworkComponent>().StopProjectile();
				}
				else
				{
					npcToDamage.ApplyDamage(_damage, _damagerPosition);
				
					_npcsFound.Add(npcToDamage);
					_pierceCount++;
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

	public override void OnDestroy()
	{
		var go = Instantiate(_detonateParticles.gameObject, transform.position, Quaternion.identity);
		go.GetComponent<ParticleSystem>().Play();

		if (IsServer)
		{
			Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, _explosionRadius);
			foreach (var collider in hitColliders)
			{
				if(ColliderIsSourcePlayer(collider)) continue;
			
				if (collider.TryGetComponent(out IHasHealth npcToDamage))
				{
					npcToDamage.ApplyDamage(_damage, transform.position, _knockback);
				}
			}
		}

		base.OnDestroy();
	}
}
