using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MeleeCollider : NetworkBehaviour
{
    [field: SerializeField] public float DetectionBetweenHitsDuration { get; private set; }

    private List<NetworkHealthState> _targetsFound = new();
    private List<NetworkHealthState> _targetsHit = new();
    private Collider2D _meleeCollider;
    private WandItemSO _staffItemSO;
    
    private void Awake()
    {
        _meleeCollider = GetComponent<Collider2D>();
        _meleeCollider.enabled = false;
    }

    public void StartSwing(WandItemSO staffItemSO)
    {
        if (!IsOwner) return;
        Debug.Log("Swing started");
        _staffItemSO = staffItemSO;
        _targetsFound = new();
        _targetsHit = new();
        _meleeCollider.enabled = true;

        StartCoroutine(HitFoundTargets());
    }

    public void EndSwing()
    {
        if (!IsOwner) return;
        Debug.Log("Swing Ended");
        _targetsFound = new();
        _targetsHit = new();
        _meleeCollider.enabled = false;

        StopAllCoroutines();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (collision.TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(Player.LocalClientInstance.CurrentPlayerBiome.Value))
        {
            _targetsFound.Add(npcNet.gameObject.GetComponent<NetworkHealthState>());
        }
    }

    private IEnumerator HitFoundTargets()
    {
        if(_targetsFound.Count > 0)
        {
            foreach (NetworkHealthState targetToDamage in _targetsFound.ToArray())
            {
                if(_targetsHit.Contains(targetToDamage)) continue;
                
                targetToDamage.TakeDamageRpc(_staffItemSO.MeleeDamage, Player.LocalClientInstance.MainHand.SpellSpawnTransform.position, _staffItemSO.Knockback);
                _targetsFound.Remove(targetToDamage);
                _staffItemSO.PlayHitSound();
                _targetsHit.Add(targetToDamage);
                
                yield return new WaitForSeconds(DetectionBetweenHitsDuration);
            }
        }
        
        yield return null;
        StartCoroutine(HitFoundTargets());
    }
}
