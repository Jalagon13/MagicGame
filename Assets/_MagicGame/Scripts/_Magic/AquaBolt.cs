using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AquaBolt : Spell
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
		
		if(IsOwner)
		{
            Velocity.Value = _finalDirection * SpellData.Value.Speed;
		}
	}

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!Started.Value || !IsOwner || _isDead) return; //don't do anything before OnNetworkSpawn has run.

        Velocity.Value = Vector2.Lerp(Velocity.Value, Vector2.zero, _velocityDecay * Time.fixedDeltaTime);
        _rigidbody2D.linearVelocity = Velocity.Value;
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
    	_trailParticles.gameObject.transform.parent = null;
    	var main = _trailParticles.main;
    	main.loop = false;
    	main.stopAction = ParticleSystemStopAction.Destroy;

    	base.OnDestroy();
    }
}
