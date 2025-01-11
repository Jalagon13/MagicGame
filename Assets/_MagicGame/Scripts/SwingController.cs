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
	
	private Animator _mainHandAnimator;
	private ItemSO _swingItemSO;
	private ItemSO _focusItemSO;
	private bool _performingMainHandSwing, _performingOffHandSwing;
	private float _defaultSwingSpeed = 1, _currentSwingDuration;
	private Player _thisPlayer;
	
	public CardinalDirection SwingDirection { get; private set;}
	
	private void Awake()
	{
		Instance = this;
		
		_mainHandAnimator = GetComponent<Animator>();
		_mainHandAnimator.speed = _defaultSwingSpeed;
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
	
		if(CanSwingMainHand())
		{
			_performingMainHandSwing = true;
		
			float angle = CalculateAngle();

			// Changes pivot point position based on rotation.
			if ((angle < 45 && angle > 0) || (angle < 359.999 && angle > 315))
			{
				// East
				AnimStateManager.ChangeAnimationState(_mainHandAnimator, _swingEastClip);
			}
			else if (angle < 135 && angle > 45)
			{
				// North
				AnimStateManager.ChangeAnimationState(_mainHandAnimator, _swingNorthClip);
			}
			else if (angle < 225 && angle > 135)
			{
				// West
				AnimStateManager.ChangeAnimationState(_mainHandAnimator, _swingWestClip);
			}
			else if (angle < 315 && angle > 225)
			{
				// South
				AnimStateManager.ChangeAnimationState(_mainHandAnimator, _swingSouthClip);
			}
		}
		
		if(CanSwingOffHand())
		{
			_performingOffHandSwing = true;
		
			float angle = CalculateAngle();

			// Changes pivot point position based on rotation.
			if ((angle < 45 && angle > 0) || (angle < 359.999 && angle > 315))
			{
				// East
				AnimStateManager.ChangeAnimationState(_mainHandAnimator, _swingEastClip);
			}
			else if (angle < 135 && angle > 45)
			{
				// North
				AnimStateManager.ChangeAnimationState(_mainHandAnimator, _swingNorthClip);
			}
			else if (angle < 225 && angle > 135)
			{
				// West
				AnimStateManager.ChangeAnimationState(_mainHandAnimator, _swingWestClip);
			}
			else if (angle < 315 && angle > 225)
			{
				// South
				AnimStateManager.ChangeAnimationState(_mainHandAnimator, _swingSouthClip);
			}
		}
	}
	
	private bool CanSwingMainHand()
	{
		return InventoryManager.Instance.MainHandItemExists(out InventoryItem mainHandInventoryItem) &&
		mainHandInventoryItem.Item is MeleeItemSO &&
		GameInput.Instance.GetPrimaryHeldDown() && 
		!Pointer.IsOverUI() && 
		!_performingMainHandSwing && 
		_mainHandAnimator.GetCurrentAnimatorStateInfo(0).IsName(_swingIdleClip.name);
	}
	
	private bool CanSwingOffHand()
	{
		return InventoryManager.Instance.OffHandItemExists(out InventoryItem offHandInventoryItem) &&
		offHandInventoryItem.Item is MeleeItemSO &&
		GameInput.Instance.GetSecondaryHeldDown() && 
		!Pointer.IsOverUI() && 
		!_performingOffHandSwing && 
		_mainHandAnimator.GetCurrentAnimatorStateInfo(0).IsName(_swingIdleClip.name);
	}
	
	private void FocusItemIndexNetworkVariable_OnValueChanged(int previousValue, int newValue)
	{
		_focusItemSO = GameManager.Instance.GetItemSOFromIndex(newValue);
		
		// If melee item found, set it to this item, if not to null
		if(_focusItemSO != null)
		{
			if(_focusItemSO is MeleeItemSO)
			{
				_swingItemSO = _focusItemSO as MeleeItemSO;
			}
			else
			{
				_swingItemSO = null;
			}
		}
		else
		{
			_swingItemSO = null;
		}
		
		if(!_performingMainHandSwing)
		{
			// Update melee data if melee item found, else reset it to default
			UpdateMeleeData();
		}
	}
	
	public void OnSwingStartAnimationEvent() // Connected to first frame of animation
	{
		if(_mainHandAnimator.GetCurrentAnimatorStateInfo(0).IsName(_swingNorthClip.name))
		{
			SwingDirection = CardinalDirection.North;
		}
		else if(_mainHandAnimator.GetCurrentAnimatorStateInfo(0).IsName(_swingSouthClip.name))
		{
			SwingDirection = CardinalDirection.South;
		}
		else if(_mainHandAnimator.GetCurrentAnimatorStateInfo(0).IsName(_swingEastClip.name))
		{
			SwingDirection = CardinalDirection.East;
		}
		else if(_mainHandAnimator.GetCurrentAnimatorStateInfo(0).IsName(_swingWestClip.name))
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
		_performingMainHandSwing = false;
		// _swingTimer.RemainingSeconds = 0.1f;
		
		if(_mainHandAnimator.GetCurrentAnimatorStateInfo(0).IsName(_swingNorthClip.name))
		{
			SwingDirection = CardinalDirection.North;
		}
		else if(_mainHandAnimator.GetCurrentAnimatorStateInfo(0).IsName(_swingSouthClip.name))
		{
			SwingDirection = CardinalDirection.South;
		}
		else if(_mainHandAnimator.GetCurrentAnimatorStateInfo(0).IsName(_swingEastClip.name))
		{
			SwingDirection = CardinalDirection.East;
		}
		else if(_mainHandAnimator.GetCurrentAnimatorStateInfo(0).IsName(_swingWestClip.name))
		{
			SwingDirection = CardinalDirection.West;
		}
		
		if(IsOwner)
		{
			_thisPlayer.SetIsSwingOnGoingOn(false);
			AnimStateManager.ChangeAnimationState(_mainHandAnimator, _swingIdleClip);
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
		_currentSwingDuration = _swingItemSO != null ? _swingItemSO.ExtractParameterValue(GameManager.Instance.GetItemParameterDataBaseSO().SwingSpeedParameter) : _defaultSwingSpeed;
		_meleeCollider.Damage = _swingItemSO != null ? 5 : 0; // NTFS FIX THIS 
		
		if(_meleeObjectSprite != null)
		{
			_meleeObjectSprite.sprite = _swingItemSO!= null ? _swingItemSO.UiDisplay : null;
		}
		
		if(_mainHandAnimator == null) _mainHandAnimator = GetComponent<Animator>();
		_mainHandAnimator.speed = _swingItemSO != null ? _currentSwingDuration : _defaultSwingSpeed;
	}
	
	public override void OnDestroy()
	{
		_thisPlayer.GetFocusItemIndexNetworkVariable().OnValueChanged -= FocusItemIndexNetworkVariable_OnValueChanged;
	
		base.OnDestroy();
	}
}
