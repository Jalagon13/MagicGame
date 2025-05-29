using Unity.Netcode;
using UnityEngine;

public class ServerCharacterMovement : NetworkBehaviour
{
    [SerializeField] 
    private ServerCharacter _serverCharacter;

    [SerializeField] 
    private Rigidbody2D _rigidbody2D;
    public Rigidbody2D RigidBody2D => _rigidbody2D;
    
    private Knockback _knockback;
    
    private void Awake()
    {
        _knockback = new(_serverCharacter);
    }
    
    private void FixedUpdate()
    {
        // Movement is handled here
        _knockback.UpdateKnockback(Time.fixedDeltaTime);
        
        // Knockback handling here
    }
}
