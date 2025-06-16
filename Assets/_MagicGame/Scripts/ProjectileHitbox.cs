using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileHitbox : NetworkBehaviour
{
    [SerializeField] 
    private ServerSpell _serverSpell;
    
    private CircleCollider2D _spellCollider;
    private CollisionDetector _collisionDetector;
    private List<DamageReceiver> _damagedNetworkHealthStates = new();

    private void Awake()
    {
        _spellCollider = GetComponent<CircleCollider2D>();
        _collisionDetector = GetComponent<CollisionDetector>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _serverSpell.SpellData.OnValueChanged += UpdateCollisionDetector;
        }
    }
    
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            _serverSpell.SpellData.OnValueChanged -= UpdateCollisionDetector;
        }
    }

    private void UpdateCollisionDetector(SyncSpellData previousValue, SyncSpellData newValue)
    {
        if (_collisionDetector != null)
        {
            // Debug.Log($"Updating collision detector biome from {previousValue.SpawnBiome} to {newValue.SpawnBiome}");
            _collisionDetector.SetBiome(newValue.SpawnBiome);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_spellCollider == null || !IsOwner || _serverSpell.SpellStateNV.Value != SpellState.Casting) return;

        if (collision.gameObject.layer == _serverSpell.FoliageLayer) // Detecting Foliage tiles
        {
            Vector3Int tilePos = new Vector3Int((int)collision.gameObject.transform.position.x, (int)collision.gameObject.transform.position.y, 0);

            if(TileManager.Instance.HasTile(tilePos, TileType.Foliage, out TileSO tileSO))
            {
                int tileId = GameManager.Instance.GetTileIDFromTilemapTilePosition(TileManager.Instance.FoliageTm, (Vector3Int)tilePos);
                TileManager.Instance.DestroyTileServerRpc((Vector2Int)tilePos, tileId, _serverSpell.SpellData.Value.SpawnBiome);
                collision.gameObject.GetComponent<FoliageCollider>().DestroyFoliage();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(IsServer && collision.gameObject.transform.parent.TryGetComponent(out WorldObject worldObject))
        {
            return; // If on server, ignore collisions with WorldObjects so the code below will only play for pathfinding wall collisions
        }
    
        Debug.Log($"{gameObject.transform.root.gameObject.name} Collision detected with {collision.gameObject.name}");
    }

    private void FixedUpdate()
    {
        if (_spellCollider == null || !IsOwner || _serverSpell.SpellStateNV.Value != SpellState.Casting) return;
        
        // First: Handle NPC hits using OverlapCircleAll
        Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, _spellCollider.radius, _serverSpell.NpcLayer);
        foreach (var col in collisions)
        {
            if (_serverSpell.IsValidNpcHit(col, out DamageReceiver damageReceiver))
            {
                if (!_damagedNetworkHealthStates.Contains(damageReceiver))
                {
                    if (_serverSpell.SpellCasterNetworkObject.TryGetComponent(out ServerCharacter inflicter))
                    {
                        damageReceiver.ReceiveHP(inflicter, -_serverSpell.SpellData.Value.Damage, true, _serverSpell.SpellData.Value.Knockback);
                    }
                    
                    _damagedNetworkHealthStates.Add(damageReceiver);

                    break;
                }
            }
        }

        // Second: Handle wall bounces using a predictive raycast
        // Vector2 currentPosition = transform.root.position;
        // Vector2 direction = Spell.Velocity.Value.normalized;
        // float distance = Spell.Velocity.Value.magnitude * Time.fixedDeltaTime + 0.02f;

        // RaycastHit2D hit = Physics2D.Raycast(currentPosition, direction, distance, Spell.CollisionMask);
        // if (hit.collider != null && hit.collider.gameObject.layer == 9)
        // {
        //     if (_bounces >= BounceCount)
        //     {
        //         Debug.Log($"Ending spell on bounce");
        //         Spell.OnOwnerSpellEnd();
        //         return;
        //     }
        //     else
        //     {
        //         Vector2 hitNormal = hit.normal;
        //         float speed = Spell.Velocity.Value.magnitude;
        //         Spell.Velocity.Value = Vector2.Reflect(direction, hitNormal) * speed;
        //         _bounces++;
        //         Debug.Log($"Bounced! Bounce count: {_bounces}");
        //         return;
        //     }
        // }
    }
}
