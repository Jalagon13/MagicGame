using System.Collections;
using UnityEngine;

public class FireBolt : ProjectileSpell
{
    [SerializeField] 
    private float _velocityDecay = 5f;

    protected override Vector2 CalculateVelocity(Vector2 currentVelocity)
    {
        return Vector2.Lerp(Velocity, Vector2.zero, _velocityDecay * Time.fixedDeltaTime);
    }
}
