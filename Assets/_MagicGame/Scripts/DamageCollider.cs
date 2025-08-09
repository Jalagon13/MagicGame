using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDamagerCollider : MonoBehaviour
{
	[SerializeField] private ServerCharacter _serverCharacter;

	private void OnTriggerStay2D(Collider2D other)
	{
		if(!_serverCharacter.IsServer || 
		!other.TryGetComponent(out Player player) || 
		player.CurrentBiome.Value != _serverCharacter.CurrentBiome || 
		player.ServerCharacter.LifeState != LifeState.Alive) return;
		
		// Only detect collisions with players in the same biome.
		DamageReceiver damageReceiver = player.GetComponent<DamageReceiver>();
		damageReceiver.ReceiveHP(_serverCharacter, -_serverCharacter.Data.BaseAttack, true, _serverCharacter.Data.BaseAttackKnockback);
	}
}