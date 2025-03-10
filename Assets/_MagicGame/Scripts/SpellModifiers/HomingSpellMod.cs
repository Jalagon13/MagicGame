using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HomingSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private float _detectionRadius = 2.5f;
    [SerializeField] private float _rotateSharpness = 5f; // Higher values = faster rotation, lower values = slower rotation

    private const int MAX_TARGETS = 10;
    private Spell _spell;
    private List<GameObject> _potentialTargetsToHomeTo = new(MAX_TARGETS);

    public void ApplyModifier(Spell spell)
    {
        _spell = spell;
    }
    
    private void FixedUpdate()
    {
        if(_spell == null || !_spell.Started || !_spell.IsServer) return;

        _potentialTargetsToHomeTo.Clear();

        Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, _detectionRadius, _spell.CollisionMask);
        for (int i = 0; i < collisions.Length; i++)
        {
            int layerTest = 1 << collisions[i].gameObject.layer;
            if((layerTest & _spell.CollisionMask) != 0)
            {
                if (collisions[i].gameObject.layer == _spell.NpcLayer)
                {
                    if (collisions[i].TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(_spell.SpellDataNV.Value.SpawnBiome))
                    {
                        if (!_spell.HitTargets.Contains(collisions[i].gameObject))
                        {
                            // Found an npc in range, in same biome, and not already hit
                            _potentialTargetsToHomeTo.Add(collisions[i].gameObject);
                        }
                    }
                }
            }
        }
        
        if (_potentialTargetsToHomeTo.Count > 0)
        {
            // Pick the closest target to home to
            float closestDistance = float.MaxValue;
            GameObject closestTarget = null;
            foreach (GameObject target in _potentialTargetsToHomeTo)
            {
                float distance = Vector2.Distance(transform.position, target.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }
            
            // Slightly rotate towards the target
            float currentSpeed = _spell.Velocity.magnitude;
            _spell.Velocity = Vector2.Lerp(_spell.Velocity, (closestTarget.transform.position - transform.position).normalized, Mathf.Clamp01(_rotateSharpness * Time.fixedDeltaTime));
            _spell.Velocity.Normalize();
            _spell.Velocity *= currentSpeed;
            Debug.Log($"Rotating towards: {closestTarget.name} at position: {closestTarget.transform.position}");
        }
    }
}