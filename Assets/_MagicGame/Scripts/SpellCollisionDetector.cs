using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CollisionDetector))]
public class SpellCollisionDetector : NetworkBehaviour
{
    [SerializeField]
    private ServerSpell _serverSpell;
    
    [SerializeField] 
    private int _bounceAmount; // Number of bounces allowed before the spell is destroyed

    private CircleCollider2D _spellCollider;
    private CollisionDetector _collisionDetector;

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsServer && collision.gameObject.transform.parent.TryGetComponent(out WorldObject worldObject))
        {
            return; // If on server, ignore collisions with WorldObjects so the code below will only play for pathfinding wall collisions
        }

        if (_spellCollider == null || !IsOwner || _serverSpell.SpellStateNV.Value != SpellState.Casting) return;

        if (transform.root.TryGetComponent(out FireBolt fireBolt))
        {
            ContactPoint2D contact = collision.GetContact(0);
            Vector2 hitNormal = contact.normal;
            float speed = fireBolt._velocity.magnitude;
            Vector2 reflected = Vector2.Reflect(fireBolt._velocity.normalized, hitNormal);
            fireBolt._velocity = reflected * speed;

            if (float.IsNaN(fireBolt._velocity.x) || float.IsNaN(fireBolt._velocity.y) ||
                float.IsInfinity(fireBolt._velocity.x) || float.IsInfinity(fireBolt._velocity.y))
            {
                Debug.LogError("Velocity became invalid after reflection!");
                fireBolt._velocity = Vector2.zero;
            }

            _bounceAmount--;
            if (_bounceAmount < 0)
            {
                // TODO: Destroy or deactivate the spell after final bounce
                _serverSpell.EndSpellExternally();
                return;
            }

            Debug.Log($"[OnCollisionEnter2D] Bounced! New velocity: {fireBolt._velocity} from {collision.gameObject.name}");
        }

        Debug.Log($"{gameObject.transform.root.gameObject.name} Collision detected with {collision.gameObject.name}");
    }
}
