using UnityEngine;


public class SwingSpellVFX : MonoBehaviour
{
    [Tooltip("Used for allowing the rest of the VFX to play out like particles or something")]
    [SerializeField] private float _destroyDelay = 1f; 

    private Animator _animator;
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
    }

    public void ExecuteSwingSpellVFX(CardinalDirection direction) // Eventually make an interface for this if things get too complex and need different vfx executions
    {
        bool flipX = false;
    
        float zRotation = 0f;
        switch (direction)
        {
            case CardinalDirection.North:
                zRotation = 180f;
                break;
            case CardinalDirection.East:
                zRotation = 90f;
                break;
            case CardinalDirection.South:
                zRotation = 0f;
                break;
            case CardinalDirection.West:
                flipX = true;
                zRotation = -90f;
                break;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
        _spriteRenderer.flipX = flipX;

        AnimatorClipInfo[] clipInfo = _animator.GetCurrentAnimatorClipInfo(0);
        float animLength = clipInfo[0].clip.length;

        Destroy(gameObject, animLength + _destroyDelay);
    }
}
