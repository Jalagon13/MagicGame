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
    public List<DamageReceiver> DamagedNetworkHealthStates => _damagedNetworkHealthStates;
    
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

            if(TileManager.Instance.HasTile(tilePos, TileType.Foliage, out TileDataSO tileSO))
            {
                ushort tileId = GameDataRegistry.Instance.GetUShortIdFromTilemapTilePosition(TileManager.Instance.FoliageTm, tilePos);
                TileManager.Instance.DestroyTile((Vector2Int)tilePos, tileId, _serverSpell.SpellData.Value.SpawnBiome);
                collision.gameObject.GetComponent<FoliageCollider>()?.DestroyFoliage();
            }
        }

        // Handle NPC collision
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
                    SpellItemSO spellItemSO = GameManager.Instance.GetItemSOFromItemId(_serverSpell.SpellData.Value.SpellItemId) as SpellItemSO;
                    SoundManager.Instance.PlayOneShot(spellItemSO.SpellOnDamageSound, transform.position);

                    _serverSpell.EndSpellExternally();
                    return;
                }
            }
        }
    }
}
