using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ManaBolt : Spell
{
	[SerializeField] private ParticleSystem _hitParticles;
	[SerializeField] private ParticleSystem _trailParticles;
	[SerializeField] private float _velocityDecay = 5f; 
	
	private Rigidbody2D _rigidbody2D;
	

    protected override void Awake()
    {
        base.Awake();

		_rigidbody2D = GetComponent<Rigidbody2D>();
    }

    public override void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
    {
        base.ExecuteSpellStart(finalDirection, spawnPoint);

		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		
		if(IsServer)
		{
			Velocity = _finalDirection * SpellDataNV.Value.Speed;
		}
	}

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if(_isDead) return;

        Velocity = Vector2.Lerp(Velocity, Vector2.zero, _velocityDecay * Time.fixedDeltaTime);
        _rigidbody2D.linearVelocity = Velocity;

        if(_miningSpellMod == null) return;
        
        Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, _spellCollider.radius, CollisionMask);
        for (int i = 0; i < collisions.Length; i++)
        {
            if(collisions[i].gameObject.layer == WallMask)
            {
                if (collisions[i].TryGetComponent(out PathfindingWallTm pfWall))
                {
                    if (pfWall.BiomeSameAs(SpellDataNV.Value.SpawnBiome))
                    {
                        _miningSpellMod.TryToHitTiles(_spellCollider.radius);
                    }
                }
            }
        }
    }

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
