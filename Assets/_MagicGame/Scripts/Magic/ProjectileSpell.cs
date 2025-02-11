using UnityEngine;

public class ProjectileSpell : Spell
{
	private Rigidbody2D _rigidbody2D;

	private void Awake()
	{
		_rigidbody2D = GetComponent<Rigidbody2D>();
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if(!IsServer) return;
	
		// If is overlapping with the collider attached to the player who sent it, don't damage it
		if(NetworkManager.ConnectedClients[_sourcePlayerId].PlayerObject == null || NetworkManager.ConnectedClients[_sourcePlayerId].PlayerObject.GetComponent<Player>().HitCollider == other) return;

		if (other.TryGetComponent(out IHasHealth npcToDamage))
		{
			npcToDamage.ApplyDamage(_damage, _damagerPosition, 20);
			GetComponent<SpellNetworkComponent>().StopProjectile();
			return;
		}
	}
	
	public override void Initialize(BiomeType biome, int speed, int damage, Vector3 directionNormalized, ulong sourcePlayerId, int knockback, float lifetime)
	{
		base.Initialize(biome, speed, damage, directionNormalized, sourcePlayerId, knockback, lifetime);
		
		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		_rigidbody2D.AddForce(_directionNormalized * _speed, ForceMode2D.Impulse);
	}
}
