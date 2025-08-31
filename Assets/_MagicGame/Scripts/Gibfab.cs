using Sirenix.OdinInspector;
using UnityEngine;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine.Events;

public class Gibfab : MonoBehaviour
{
    [SerializeField] 
    [Tooltip("How quickly the gib slows down in the air.")]
    private float _airResistance = 2f, _minRotationMultiplier = 2.5f, _maxRotationMultiplier = 5f, 
    _minShrinkDelay = 1f, _maxShrinkDelay = 1.5f;
    
    [Space(15)]
    [SerializeField] 
    private UnityEvent _onGibBounce;

    private ZAxisSimulator _zAxisSimulator;
    private SpriteRenderer _gibSprite, _shadowSprite;
    private Vector2 _velocity;
    private Rigidbody2D _rb;
    private MMF_Player _bounceFeedback;
    private bool _gibStarted;
    private float _rotationDirection, _rotationSpeed, _rotationMultiplier = 1f;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _bounceFeedback = GetComponent<MMF_Player>();
        _gibSprite = transform.GetChild(0).GetComponent<SpriteRenderer>();
        _shadowSprite = transform.GetChild(1).GetComponent<SpriteRenderer>();
        _zAxisSimulator = GetComponent<ZAxisSimulator>();

        _zAxisSimulator.OnBounce += OnGibBounce;
        
        gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        _zAxisSimulator.OnBounce -= OnGibBounce;

        if (_gibSprite != null)
            DOTween.Kill(_gibSprite.transform);
        if (_gibSprite != null)
            DOTween.Kill(_gibSprite);
        if (_shadowSprite != null)
            DOTween.Kill(_shadowSprite.transform);
        if (_shadowSprite != null)
            DOTween.Kill(_shadowSprite);
    }

    private void FixedUpdate()
    {
        if(!_gibStarted) return;
    
        _velocity = Vector2.Lerp(_velocity, Vector2.zero, _airResistance * Time.fixedDeltaTime);

        if (_velocity.magnitude <= 0.1f && _gibStarted)
        {
            _rb.linearVelocity = Vector2.zero;
            _gibStarted = false;

            float delay = Random.Range(_minShrinkDelay, _maxShrinkDelay);
            float shrinkDuration = 0.25f;

            // Shrink and fade the gib sprite
            _gibSprite.transform.DOScale(0f, shrinkDuration).SetDelay(delay);
            _gibSprite.DOFade(0f, shrinkDuration).SetDelay(delay);

            // Shrink and fade the shadow sprite
            _shadowSprite.transform.DOScale(0f, shrinkDuration).SetDelay(delay).OnComplete(() => Destroy(gameObject));
            _shadowSprite.DOFade(0f, shrinkDuration).SetDelay(delay);
        }
        else
        {
            _rb.linearVelocity = _velocity;
        }

        // Calculate and apply rotation based on velocity magnitude and direction
        _rotationSpeed = _velocity.magnitude;
        float rotationAmount = _rotationSpeed * _rotationDirection * _rotationMultiplier;
        _gibSprite.transform.Rotate(0f, 0f, rotationAmount);
    }

    private void OnGibBounce(object sender, System.EventArgs e)
    {
        _bounceFeedback?.PlayFeedbacks();
    }

    public void LaunchGib(float initialSpeed, float startingHeight, Vector2 velocity)
    {
        gameObject.SetActive(true);

        _velocity = velocity;
        _zAxisSimulator.Launch(initialSpeed);
        _zAxisSimulator.SetZAxis(startingHeight);
        
        // Randomize rotation direction and reset rotation speed
        _rotationDirection = Random.value < 0.5f ? -1f : 1f;
        _rotationSpeed = 0f;
        _rotationMultiplier = Random.Range(_minRotationMultiplier, _maxRotationMultiplier);

        _gibStarted = true;
    }
}
