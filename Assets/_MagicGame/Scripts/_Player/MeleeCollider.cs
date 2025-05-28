using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class MeleeCollider : NetworkBehaviour
{
    public struct SwingData
    {
        public int Damage;
        public int Knockback;
        public float DetectionBetweenHitsDuration;
        public EventReference HitSound;
        public float ColliderLength;
    }
    private List<NetworkHealthState> _targetsFound = new();
    private List<NetworkHealthState> _targetsHit = new();
    private BoxCollider2D _meleeCollider;
    private SwingData _currentSwingData;

    private void Awake()
    {
        _meleeCollider = GetComponent<BoxCollider2D>();
        _meleeCollider.enabled = false;
    }

    public void StartSwing(SwingData swingData)
    {
        if (!IsOwner) return;

        _currentSwingData = swingData;
        _targetsFound = new();
        _targetsHit = new();

        if (_meleeCollider is BoxCollider2D box)
        {
            Vector2 defaultBoxSize = new(_meleeCollider.size.x, _meleeCollider.size.y);
            Vector2 defaultBoxOffset = Vector2.zero;
            
            float desiredLength = _currentSwingData.ColliderLength;
            float increasedLength = desiredLength - defaultBoxSize.y;
            box.offset = new Vector2(defaultBoxOffset.x, (increasedLength / 2f) * -1f);
            box.size = new Vector2(defaultBoxSize.x, _currentSwingData.ColliderLength);
        }

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
            int tileId = GameManager.Instance.GetTileIDFromTilemapTilePosition(TileManager.Instance.FoliageTm, (Vector3Int)tilePos);
            TileManager.Instance.DestroyTileServerRpc(tilePos, tileId, Player.LocalClientInstance.CurrentPlayerBiome.Value);
            collision.gameObject.GetComponent<FoliageCollider>().DestroyFoliage();
        }
    }

    private IEnumerator HitFoundTargets()
    {
        if(_targetsFound.Count > 0)
        {
            foreach (NetworkHealthState targetToDamage in _targetsFound.ToArray())
            {
                if(_targetsHit.Contains(targetToDamage)) continue;
                
                SoundManager.Instance.PlayOneShot(_currentSwingData.HitSound, Player.LocalClientInstance.transform.position);
                
                targetToDamage.TakeDamageRpc(_currentSwingData.Damage, Player.LocalClientInstance.transform.position, _currentSwingData.Knockback);
                _targetsFound.Remove(targetToDamage);
                _targetsHit.Add(targetToDamage);
                
                yield return new WaitForSeconds(_currentSwingData.DetectionBetweenHitsDuration);
            }
        }
        
        yield return null;
        StartCoroutine(HitFoundTargets());
    }
}
