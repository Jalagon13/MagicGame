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
	
	private Vector2 _travelPoint;
	private Collider2D _spellCollider;
	private int _miningPower;
	private bool _projectileEnd = true;
	private bool _mouseOverFloor, _mouseOverWall, _resourceSelected;
	
	private void Awake()
	{
		_spellCollider = GetComponent<Collider2D>();
	}
	
	// Spawn muzzle vfx prefab and then destroy it when it is done.
	private void Start()
	{
		MMSoundManagerSoundPlayEvent.Trigger(_castSound, MMSoundManager.MMSoundManagerTracks.Sfx, default, pitch: Random.Range(0.9f, 1.1f), volume: 0.65f);
		
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
				// StatManager.Instance.RemoveFromStat(StatManager.Stat.Mana, 1);// Change hard coded 1 in the future
			
				// Spawn hit prefab.
				SpawnHitPrefab();
			
				if(IsServer)
				{
					// HitTilemap
					HitTilemap();
				}
			
				// Just destroy gameobject if clickable is destroyed already.
				StopProjectile();
				
				return;
			}
		}
		
		// If collider to check is not broken.
		if(_resourceSelected)
		{
			var colliders = Physics2D.OverlapPointAll(transform.position);

			foreach(Collider2D collider in colliders)
			{
				if(collider.TryGetComponent(out ResourceObject resourceAsset))
				{
					if(_spellCollider.IsTouching(collider))
					{
						// StatManager.Instance.RemoveFromStat(StatManager.Stat.Mana, 1);// Change hard coded 1 in the future
						if(IsServer)
						{
							// Register hit.
							Vector2Int resourcePosition = new(Mathf.RoundToInt(resourceAsset.transform.position.x), Mathf.RoundToInt(resourceAsset.transform.position.y));
							ObjectManager.Instance.DamageObject(resourcePosition, (ushort)_miningPower, Player.LocalClientInstance.GetPlayerEnvironment());
						}
				
						// Spawn hit prefab.
						SpawnHitPrefab();
				
						// End the projectile.
						StopProjectile();
						
						return;
					}
				}
			}
		}
	}

	private void HitTilemap()
	{
		Vector2Int tilePos = new(Mathf.FloorToInt(_travelPoint.x), Mathf.FloorToInt(_travelPoint.y));
	
		if(_mouseOverWall)
		{
			Environment.Instance.GetWallTilemapData().HitTile(tilePos, _miningPower, Player.LocalClientInstance.GetPlayerEnvironment());
			return;
		}
		else if(_mouseOverFloor)
		{
			Environment.Instance.GetFloorTilemapData().HitTile(tilePos, _miningPower, Player.LocalClientInstance.GetPlayerEnvironment());
			return;
		}
	}

	private void StopProjectile()
	{
		_projectileEnd = true;
		transform.GetChild(0).gameObject.SetActive(false);
		MMSoundManagerSoundPlayEvent.Trigger(_hitSound, MMSoundManager.MMSoundManagerTracks.Sfx, default, pitch: Random.Range(1f, 1.2f), volume: 0.65f);
		Destroy(gameObject);
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
