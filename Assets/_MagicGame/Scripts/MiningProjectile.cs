using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class MiningProjectile : NetworkBehaviour
{
	[SerializeField] private AudioClip _castSound;
	[SerializeField] private AudioClip _hitSound;
	[SerializeField] private GameObject _muzzleVfxPrefab;
	[SerializeField] private GameObject _hitVfxPrefab;
	[SerializeField] private float _speed = 10f;
	
	private ParticleSystem _projectileParticleSystem;
	private Vector2 _travelPoint;
	private int _miningPower;
	private bool _projectileEnd = true;
	private bool _mouseOverFloor, _mouseOverWall, _resourceSelected;
	
	private void Awake()
	{
		_projectileParticleSystem = transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>();
	}
	
	// Spawn muzzle vfx prefab and then destroy it when it is done.
	private void Start()
	{
		if(_muzzleVfxPrefab != null)
		{
			var muzzleVFX = Instantiate(_muzzleVfxPrefab, transform.position, Quaternion.identity);
			var psMuzzle = muzzleVFX.transform.GetChild(0).GetComponent<ParticleSystem>();
			if(psMuzzle != null)
			{
				Destroy(muzzleVFX, psMuzzle.main.duration + psMuzzle.main.startLifetime.constantMax);
			}
		}
	}
	
	public void InitializeMiningSpell(Vector2 travelPoint, int miningPower, bool mouseOverFloor, bool mouseOverWall, bool resourceSelected)
	{
		_travelPoint = travelPoint;
		_miningPower = miningPower;
		_mouseOverFloor = mouseOverFloor;
		_mouseOverWall = mouseOverWall;
		_resourceSelected = resourceSelected;
		_projectileEnd = false;
	}
	
	private void FixedUpdate()
	{
		if(_projectileEnd) return;
		
		// Move the orb towards the target position.
		transform.position = Vector3.MoveTowards(transform.position, _travelPoint, _speed * Time.deltaTime);
		
		// Check if the orb has reached the target position, that means clickable is broken and should not consume mana.
		if((_mouseOverFloor || _mouseOverWall) && !_resourceSelected)
		{
			if (Vector3.Distance(transform.position, _travelPoint) < 0.03f)
			{
				if(IsServer)
				{
					HitTilemap();
				}
				
				SpawnHitPrefab();
				StopProjectile();
				
				return;
			}
		}
		
		// NTFS: There is a tiny chance that this will simply not work if the transform.position is 0.03 away from the target position and it does not find any colliders. If so just destroy it anyway
		if(_resourceSelected)
		{
			if (Vector3.Distance(transform.position, _travelPoint) < 0.03f)
			{
				var colliders = Physics2D.OverlapPointAll(transform.position);
				
				if(IsServer)
				{
					foreach(Collider2D collider in colliders)
					{
						if(collider.TryGetComponent(out ResourceObject resourceAsset))
						{
							Vector2Int resourcePosition = new(Mathf.RoundToInt(resourceAsset.transform.position.x), Mathf.RoundToInt(resourceAsset.transform.position.y));
							ObjectManager.Instance.DamageObject(resourcePosition, (ushort)_miningPower, Player.LocalClientInstance.CurrentBiome.Value);
						}
					}
				}
				
				SpawnHitPrefab();
				StopProjectile();
						
				return;
				
			}
		}
	}

	private void HitTilemap()
	{
		Vector2Int tilePos = new(Mathf.FloorToInt(_travelPoint.x), Mathf.FloorToInt(_travelPoint.y));
	
		if(_mouseOverWall)
		{
			Environment.Instance.WallTmData.HitTile(tilePos, _miningPower, Player.LocalClientInstance.CurrentBiome.Value);
			return;
		}
		else if(_mouseOverFloor)
		{
			Environment.Instance.FloorTmData.HitTile(tilePos, _miningPower, Player.LocalClientInstance.CurrentBiome.Value);
			return;
		}
	}

	private void StopProjectile()
	{
		_projectileEnd = true;

		MMSoundManagerSoundPlayEvent.Trigger(_hitSound, MMSoundManager.MMSoundManagerTracks.Sfx, default, pitch: Random.Range(1f, 1.2f), volume: 0.65f);

		float totalDuration = 0f;
		
		if (_projectileParticleSystem != null)
		{
			_projectileParticleSystem.Stop();

			totalDuration = _projectileParticleSystem.main.duration + _projectileParticleSystem.main.startLifetime.constantMax;
		}

		// Destroy the particle system game object after it finishes
		Destroy(gameObject, totalDuration);
	}
	
	private void SpawnHitPrefab()
	{
		if(_hitVfxPrefab != null)
		{
			var hitVFX = Instantiate(_hitVfxPrefab, transform.position, Quaternion.identity);
			var psHit = hitVFX.transform.GetChild(0).GetComponent<ParticleSystem>();
			if(psHit != null)
			{
				Destroy(hitVFX, psHit.main.duration + psHit.main.startLifetime.constantMax);
			}
		}
	}
}
