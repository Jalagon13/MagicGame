using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class SwingController : NetworkBehaviour
{
    public static SwingController Instance;

    public event EventHandler<SwingEventArgs> OnSwingEnd;
    public event EventHandler<SwingEventArgs> OnSwingStart;
    public class SwingEventArgs : EventArgs
    {
        public CardinalDirection SwingDirection;
    }

    [SerializeField] private MeleeCollider _meleeCollider;
    [SerializeField] private SpriteRenderer _meleeObjectSprite;
    [SerializeField] private AudioClip _wooshSound;
	
    [Header("Item Parameters")]
    [SerializeField] private ItemParameter _swingSpeedParameter;
    [SerializeField] private ItemParameter _damageParameter;
	
    [FoldoutGroup("Animation Clips")]
    [SerializeField] private AnimationClip _swingEastClip;
	
    [FoldoutGroup("Animation Clips")]
    [SerializeField] private AnimationClip _swingNorthClip;
	
    [FoldoutGroup("Animation Clips")]
    [SerializeField] private AnimationClip _swingWestClip;
	
    [FoldoutGroup("Animation Clips")]
    [SerializeField] private AnimationClip _swingSouthClip;
	
    [FoldoutGroup("Animation Clips")]
    [SerializeField] private AnimationClip _swingIdleClip;
	
    private Animator _animator;
    private Timer _swingTimer;
    private MeleeItemSO _meleeItemSO;
    private ItemSO _focusItemSO;
    private bool _swingPerfoming;
    private float _defaultSwingSpeed = 1, _currentSwingDuration;
    private Player _thisPlayer;
	
    public CardinalDirection SwingDirection { get; private set;}
	
    private void Awake()
    {
        Instance = this;
		
        _animator = GetComponent<Animator>();
        _swingTimer = new Timer(0.1f);
        _animator.speed = _defaultSwingSpeed;
    }

    private void Start()
    {
        if(_thisPlayer == null)
        {
            _thisPlayer = transform.root.GetComponent<Player>();
            _thisPlayer.GetFocusItemIndexNetworkVariable().OnValueChanged += FocusItemIndexNetworkVariable_OnValueChanged;
        }
    }
	
    private void Update()
    {
        if(!IsOwner || Player.LocalClientInstance.IsDead()) return;
	
        _swingTimer.Tick(Time.deltaTime);
	
        if(_swingTimer.RemainingSeconds <= 0 && _meleeItemSO != null && !Pointer.IsOverUI() && GameInput.Instance.GetPrimaryHeldDown() && !_swingPerfoming && _animator.GetCurrentAnimatorStateInfo(0).IsName(_swingIdleClip.name))
        {
            _swingPerfoming = true;
		
            float angle = CalculateAngle();

            // Changes pivot point position based on rotation.
            if ((angle < 45 && angle > 0) || (angle < 359.999 && angle > 315))
            {
                // East
                AnimStateManager.ChangeAnimationState(_animator, _swingEastClip);
            }
            else if (angle < 135 && angle > 45)
            {
                // North
                AnimStateManager.ChangeAnimationState(_animator, _swingNorthClip);
            }
            else if (angle < 225 && angle > 135)
            {
                // West
                AnimStateManager.ChangeAnimationState(_animator, _swingWestClip);
            }
            else if (angle < 315 && angle > 225)
            {
                // South
                AnimStateManager.ChangeAnimationState(_animator, _swingSouthClip);
            }
        }
    }
	
    private void FocusItemIndexNetworkVariable_OnValueChanged(int previousValue, int newValue)
    {
        if(newValue <= -1) return;
	
        _focusItemSO = GameManager.Instance.GetItemSOFromIndex(newValue);
		
        // If melee item found, set it to this item, if not to null
        _meleeItemSO = _focusItemSO != null && _focusItemSO is MeleeItemSO ? _focusItemSO as MeleeItemSO : null;
		
        if(!_swingPerfoming)
        {
            // Update melee data if melee item found, else reset it to default
            UpdateMeleeData();
        }
    }
	
    public void OnSwingStartAnimationEvent() // Connected to first frame of animation
    {
        _swingPerfoming = true;
	
        if(_animator.GetCurrentAnimatorStateInfo(0).IsName(_swingNorthClip.name))
        {
            SwingDirection = CardinalDirection.North;
        }
        else if(_animator.GetCurrentAnimatorStateInfo(0).IsName(_swingSouthClip.name))
        {
            SwingDirection = CardinalDirection.South;
        }
        else if(_animator.GetCurrentAnimatorStateInfo(0).IsName(_swingEastClip.name))
        {
            SwingDirection = CardinalDirection.East;
        }
        else if(_animator.GetCurrentAnimatorStateInfo(0).IsName(_swingWestClip.name))
        {
            SwingDirection = CardinalDirection.West;
        }
		
        MMSoundManagerSoundPlayEvent.Trigger(_wooshSound, MMSoundManager.MMSoundManagerTracks.Sfx, default, volume:0.5f, pitch: UnityEngine.Random.Range(0.9f, 1.1f));	
		
        if(IsOwner)
        {
            _thisPlayer.SetIsSwingOnGoingOn(true);
        }
		
        OnSwingStart?.Invoke(this, new SwingEventArgs
        {
            SwingDirection = SwingDirection
        });
    }
	
    public void OnSwingEndAnimationEvent() // Connected to last frame of animation
    {
        _swingPerfoming = false;
        _swingTimer.RemainingSeconds = 0.1f;
		
        if(_animator.GetCurrentAnimatorStateInfo(0).IsName(_swingNorthClip.name))
        {
            SwingDirection = CardinalDirection.North;
        }
        else if(_animator.GetCurrentAnimatorStateInfo(0).IsName(_swingSouthClip.name))
        {
            SwingDirection = CardinalDirection.South;
        }
        else if(_animator.GetCurrentAnimatorStateInfo(0).IsName(_swingEastClip.name))
        {
            SwingDirection = CardinalDirection.East;
        }
        else if(_animator.GetCurrentAnimatorStateInfo(0).IsName(_swingWestClip.name))
        {
            SwingDirection = CardinalDirection.West;
        }
		
        if(IsOwner)
        {
            _thisPlayer.SetIsSwingOnGoingOn(false);
            AnimStateManager.ChangeAnimationState(_animator, _swingIdleClip);
        }
		
        OnSwingEnd?.Invoke(this, new SwingEventArgs
        {
            SwingDirection = SwingDirection	
        });
    }
	
    private float CalculateAngle()
    {
        Vector2 direction = ActionManager.MouseWorldPosition - (Vector2)transform.position; // Calculate direction to target.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; // Calculate angle in degrees.
		
        // Clamps pivot to be positive.
        if (angle < 0)
        {
            angle = Mathf.Abs(angle);
            float leftover = 180 - angle;
            angle = 180 + leftover;
        }
		
        return angle;
    }
	
    private void UpdateMeleeData()
    {
        _currentSwingDuration = _meleeItemSO != null ? _meleeItemSO.ExtractParameterValue(_swingSpeedParameter) : _defaultSwingSpeed;
        _meleeCollider.Damage = _meleeItemSO!= null ? (int)_meleeItemSO.ExtractParameterValue(_damageParameter) : 0;
        if(_meleeObjectSprite != null)
        {
            _meleeObjectSprite.sprite = _meleeItemSO!= null ? _meleeItemSO.UiDisplay : null;
        }
		
        if(_animator == null) _animator = GetComponent<Animator>();
        _animator.speed = _meleeItemSO != null ? _currentSwingDuration : _defaultSwingSpeed;
    }
	
    public override void OnDestroy()
    {
        _thisPlayer.GetFocusItemIndexNetworkVariable().OnValueChanged -= FocusItemIndexNetworkVariable_OnValueChanged;
	
        base.OnDestroy();
    }
}
