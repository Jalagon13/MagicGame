using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileHitbox : NetworkBehaviour
{
    [SerializeField] 
    private ServerSpell _serverSpell;
    
    private CircleCollider2D _spellCollider;
    private List<DamageReceiver> _damagedNetworkHealthStates = new();
    private int _remainingPierces; 
    private bool _pierceInitialized = false;

    private void Awake()
    {
        _spellCollider = GetComponent<CircleCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_spellCollider == null || !IsOwner || _serverSpell.SpellStateNV.Value != SpellState.Casting) return;
        
        if (!_pierceInitialized)
        {
            _remainingPierces = _serverSpell.SpellData.Value.PierceCount + 1;
            _pierceInitialized = true;
        }

        if (collision.gameObject.layer == _serverSpell.FoliageLayer) // Detecting Foliage tiles
        {
            Vector3Int tilePos = new Vector3Int((int)collision.gameObject.transform.position.x, (int)collision.gameObject.transform.position.y, 0);

            if(TileManager.Instance.HasTile(tilePos, TileType.Foliage, out TileSO tileSO))
            {
                int tileId = GameManager.Instance.GetTileIDFromTilemapTilePosition(TileManager.Instance.FoliageTm, (Vector3Int)tilePos);
                TileManager.Instance.DestroyTileServerRpc((Vector2Int)tilePos, tileId, _serverSpell.SpellData.Value.SpawnBiome);
                collision.gameObject.GetComponent<FoliageCollider>()?.DestroyFoliage();
            }
        }

        // Handle NPC collision
        if (collision.gameObject.layer == _serverSpell.NpcLayer)
        {
            if (_serverSpell.IsValidNpcHit(collision, out DamageReceiver damageReceiver))
            {
                if (!_damagedNetworkHealthStates.Contains(damageReceiver))
                {
                    if (_serverSpell.SpellCasterNetworkObject.TryGetComponent(out ServerCharacter inflicter))
                    {
                        damageReceiver.ReceiveHP(inflicter, -_serverSpell.SpellData.Value.Damage, true, _serverSpell.SpellData.Value.Knockback);
                    }

                    _damagedNetworkHealthStates.Add(damageReceiver);
                    _remainingPierces--;

                    if (_remainingPierces <= 0)
                    {
                        // TODO: Destroy or deactivate the spell
                        _serverSpell.EndSpellExternally();
                        return;
                    }
                }
            }
        }
    }
}
