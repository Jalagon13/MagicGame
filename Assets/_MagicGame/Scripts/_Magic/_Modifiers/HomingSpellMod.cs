using System.Collections.Generic;
using UnityEngine;

public class HomingSpellMod : SpellModifier
{
    private const int MAX_TARGETS = 10;
    
    [SerializeField] 
    private float _detectionRadius = 2.5f;
    
    [Tooltip("The higher the value, the faster the spell will home to the target")]
    [SerializeField] 
    private float _rotateSharpness = 5f;

    private ProjectileSpell _projSpell;
    private List<GameObject> _potentialTargetsToHomeTo = new(MAX_TARGETS);

    public override SyncSpellData ModifiySpellData(SyncSpellData spellData, ServerSpell serverSpell)
    {
        if(serverSpell != null)
        {
            _projSpell = serverSpell as ProjectileSpell;
        }
    
        return spellData;
    }

    private void FixedUpdate()
    {
        Debug.Log($"HomingSpellMod FixedUpdate");
        if (_projSpell == null || !IsOwner || _projSpell.SpellStateNV.Value != SpellState.Casting) return;
        _potentialTargetsToHomeTo.Clear();

        Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, _detectionRadius, _projSpell.CollisionMask);
        
        for (int i = 0; i < collisions.Length; i++)
        {
            if (_projSpell.IsValidNpcHit(collisions[i], out DamageReceiver damageReceiver))
            {
                if(!_projSpell.ProjectileHitbox.DamagedNetworkHealthStates.Contains(damageReceiver))
                {
                    _potentialTargetsToHomeTo.Add(collisions[i].gameObject);
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
            float currentSpeed = _projSpell.Velocity.magnitude;
            Vector2 desiredDirection = (closestTarget.transform.position - transform.position).normalized;

            // Lerp smoothly towards the desired direction without renormalizing
            _projSpell.Velocity = Vector2.Lerp(
                _projSpell.Velocity.normalized,
                desiredDirection,
                Mathf.Clamp01(_rotateSharpness * Time.fixedDeltaTime)
            ) * currentSpeed;
        }
    }
}
