using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using Unity.Netcode;
using UnityEngine;

public class MeleeCollider : NetworkBehaviour
{
	[SerializeField] private float _detectionBetweenHits;
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
		if(!IsOwner) return;
	
		if(collision.TryGetComponent(out IHasHealth iHasHealth))
		{
			// If this collider, is it's own, skip it
			if(collision == _thisPlayer.GetComponent<Collider2D>()) return;
			
			if(_entitiesFoundThisSwing == null)
			{
				_entitiesFoundThisSwing = new();
			}
			
			if(!_entitiesFoundThisSwing.Contains(iHasHealth))
			{
				_entitiesFoundThisSwing.Add(iHasHealth);
			}
		}
	}
	
	private IEnumerator HitEnemies()
	{
		if(_entitiesFoundThisSwing.Count > 0)
		{
			foreach (IHasHealth entityToDamage in _entitiesFoundThisSwing.ToArray())
			{
				if (_entitiesHitThisSwing.Contains(entityToDamage)) continue;
				
				entityToDamage.ApplyDamage((_playerHand.HeldItem as WandItemSO).MeleeDamage, transform.root.position, (_playerHand.HeldItem as WandItemSO).MeleeKnockback);
				
				_entitiesFoundThisSwing.Remove(entityToDamage);
				
				SoundManager.Instance.PlayOneShot(FMODEvents.Instance.MeleeHit, Player.LocalClientInstance.transform.position);
					
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
		
		base.OnDestroy();
	}
}
