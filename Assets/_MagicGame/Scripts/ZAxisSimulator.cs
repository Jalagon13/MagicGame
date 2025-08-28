using UnityEngine;

public class ZAxisSimulator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Renderer used to draw the shadow beneath this object.")]
    [SerializeField] private SpriteRenderer _shadowRenderer;

    [Header("Runtime State (Debug Only)")]
    [Tooltip("Current simulated vertical position (fake Z height above ground).")]
    [SerializeField] private float _zAxis;
    [Tooltip("Current simulated vertical speed (up/down movement speed).")]
    [SerializeField] private float _zSpeed;

    [Header("Physics Settings")]
    [Tooltip("Downward force applied each frame to simulate gravity.")]
    [SerializeField] private float _gravity = 0.01f;
    [Tooltip("Multiplier applied to vertical speed after bouncing.")]
    [SerializeField] private float _bounceDamping = 0.7f;
    [Tooltip("Flat loss applied to vertical speed after bouncing.")]
    [SerializeField] private float _bounceLoss = 0.01f;
    [Tooltip("Minimum vertical speed required for bounce to continue.")]
    [SerializeField] private float _minBounceSpeed = 0.04f;

    [Header("Behavior Settings")]
    [Tooltip("If true, enables rendering and scaling of the shadow.")]
    [SerializeField] private bool _enableShadow = true;
    [Tooltip("If true, object will bounce when hitting the ground.")]
    [SerializeField] private bool _allowBounce = true;
    [Tooltip("If true, object will bounce forever instead of settling.")]
    [SerializeField] private bool _loopBounce = false;

    private Vector3 _defaultShadowScale;
    private Vector2 _defaultSpritePos;
    private Vector2 _defaultShadowPos;

    public float ZAxis => _zAxis;
    public float ZSpeed => _zSpeed;

    private void Awake()
    {
        _defaultSpritePos = transform.localPosition;
        if (_shadowRenderer != null)
        {
            _defaultShadowPos = _shadowRenderer.transform.localPosition;
            _defaultShadowScale = _shadowRenderer.transform.localScale;
        }
    }

    private void FixedUpdate()
    {
        HandleZAxis();

        transform.localPosition = new Vector3(_defaultSpritePos.x, _defaultSpritePos.y + _zAxis);
        if (_enableShadow && _shadowRenderer != null)
        {
            float scaleFactor = 1f / (1f + _zAxis);
            _shadowRenderer.transform.localScale = _defaultShadowScale * scaleFactor;
            _shadowRenderer.transform.localPosition = _defaultShadowPos;
        }
    }

    public void Launch(float initialSpeed)
    {
        _zSpeed = initialSpeed;
    }

    public void SetZAxis(float newZ)
    {
        _zAxis = Mathf.Max(newZ, 0f); // prevent going below ground
    }

    private void HandleZAxis()
    {
        if (_zAxis > 0)
            _zSpeed -= _gravity;

        _zAxis += _zSpeed;

        if (_zAxis < 0)
        {
            if (!_allowBounce)
            {
                _zAxis = 0;
                _zSpeed = 0;
                return;
            }

            _zAxis = -_zAxis;
            if (_zSpeed < 0)
                _zSpeed = -_zSpeed * _bounceDamping - _bounceLoss;

            if (!_loopBounce && _zSpeed < _minBounceSpeed)
            {
                _zAxis = 0;
                _zSpeed = 0;
            }
        }
    }
}
