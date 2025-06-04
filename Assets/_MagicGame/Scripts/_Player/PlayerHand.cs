using System;
using System.Collections;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;

public class PlayerHand : NetworkBehaviour
{
	// Events
	public event EventHandler<CardinalDirectionEventArgs> OnHoldingWandEnd;
	public event EventHandler<CardinalDirectionEventArgs> OnHoldingWandStart;
	public event EventHandler<CardinalDirectionEventArgs> OnSwingStart;
	public event EventHandler<CardinalDirectionEventArgs> OnSwingEnd;
	public event EventHandler<CardinalDirectionEventArgs> OnCastingArmDirectionChanged;

	public class CardinalDirectionEventArgs : EventArgs
	{
		public CardinalDirection Direction;
	}

	[FoldoutGroup("Pivots"), SerializeField] 
	private Transform _northPivot;
	[FoldoutGroup("Pivots"), SerializeField] 
	private Transform _southPivot;
	[FoldoutGroup("Pivots"), SerializeField] 
	private Transform _eastPivot;
	[FoldoutGroup("Pivots"), SerializeField] 
	private Transform _westPivot;
	
	[SerializeField] 
	private GameObject _armPivotGO;
	[SerializeField] 
	private GameObject _armGO;
	
	[SerializeField] 
	private SpriteRenderer _itemHeldSR;
	[field: SerializeField] 
	public Transform SpellSpawnTransform;
	[field: SerializeField] 
	public MeleeCollider MeleeCollider;
	public bool IsSwinging { get; private set; }
	// public ItemSO HeldItem { get; private set; }

	private Player _thisPlayer;
	private ItemSO _heldItem;
	// private bool _stoppingSwing;
	// private Timer _swingCooldownTimer;

	// private NetworkVariable<float> _angleNetworkVariable = new(
	// 	default,
	// 	NetworkVariableReadPermission.Everyone,
	// 	NetworkVariableWritePermission.Owner
	// );

