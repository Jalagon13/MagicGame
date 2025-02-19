using System;
using Unity.Netcode;
using UnityEngine;

public class TwinBurst : Spell
{
	[SerializeField] private WallDetectorCollider _wallDetectorCollider;
	[SerializeField] private ParticleSystem _hitParticles;

	private Rigidbody2D _rigidbody2D;
	private int _damageRef;

	private void Awake()
	{
		_rigidbody2D = GetComponent<Rigidbody2D>();
	}

    void Start()
    {
		_wallDetectorCollider.OnWallCollide += OnWallCollide;
    }

    private void OnTriggerEnter2D(Collider2D other)
	{
		if(!Player.LocalClientInstance.IsServer || ColliderIsSourcePlayer(other)) return;
		
		if (other.TryGetComponent(out IHasHealth npcToDamage))
		{
			npcToDamage.ApplyDamage(_damageRef, Player.LocalClientInstance.transform.position);
			_spellNetworkComponent.StopProjectile();
			return;
		}
		else if(other.gameObject.layer == 9)
		{
			_spellNetworkComponent.StopProjectile();
		}
	}
	
	protected override void CastSpell()
	{
		Vector3 perpendicular1 = new Vector2(_directionNormalized.y, -_directionNormalized.x).normalized;
		Vector3 perpendicular2 = new Vector2(-_directionNormalized.y, _directionNormalized.x).normalized;
		
		Vector2 copySpawnPos = transform.position + perpendicular1;
		GameObject twinBurstCopy = Instantiate(gameObject, copySpawnPos, Quaternion.identity);
		
		if(IsServer)
		{
			Debug.Log($"Initializing the copy of {gameObject.name} to spell network component");
			twinBurstCopy.GetComponent<NetworkObject>().Spawn(true);
			twinBurstCopy.GetComponent<Spell>().InitializeBaseSpell(_biome, _speed, _damage, _directionNormalized, _spawnPlayerId, _knockback, _lifetime, _projectileId);
			twinBurstCopy.GetComponent<TwinBurst>().CastSpell();
		}
		
		twinBurstCopy.GetComponent<TwinBurst>().StartProjectile(_speed, _lifetime, _damage, _spawnPlayerId);
		
		transform.position += perpendicular2;
		
		StartProjectile(_speed, _lifetime, _damage, _spawnPlayerId);
	}
	
	public void StartProjectile(int speed, float lifetime, int damage, ulong sourcePlayerId)
	{
		Vector2 direction = (ActionManager.MouseWorldPosition - (Vector2)transform.position).normalized;
	
		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		_rigidbody2D.AddForce(direction * speed, ForceMode2D.Impulse);
		
		_damageRef = damage;
		_spawnPlayerId = sourcePlayerId;
		
		if(_spellNetworkComponent == null)
			_spellNetworkComponent = GetComponent<SpellNetworkComponent>();
	}

	private void OnWallCollide(object sender, WallDetectorCollider.WallCollisionEventArgs e)
	{
		_spellNetworkComponent.StopProjectile();
	}
	
	public override void OnDestroy()
	{
		_wallDetectorCollider.OnWallCollide -= OnWallCollide;
	
		var go = Instantiate(_hitParticles.gameObject, transform.position, Quaternion.identity);
		go.GetComponent<ParticleSystem>().Play();

		base.OnDestroy();
	}
}
