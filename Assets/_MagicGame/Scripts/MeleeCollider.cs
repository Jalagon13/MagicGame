using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using Unity.Netcode;
using UnityEngine;

public class MeleeCollider : NetworkBehaviour
{
	[SerializeField] private float _detectionBetweenHits;
	[SerializeField] private AudioClip _smackSound;


	private MeleeItemSO _meleeItemSO;
	private List<IHasHealth> _entitiesFoundThisSwing;
	private List<IHasHealth> _entitiesHitThisSwing;
	private int _damage;
	private Player _thisPlayer;
	
	public int Damage { get => _damage; set => _damage = value; }

	private void Awake()
	{
		_thisPlayer = transform.root.GetComponent<Player>();
	}

	private void OnEnable()
	{
		if(!IsOwner) return;
	
		_entitiesFoundThisSwing = new();
		_entitiesHitThisSwing = new();
			
		StartCoroutine(HitEnemies());
	}

	private void OnDisable()
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

				entityToDamage.ApplyDamage(_damage, transform.root.position);
				
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
}
