using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ManaBolt : Spell
{
	[SerializeField] private ParticleSystem _hitParticles;
	[SerializeField] private ParticleSystem _trailParticles;
	// [SerializeField] private WallColliderDetector _wallDetectorCollider;
	[SerializeField] private float _velocityDecay = 5f; 
	
	private Rigidbody2D _rigidbody2D;
	private Vector2 _velocity;

    protected override void Awake()
    {
        base.Awake();

		_rigidbody2D = GetComponent<Rigidbody2D>();
    }

    protected override void SpellSetUp()
    {
        base.SpellSetUp();

		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		
		if(IsServer || _isLocalSpell)
		{
			_velocity = _spellData.Direction * _spellData.Speed;
		}
		Debug.Log($"Mana Bolt Set up");
	}

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

		if ((IsServer || _isLocalSpell) && _started)
		{
			_velocity = Vector2.Lerp(_velocity, Vector2.zero, _velocityDecay * Time.fixedDeltaTime);
			_rigidbody2D.linearVelocity = _velocity;
		}
	}

    // private void Awake()
    // {
    // 	_rigidbody2D = GetComponent<Rigidbody2D>();
    // 	_wallDetectorCollider.OnTouchingWall += OnWallCollide;

    // }

    // private void FixedUpdate()
    // {
    // 	if(!IsServer || !_started)
    // 	{
    // 		return;
    // 	}

    // 	_velocity = Vector2.Lerp(_velocity, Vector2.zero, _velocityDecay * Time.fixedDeltaTime);
    // 	_rigidbody2D.linearVelocity = _velocity;
    // }

    // private void OnWallCollide(object sender, WallColliderDetector.WallCollisionEventArgs e)
    // {
    // 	PlayHitParticles();
    // 	// _spellNetworkComponent.StopProjectile();
    // }

    // public override void CastSpell()
    // {
    // 	// _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
    // 	// _velocity = _serverSpellData.Direction * _serverSpellData.Speed;
    // 	// Debug.Log($"Velocity: {_velocity}, Direction : {_serverSpellData.Direction}, Speed : {_serverSpellData.Speed}");
    // }

    // private void OnTriggerEnter2D(Collider2D collider)
    // {
    // 	if (ColliderIsSourcePlayer(collider)) return;

    // 	// if (collider.TryGetComponent(out IHasHealth npcToDamage))
    // 	// {
    // 	// 	if(collider.TryGetComponent(out Player player) && !player.PvpEnabled.Value) return;

    // 	// 	PlayHitParticles();

    // 	// 	if(_serverSpellData.SpawnPlayerId == Player.LocalClientInstance.OwnerClientId)
    // 	// 	{
    // 	// 		npcToDamage.ApplyDamage(_serverSpellData.Damage, _serverSpellData.SpawnPoint, _serverSpellData.Knockback);
    // 	// 	}

    // 	// 	if(IsServer)
    // 	// 	{
    // 	// 		_spellNetworkComponent.StopProjectile();
    // 	// 	}
    // 	// }
    // }

    // private void PlayHitParticles()
    // {
    // 	if(_spellGameObject.activeInHierarchy)
    // 	{
    // 		var go = Instantiate(_hitParticles.gameObject, transform.position, Quaternion.identity);
    // 		go.GetComponent<ParticleSystem>().Play();
    // 	}
    // }

    // public override void OnDestroy()
    // {
    // 	// _wallDetectorCollider.OnTouchingWall -= OnWallCollide;

    // 	_trailParticles.gameObject.transform.parent = null;
    // 	var main = _trailParticles.main;
    // 	main.loop = false;
    // 	main.stopAction = ParticleSystemStopAction.Destroy;

    // 	base.OnDestroy();
    // }
}
