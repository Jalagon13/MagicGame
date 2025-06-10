using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProjectileHitbox : MonoBehaviour
{
    [field: SerializeField] public Spell Spell { get; private set; }
    [field: SerializeField] public int BounceCount { get; private set; }
    [field: SerializeField] public int PierceCount { get; private set; }
    
    private CircleCollider2D _spellCollider;
    private int _bounces;
    private List<DamageReceiver> _damagedNetworkHealthStates = new();


    private void Awake()
    {
        _spellCollider = GetComponent<CircleCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_spellCollider == null || Spell.SpellData.Value.CasterNetworkObjectId != Player.LocalClientInstance.NetworkObjectId || !Spell.IsStarted.Value) return;

        if (collision.gameObject.layer == 16) // Detecting Foliage tiles
        {
            Vector2Int tilePos = new Vector2Int((int)collision.gameObject.transform.position.x, (int)collision.gameObject.transform.position.y);
            int tileId = GameManager.Instance.GetTileIDFromTilemapTilePosition(TileManager.Instance.FoliageTm, (Vector3Int)tilePos);
            TileManager.Instance.DestroyTileServerRpc(tilePos, tileId, Player.LocalClientInstance.CurrentBiome.Value);
            collision.gameObject.GetComponent<FoliageCollider>().DestroyFoliage();
        }
    }

    private void FixedUpdate()
    {
        if (_spellCollider == null || Spell.SpellData.Value.CasterNetworkObjectId != Player.LocalClientInstance.NetworkObjectId || !Spell.IsStarted.Value) return;

        
        // First: Handle NPC hits using OverlapCircleAll
        Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, _spellCollider.radius, Spell.CollisionMask);
        foreach (var col in collisions)
        {
            if (Spell.IsValidNpcHit(col, out DamageReceiver damageReceiver))
            {
                if (!_damagedNetworkHealthStates.Contains(damageReceiver))
                {
                    if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(Spell.SpellData.Value.CasterNetworkObjectId, out NetworkObject inflicterNetworkObj))
                    {
                        if(inflicterNetworkObj.TryGetComponent(out ServerCharacter inflicter))
                        {
                            damageReceiver.ReceiveHP(inflicter, -Spell.SpellData.Value.Damage, true, Spell.SpellData.Value.Knockback);
                        }
                    }
                    
                    _damagedNetworkHealthStates.Add(damageReceiver);

                    if (_damagedNetworkHealthStates.Count >= PierceCount)
                    {
                        Spell.OnOwnerSpellEnd();
                        return;
                    }

                    break;
                }
            }
        }

        // Second: Handle wall bounces using a predictive raycast
        Vector2 currentPosition = transform.root.position;
        Vector2 direction = Spell.Velocity.Value.normalized;
        float distance = Spell.Velocity.Value.magnitude * Time.fixedDeltaTime + 0.02f;

        RaycastHit2D hit = Physics2D.Raycast(currentPosition, direction, distance, Spell.CollisionMask);
        if (hit.collider != null && hit.collider.gameObject.layer == 9)
        {
            if (_bounces >= BounceCount)
            {
                Debug.Log($"Ending spell on bounce");
                Spell.OnOwnerSpellEnd();
                return;
            }
            else
            {
                Vector2 hitNormal = hit.normal;
                float speed = Spell.Velocity.Value.magnitude;
                Spell.Velocity.Value = Vector2.Reflect(direction, hitNormal) * speed;
                _bounces++;
                Debug.Log($"Bounced! Bounce count: {_bounces}");
                return;
            }
        }
    }
}
