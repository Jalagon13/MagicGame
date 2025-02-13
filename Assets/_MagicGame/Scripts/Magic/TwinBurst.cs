using System;
using UnityEngine;

public class TwinBurst : Spell
{
	[SerializeField] private WallDetectorCollider _wallDetectorCollider;
	[SerializeField] private ParticleSystem _hitParticles;

	private Rigidbody2D _rigidbody2D;
	private int _damageRef;
	private ulong _sourcePlayerIdRef;

	private void Awake()
	{
		_rigidbody2D = GetComponent<Rigidbody2D>();
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if(!Player.LocalClientInstance.IsServer) return;
		
		if(NetworkManager.ConnectedClients[_sourcePlayerIdRef].PlayerObject == null || 
		NetworkManager.ConnectedClients[_sourcePlayerIdRef].PlayerObject.GetComponent<Player>().HitCollider == other) return;
		
		if (other.TryGetComponent(out IHasHealth npcToDamage))
		{
			npcToDamage.ApplyDamage(_damageRef, Player.LocalClientInstance.transform.position);
			StopProjectile();
			return;
		}
		else if(other.gameObject.layer == 9)
		{
			StopProjectile();
		}
	}

	public override void Initialize(BiomeType biome, int speed, int damage, Vector3 directionNormalized, ulong sourcePlayerId, int knockback, float lifetime)
	{
		base.Initialize(biome, speed, damage, directionNormalized, sourcePlayerId, knockback, lifetime);
		
		Vector3 perpendicular1 = new Vector2(directionNormalized.y, -directionNormalized.x).normalized;
		Vector3 perpendicular2 = new Vector2(-directionNormalized.y, directionNormalized.x).normalized;
		
		Vector2 copySpawnPos = transform.position + perpendicular1;
		var go = Instantiate(gameObject, copySpawnPos, Quaternion.identity);
		go.GetComponent<TwinBurst>().StartProjectile(speed, lifetime, damage, sourcePlayerId);
		
		transform.position += perpendicular2;
		
		StartProjectile(speed, lifetime, damage, sourcePlayerId);
	}
	
	public void StartProjectile(int speed, float lifetime, int damage, ulong sourcePlayerId)
	{
		_wallDetectorCollider.OnWallCollide += OnWallCollide;
	
		Vector2 direction = (ActionManager.MouseWorldPosition - (Vector2)transform.position).normalized;
	
		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		_rigidbody2D.AddForce(direction * speed, ForceMode2D.Impulse);
		
		_damageRef = damage;
		_sourcePlayerIdRef = sourcePlayerId;
		
		Invoke(nameof(StopProjectile), lifetime);
	}

	private void OnWallCollide(object sender, WallDetectorCollider.WallCollisionEventArgs e)
	{
		StopProjectile();
	}

	private void StopProjectile()
	{
		var go = Instantiate(_hitParticles.gameObject, transform.position, Quaternion.identity);
		go.GetComponent<ParticleSystem>().Play();
	
		_wallDetectorCollider.OnWallCollide -= OnWallCollide;
		GetComponent<SpellNetworkComponent>().StopProjectile();
	}
}
