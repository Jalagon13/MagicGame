using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CollisionDetector))]
public class SpellCollisionDetector : NetworkBehaviour
{
    [SerializeField]
    private ServerSpell _serverSpell;
    
    private CircleCollider2D _spellCollider;
    private CollisionDetector _collisionDetector;
    private int _remainingBounces; // Number of bounces allowed before the spell is destroyed
    private bool _bounceInitialized = false;

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
        if (_spellCollider == null || !IsOwner || _serverSpell.SpellStateNV.Value != SpellState.Casting) return;
        
        // NTFS: This MIGHT not be the most reliable way of detecting if it is colliding with anything that ISN'T a world collider, but idk keep testing
        if (IsServer && collision != null  && collision.gameObject.transform.parent != null && collision.gameObject.transform.parent.TryGetComponent(out ResourceObject worldObject))
        {
            return; // NTFS: If on server, ignore collisions with WorldObjects so the code below will only play for pathfinding wall collisions
        }

        if (!_bounceInitialized)
        {
            _remainingBounces = _serverSpell.SpellData.Value.BounceCount;
            _bounceInitialized = true;
        }

        // NTFS: Need to get rid of this explicit temporary FireBolt velocity stuff and use something more generic, AS WELL as test this shit on multiplayer
        if (transform.root.TryGetComponent(out ProjectileSpell projectileSpell))
        {
            ContactPoint2D contact = collision.GetContact(0);
            Vector2 hitNormal = contact.normal;
            float speed = projectileSpell.Velocity.magnitude;
            Vector2 reflected = Vector2.Reflect(projectileSpell.Velocity.normalized, hitNormal);
            projectileSpell.Velocity = reflected * speed;

            if (float.IsNaN(projectileSpell.Velocity.x) || float.IsNaN(projectileSpell.Velocity.y) ||
                float.IsInfinity(projectileSpell.Velocity.x) || float.IsInfinity(projectileSpell.Velocity.y))
            {
                Debug.LogError("Velocity became invalid after reflection!");
                projectileSpell.Velocity = Vector2.zero;
            }

            _remainingBounces--;
            if (_remainingBounces < 0)
            {
                // TODO: Destroy or deactivate the spell after final bounce
                _serverSpell.EndSpellExternally();
                return;
            }

            Debug.Log($"[OnCollisionEnter2D] Bounced! New velocity: {projectileSpell.Velocity} from {collision.gameObject.name}");
        }

        Debug.Log($"{gameObject.transform.root.gameObject.name} Collision detected with {collision.gameObject.name}");
    }
}
