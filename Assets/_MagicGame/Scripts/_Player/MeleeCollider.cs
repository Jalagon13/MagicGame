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
    private ToolItemSO _staffItemSO;
    
    private void Awake()
    {
        _meleeCollider = GetComponent<Collider2D>();
        _meleeCollider.enabled = false;
    }

    public void StartSwing(ToolItemSO staffItemSO)
    {
        if (!IsOwner) return;
        
        _staffItemSO = staffItemSO;
        _targetsFound = new();
        _targetsHit = new();
        _meleeCollider.enabled = true;

        StartCoroutine(HitFoundTargets());
    }

    public void EndSwing()
    {
        if (!IsOwner) return;
        
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
        
        if(collision.gameObject.layer == 16)
        {
            Vector2Int tilePos = new Vector2Int((int)collision.gameObject.transform.position.x, (int)collision.gameObject.transform.position.y);
            int tileId = GameManager.Instance.GetTileIDFromTilemapTilePosition(TileRenderManager.Instance.FoliageTm, (Vector3Int)tilePos);
            TileRenderManager.Instance.DestroyTileServerRpc(tilePos, tileId, Player.LocalClientInstance.CurrentPlayerBiome.Value);
            Destroy(collision.gameObject);
        }
    }

    private IEnumerator HitFoundTargets()
    {
        if(_targetsFound.Count > 0)
        {
            foreach (NetworkHealthState targetToDamage in _targetsFound.ToArray())
            {
                if(_targetsHit.Contains(targetToDamage)) continue;
                
                targetToDamage.TakeDamageRpc(_staffItemSO.MeleeDamage, Player.LocalClientInstance.transform.position, _staffItemSO.Knockback);
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
