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
    private List<NetworkHealthState> _damagedNetworkHealthStates = new List<NetworkHealthState>();


    private void Awake()
    {
        _spellCollider = GetComponent<CircleCollider2D>();
    }
    
    private void FixedUpdate()
    {
        if (_spellCollider == null || Spell.SpellData.Value.OwnerPlayerId != Player.LocalClientInstance.OwnerClientId || !Spell.IsStarted.Value) return;

        // First: Handle NPC hits using OverlapCircleAll
        Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, _spellCollider.radius, Spell.CollisionMask);
        for (int i = 0; i < collisions.Length; i++)
        {
            int layerTest = 1 << collisions[i].gameObject.layer;
            if ((layerTest & Spell.CollisionMask) != 0)
            {
                if (collisions[i].gameObject.layer == Spell.NpcLayer)
                {
                    if (collisions[i].TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(Spell.SpellData.Value.SpawnBiome))
                    {
                        NetworkHealthState npcHealth = npcNet.gameObject.GetComponent<NetworkHealthState>();

                        if (!_damagedNetworkHealthStates.Contains(npcHealth))
                        {
                            npcHealth.TakeDamageRpc(Spell.SpellData.Value.Damage, Spell.NetworkManager.ConnectedClients[Spell.SpellData.Value.OwnerPlayerId].PlayerObject.transform.position, Spell.SpellData.Value.Knockback);
                            _damagedNetworkHealthStates.Add(npcHealth);

                            if (_damagedNetworkHealthStates.Count >= PierceCount)
                            {
                                Debug.Log($"Ending spell on NPC hits");
                                Spell.OnOwnerSpellEnd();
                                return;
                            }

                            break;
                        }
                    }
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
