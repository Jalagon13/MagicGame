using UnityEngine;

public class TestDummyAI : MonoBehaviour
{
    [SerializeField] private bool _enableKnockback;

    private Knockback _knockback;
    private Rigidbody2D _rb2d;

    private void Awake()
    {
        _knockback = GetComponent<Knockback>();
        _rb2d = GetComponent<Rigidbody2D>();
    }
    
    private void FixedUpdate()
    {
        if (!_enableKnockback) return;
    
        _rb2d.MovePosition(_rb2d.position + _knockback.Velocity * Time.fixedDeltaTime);
    }
}
