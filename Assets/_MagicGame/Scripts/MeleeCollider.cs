using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using Unity.Netcode;
using UnityEngine;

public class MeleeCollider : NetworkBehaviour
{
	[SerializeField] private float _detectionBetweenHits;
	[SerializeField] private AudioClip _smackSound;
	[SerializeField] private PlayerHand _playerHand;

	private MeleeItemSO _meleeItemSO;
	private List<IHasHealth> _entitiesFoundThisSwing;
	private List<IHasHealth> _entitiesHitThisSwing;
	private int _damage;
	private Player _thisPlayer;
	private bool _wandOut;

	private void Awake()
	{
		_thisPlayer = transform.root.GetComponent<Player>();
	}
	
	private void Start()
	{
		_playerHand.OnSwingStart += OnSwingStart;
		_playerHand.OnSwingEnd += OnSwingEnd;
		_playerHand.OnHoldingWandStart += OnHoldingWandStart;
		_playerHand.OnHoldingWandEnd += OnHoldingWandEnd;
	}

	private void OnHoldingWandStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		_wandOut = true;
	}

	private void OnHoldingWandEnd(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		_wandOut = false;
	}

	private void OnSwingStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		if(!IsOwner) return;
		
		_entitiesFoundThisSwing = new();
		_entitiesHitThisSwing = new();
			
		StartCoroutine(HitEnemies());
	}

	private void OnSwingEnd(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		if(!IsOwner) return;
		
		_entitiesFoundThisSwing = new();
		_entitiesHitThisSwing = new();
			
		StopAllCoroutines();
	}
	
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if(!IsOwner || _wandOut) return;
	
		if(collision.TryGetComponent(out IHasHealth iHasHealth))
		{
			// If this collider, is it's own, skip it
			if(collision == _thisPlayer.GetComponent<Collider2D>()) return;
			
			if(_entitiesFoundThisSwing == null)
			{
				_entitiesFoundThisSwing = new();
			}
			
			_entitiesFoundThisSwing.Add(iHasHealth);
		}
	}
	
	private IEnumerator HitEnemies()
	{
		if(_entitiesFoundThisSwing.Count > 0)
		{
			foreach (IHasHealth entityToDamage in _entitiesFoundThisSwing.ToArray())
			{
				if (_entitiesHitThisSwing.Contains(entityToDamage)) continue;

				entityToDamage.ApplyDamage(8, transform.root.position);
				
				_entitiesFoundThisSwing.Remove(entityToDamage);
				
				MMSoundManagerSoundPlayEvent.Trigger(_smackSound, MMSoundManager.MMSoundManagerTracks.Sfx, default, pitch: UnityEngine.Random.Range(1.2f, 1.3f), volume: 0.85f);
					
				if(entityToDamage != null)
				{
					_entitiesHitThisSwing.Add(entityToDamage);
				}
				
				yield return new WaitForSeconds(_detectionBetweenHits);
			}
		}
			
		yield return null;

		StartCoroutine(HitEnemies());
	}
	
	public override void OnDestroy()
	{
		_playerHand.OnSwingStart -= OnSwingStart;
		_playerHand.OnSwingEnd -= OnSwingEnd;
		_playerHand.OnHoldingWandStart -= OnHoldingWandStart;
		_playerHand.OnHoldingWandEnd -= OnHoldingWandEnd;
		
		base.OnDestroy();
	}
}
