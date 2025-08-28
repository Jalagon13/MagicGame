using Sirenix.OdinInspector;
using UnityEngine;

public class Gibfab : MonoBehaviour
{
    [SerializeField] 
    private ZAxisSimulator _zAxisSimulator;
    
    [SerializeField] 
    private float _airResistance = 2f;

    private Collider2D _gibCollider;
    private Vector2 _velocity;
    private Rigidbody2D _rb;
    private bool _gibStarted;
    
    private void Awake()
    {
        _gibCollider = GetComponent<Collider2D>();
        _rb = GetComponent<Rigidbody2D>();
        
        gameObject.SetActive(false);
    }
    
    private void FixedUpdate()
    {
        if(!_gibStarted) return;
    
        _velocity = Vector2.Lerp(_velocity, Vector2.zero, _airResistance * Time.fixedDeltaTime);

        _rb.linearVelocity = _velocity;
    }
    
    // NTFS: Need to come up with a speed system of measurement that actually makes sense.
    [Button("Test Velocity to the right")]
    public void StartGib(float speed, Vector2 velocity)
    {
        _velocity = velocity;
        _zAxisSimulator.Launch(speed);
    
        _gibStarted = true;
    }
}