	public CardinalDirection ArmCardinalDirection { get; private set; }
	public NetworkVariable<float> AngleToMouse { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<CardinalDirection> AimDirection { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<CardinalDirection> SwingDirection { get; private set; } = new(CardinalDirection.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	private void Awake()
	{
		HideArm();
		
		_thisPlayer = transform.root.GetComponent<Player>();
		if(_thisPlayer != null)
		{
		    _thisPlayer.SelectedItemIdNetworkVariable.OnValueChanged += OnItemIdChanged;
		}
	}

	public override void OnDestroy()
	{
		_thisPlayer.SelectedItemIdNetworkVariable.OnValueChanged -= OnItemIdChanged;
	}

	private void Update()
	{
		if (IsOwner)
		{
			Vector3 direction = ActionManager.MouseWorldPosition - (Vector2)transform.position;
			AngleToMouse.Value = NormalizeAngle(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
			AimDirection.Value = DetermineCardinalDirection(AngleToMouse.Value);
		}
	}

	private void OnItemIdChanged(int previousValue, int newValue)
    {
		_heldItem = GameManager.Instance.GetItemSOFromItemId(newValue);

		_itemHeldSR.flipX = _heldItem is WandItemSO || _heldItem is ToolItemSO;
		_itemHeldSR.sprite = _heldItem switch
		{
			WandItemSO wand => wand.UiDisplay,
			ToolItemSO tool => tool.UiDisplay,
			_ => null
		};
	}

	public void SetPivotPosition(CardinalDirection direction)
	{
		switch (direction)
		{
			case CardinalDirection.North:
				_armPivotGO.transform.position = _northPivot.transform.position;
				break;
			case CardinalDirection.South:
				_armPivotGO.transform.position = _southPivot.transform.position;
				break;
			case CardinalDirection.West:
				_armPivotGO.transform.position = _westPivot.transform.position;
				break;
			case CardinalDirection.East:
				_armPivotGO.transform.position = _eastPivot.transform.position;
				break;
		}
	}

	public void PerformSwing(Quaternion startRotation, Quaternion endRotation, float duration, CardinalDirection direction, MeleeCollider.SwingData swingData)
	{
		ShowArm();

		MeleeCollider.StartSwing(swingData);

		IsSwinging = true;
		SwingDirection.Value = direction;
		float buildUpDuration = duration / 2f;
		_armPivotGO.transform.rotation = startRotation;
		_itemHeldSR.transform.localScale = Vector3.zero;

		_itemHeldSR.transform.DOScale(Vector3.one, buildUpDuration)
			.SetEase(Ease.OutSine)
			.OnComplete(() =>
			{
				SoundManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerMeleeSwing, transform.root.transform.position);

				_armPivotGO.transform
					.DORotateQuaternion(endRotation, duration)
					.SetEase(Ease.OutSine)
					.OnComplete(() =>
					{
						HandleSwingStop(direction, duration, endRotation);

					});
			});
	}

	private void HandleSwingStop(CardinalDirection direction, float duration, Quaternion endRotation)
	{
		if (_heldItem is SpellItemSO || _heldItem is WandItemSO)
		{
			ShowArm();
		}
		else
		{
			HideArm();
			OnSwingEnd?.Invoke(this, new CardinalDirectionEventArgs { Direction = direction });
		}

		MeleeCollider.EndSwing();
		_armPivotGO.transform.rotation = endRotation;
		SwingDirection.Value = CardinalDirection.None;
		IsSwinging = false;
		// _swingCooldownTimer = new(HeldItem is ToolItemSO swordItemSO ? swordItemSO.SwingCooldown : 0.25f);
		// _thisPlayer.IsPerformingSwing = false;
		// _stoppingSwing = false;
	}

	private void UpdateArmFromItemIndex(int newValue)
	{
		var tempItem = _heldItem;
		// _heldItem = GameManager.Instance.GetItemSOFromItemId(newValue);

		if ((tempItem is WandItemSO || tempItem is SpellItemSO) && (_heldItem is not WandItemSO || _heldItem is not SpellItemSO) && !IsSwinging)
		{
			OnHoldingWandEnd?.Invoke(this, new CardinalDirectionEventArgs { Direction = ArmCardinalDirection });
		}

		// if (IsSwinging)
		// {
		// 	_stoppingSwing = true;
		// }

		if (_heldItem is WandItemSO || _heldItem is SpellItemSO)
		{
			// Set the spellspawnpoint transform.y to a negative version of its current number if helditem is wand, and positive if spell
			float originalY = Mathf.Abs(SpellSpawnTransform.localPosition.y);
			float newY = _heldItem is WandItemSO ? -originalY : originalY;
			SpellSpawnTransform.localPosition = new Vector3(SpellSpawnTransform.localPosition.x, newY, SpellSpawnTransform.localPosition.z);

			ShowArm();

			OnHoldingWandStart?.Invoke(this, new CardinalDirectionEventArgs { Direction = ArmCardinalDirection });
		}
		else
		{
			HideArm();
		}

		// _itemHeldSR.flipX = _heldItem is WandItemSO || _heldItem is ToolItemSO;
		// _itemHeldSR.sprite = _heldItem switch
		// {
		// 	WandItemSO wand => wand.UiDisplay,
		// 	ToolItemSO tool => tool.UiDisplay,
		// 	_ => null
		// };
	}

	public void ShowArm()
	{
		_armGO.SetActive(true);
	}

	public void HideArm()
	{
		_armGO.SetActive(false);
	}

	private float NormalizeAngle(float angle)
	{
		return (angle % 360 + 360) % 360;
	}

	private CardinalDirection DetermineCardinalDirection(float angle)
	{
		if (angle < 45 || angle > 315) return CardinalDirection.East;
		if (angle < 135) return CardinalDirection.North;
		if (angle < 225) return CardinalDirection.West;
		return CardinalDirection.South;
	}

	// private void Awake()
	// {
	// 	// _swingCooldownTimer = new(0);


	// 	// if (_thisPlayer == null)
	// 	// {
	// 	// 	_thisPlayer = transform.root.GetComponent<Player>();

	// 	// 	_thisPlayer.SelectedItemIdNetworkVariable.OnValueChanged += HandleItemIndexChanged;
	// 	// }
	// }

	// public override void OnNetworkSpawn()
	// {
	// 	// UpdateArmFromItemIndex(_thisPlayer.SelectedItemIdNetworkVariable.Value);

	// 	base.OnNetworkSpawn();
	// }

	// private void Update()
	// {
	// 	// if (_thisPlayer.HealthState.IsDead) return;

	// 	// if (IsOwner)
	// 	// {
	// 	// 	// _swingCooldownTimer.Tick(Time.deltaTime);

	// 	// 	Vector3 direction = ActionManager.MouseWorldPosition - (Vector2)transform.position;
	// 	// 	_angleNetworkVariable.Value = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
	// 	// }

	// 	// float angle = NormalizeAngle(_angleNetworkVariable.Value);
	// 	// ArmCardinalDirection = DetermineCardinalDirection(angle);

	// 	// if(HeldItem is WandItemSO || HeldItem is SpellItemSO)
	// 	// {
	// 	// 	if (!IsSwinging)
	// 	// 	{
	// 	// 		RotateArmBasedOnAngle();
	// 	// 	}
	// 	// }
	// 	// else if (HeldItem is ToolItemSO toolItemSO)
	// 	// {
	// 	// 	if (IsSwinging || Pointer.IsOverUI() || Pointer.IsOverInteractable() || !IsOwner || _swingCooldownTimer.RemainingSeconds > 0) return;

	// 	// 	if (GameInput.Instance.GetPrimaryHeldDown())
	// 	// 	{
	// 	// 		// ExecuteSwing(toolItemSO.SwingDuration);
	// 	// 	}
	// 	// }
	// }

	// private void HandleItemIndexChanged(int previousValue, int newValue)
	// {
	// 	UpdateArmFromItemIndex(newValue);
	// }

	// public void ExecuteSwing(float duration, int swingSpellId = -1)
	// {
	// 	switch (ArmCardinalDirection)
	// 	{
	// 		case CardinalDirection.North:
	// 			SwingRpc(150, 30, duration, true, CardinalDirection.North, OwnerClientId, swingSpellId);
	// 			break;
	// 		case CardinalDirection.South:
	// 			SwingRpc(330, 210, duration, false, CardinalDirection.South, OwnerClientId, swingSpellId);
	// 			break;
	// 		case CardinalDirection.West:
	// 			SwingRpc(120, 240, duration, false, CardinalDirection.West, OwnerClientId, swingSpellId);
	// 			break;
	// 		case CardinalDirection.East:
	// 			SwingRpc(60, 300, duration, true, CardinalDirection.East, OwnerClientId, swingSpellId);
	// 			break;
	// 	}
	// }

	// [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
	// private void SwingRpc(float startAngle, float endAngle, float duration, bool clockwise, CardinalDirection direction, ulong clientSenderId, int swingSpellId = -1)
	// {
	// 	if (clientSenderId != OwnerClientId) return;

	// 	if (OwnerClientId == NetworkManager.LocalClientId)
	// 	{
	// 		if (IsSwinging)
	// 		{
	// 			return;
	// 		}
	// 	}

	// 	SetPivotPosition(direction);

	// 	ShowArm();

	// 	OnSwingStart?.Invoke(this, new CardinalDirectionEventArgs { Direction = direction });

	// 	if(swingSpellId > -1)
	// 	{
	// 	    SwingSpellItemSO swingSpell = (SwingSpellItemSO)GameManager.Instance.GetItemSOFromItemId(swingSpellId);
	// 		SoundManager.Instance.PlayOneShot(swingSpell.SwingSound, _thisPlayer.transform.position);

	// 		MeleeCollider.StartSwing(new MeleeCollider.SwingData
	// 		{
	// 		    Damage = swingSpell.Damage,
	// 		    Knockback = swingSpell.Knockback,
	// 		    DetectionBetweenHitsDuration = swingSpell.DetectionBetweenHitsDuration,
	// 		    HitSound = swingSpell.HitSound,
	// 		    ColliderLength = swingSpell.MeleeColliderLength
	// 		});

	// 		var swingVFX = Instantiate(swingSpell.SwingSpellVFX, _thisPlayer.transform.position + Vector3.up * 0.5f, Quaternion.identity, _thisPlayer.transform);
	// 		swingVFX.ExecuteSwingSpellVFX(direction);
	// 	}
	// 	else
	// 	{
	// 		if (HeldItem is ToolItemSO toolItemSO)
	// 		{
	// 			MeleeCollider.StartSwing(new MeleeCollider.SwingData
	// 			{
	// 			    Damage = toolItemSO.MeleeDamage,
	// 			    Knockback = toolItemSO.Knockback,
	// 			    DetectionBetweenHitsDuration = toolItemSO.DetectionBetweenHitsDuration,
	// 			    HitSound = toolItemSO.HitSound,
	// 			    ColliderLength = toolItemSO.MeleeColliderLength
	// 			});
	// 		}
	// 	}

	// 	_thisPlayer.IsPerformingSwing = true;
	// 	IsSwinging = true;

	// 	startAngle = NormalizeAngle(startAngle);
	// 	endAngle = NormalizeAngle(endAngle);

	// 	if (clockwise && endAngle > startAngle) startAngle += 360f;
	// 	else if (!clockwise && startAngle > endAngle) endAngle += 360f;

	// 	Quaternion startRotation = Quaternion.Euler(0, 0, startAngle);
	// 	Quaternion endRotation = Quaternion.Euler(0, 0, endAngle);

	// 	PerformSwing(startRotation, endRotation, duration, direction);
	// }

	// private void RotateArmBasedOnAngle()
	// {
	// 	// float angle = NormalizeAngle(_angleNetworkVariable.Value);
	// 	// _armPivotGO.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

	// 	// SetPivotPosition(ArmCardinalDirection);

	// 	// if (_thisPlayer.GetComponent<PlayerStateMachine>().FacingDirection != ArmCardinalDirection)
	// 	// {
	// 	// 	OnCastingArmDirectionChanged?.Invoke(this, new CardinalDirectionEventArgs { Direction = ArmCardinalDirection });
	// 	// }
	// }

	// public Vector3 GetDirectionNormalized()
	// {
	// 	// Ensure ActionManager.MouseWorldPosition is defined and accessible
	// 	Vector3 direction = (Vector3)ActionManager.MouseWorldPosition - SpellSpawnTransform.position;
	// 	return direction.normalized;
	// }

	// private CardinalDirection DetermineCardinalDirection(float angle)
	// {
	// 	if (angle < 45 || angle > 315) return CardinalDirection.East;
	// 	if (angle < 135) return CardinalDirection.North;
	// 	if (angle < 225) return CardinalDirection.West;
	// 	return CardinalDirection.South;
	// }

	// private float NormalizeAngle(float angle)
	// {
	// 	return (angle % 360 + 360) % 360;
	// }

	// public bool IsArmShown()
	// {
	// 	return _armGO.activeInHierarchy;
	// }
	// public void StopSwing()
	// {
	//     // _stoppingSwing = true;
	//     if(_currentSwingTween != null)
	//     {
	//         _currentSwingTween.Kill();
	//         _currentSwingTween = null;
	//     }
	// }
}