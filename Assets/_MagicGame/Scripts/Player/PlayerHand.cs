using System;
using System.Collections;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

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

	[SerializeField] private bool _isMainHand;
	[SerializeField] private PlayerHand _oppositeHand;
	[FoldoutGroup("Pivots"), SerializeField] private Transform _northPivot;
	[FoldoutGroup("Pivots"), SerializeField] private Transform _southPivot;
	[FoldoutGroup("Pivots"), SerializeField] private Transform _eastPivot;
	[FoldoutGroup("Pivots"), SerializeField] private Transform _westPivot;
	[SerializeField] private SpriteRenderer _itemHeldSR;
	[SerializeField] private GameObject _armPivotGO;
	[SerializeField] private GameObject _armGO;
	[SerializeField] private PlayerArmVisualPreset _meleeSwingPreset;
	[SerializeField] private PlayerArmVisualPreset _holdingWandPreset;
	[SerializeField] private PlayerArmVisualPreset _armItemAnglePreset;
	[field: SerializeField] public Transform ProjectileSpawnTransform;

	private Player _thisPlayer;
	private ItemSO _heldItem;
	private bool _isSwinging;
	private bool _stoppingSwing;

	private NetworkVariable<float> _angleNetworkVariable = new(
		default,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);
	private NetworkVariable<bool> _swungThisFrameNetworkVariable = new(
		default,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	public CardinalDirection ArmCardinalDirection { get; private set; }

	#region Unity Callbacks

	private void Awake()
	{
		if(_thisPlayer == null)
		{
			_thisPlayer = transform.root.GetComponent<Player>();
			
			if (_isMainHand)
			{
				_thisPlayer.MainHandItemIndexNetworkVariable.OnValueChanged += HandleItemIndexChanged;
			}
			else
			{
				_thisPlayer.OffHandItemIndexNetworkVariable.OnValueChanged += HandleItemIndexChanged;
			}
		}
	}

	public override void OnNetworkSpawn()
	{
		int heldItemValue = _isMainHand ? _thisPlayer.MainHandItemIndexNetworkVariable.Value : _thisPlayer.OffHandItemIndexNetworkVariable.Value;
		
		UpdateArmFromItemIndex(heldItemValue);
		
		base.OnNetworkSpawn();
	}

	private void Update()
	{
		if (_thisPlayer.IsDead()) return;

		if (IsOwner)
		{
			Vector3 direction = ActionManager.MouseWorldPosition - (Vector2)transform.position;
			_angleNetworkVariable.Value = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		}
		
		float angle = NormalizeAngle(_angleNetworkVariable.Value);
		ArmCardinalDirection = DetermineCardinalDirection(angle);
		
		if (_heldItem is MeleeItemSO)
		{
			TryToSwing();
		}
		else if ((_heldItem is WandItemSO || _heldItem is SpellBookItemSO) && !_isSwinging)
		{
			RotateArmBasedOnAngle();
		}
	}
	
	private void TryToSwing()
	{
		if (_isSwinging || Pointer.IsOverUI() || !IsOwner) return;
		
		bool hasSwingInput = _isMainHand
			? GameInput.Instance.GetPrimaryHeldDown()
			: GameInput.Instance.GetSecondaryHeldDown();

		if (hasSwingInput)
		{
			switch (ArmCardinalDirection)
			{
				case CardinalDirection.North:
					SwingNorth(0.35f);
					break;
				case CardinalDirection.South:
					SwingSouth(0.35f);
					break;
				case CardinalDirection.West:
					SwingWest(0.35f);
					break;
				case CardinalDirection.East:
					SwingEast(0.35f);
					break;
			}
		}
	}
	
	private void RotateArmBasedOnAngle()
	{
		float angle = NormalizeAngle(_angleNetworkVariable.Value);
		_armPivotGO.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
		
		SetPivotPosition(ArmCardinalDirection);

		if (_thisPlayer.GetComponent<PlayerStateMachine>().MovingDirection != ArmCardinalDirection)
		{
			OnCastingArmDirectionChanged?.Invoke(this, new CardinalDirectionEventArgs { Direction = ArmCardinalDirection });

			if (_oppositeHand.IsSwinging()) _oppositeHand.StopSwing();
		}
	}

	public override void OnDestroy()
	{
		if (_isMainHand)
		{
			_thisPlayer.MainHandItemIndexNetworkVariable.OnValueChanged -= HandleItemIndexChanged;
		}
		else
		{
			_thisPlayer.OffHandItemIndexNetworkVariable.OnValueChanged -= HandleItemIndexChanged;
		}

		base.OnDestroy();
	}

	#endregion

	#region Item Handling

	private void HandleItemIndexChanged(int previousValue, int newValue)
	{
		UpdateArmFromItemIndex(newValue);
	}
	
	private void UpdateArmFromItemIndex(int newValue)
	{
		var tempItem = _heldItem;
		_heldItem = GameManager.Instance.GetItemSOFromItemId(newValue);

		if ((tempItem is WandItemSO || tempItem is SpellBookItemSO) && (_heldItem is not WandItemSO || _heldItem is not SpellBookItemSO) && !_isSwinging)
		{
			OnHoldingWandEnd?.Invoke(this, new CardinalDirectionEventArgs { Direction = ArmCardinalDirection });
		}
		
		if(_isSwinging)
		{
			_stoppingSwing = true;
		}

		if (_heldItem is WandItemSO || _heldItem is SpellBookItemSO)
		{
			ShowArm();
			ApplyPreset(_holdingWandPreset);
			
			OnHoldingWandStart?.Invoke(this, new CardinalDirectionEventArgs { Direction = ArmCardinalDirection });
		}
		else if (_heldItem is MeleeItemSO)
		{
			HideArm();
		}
		else
		{
			_heldItem = null;
			HideArm();
		}

		_itemHeldSR.sprite = _heldItem?.UiDisplay;
	}

	#endregion

	#region Swing and Wand Handling
	
	[Rpc(SendTo.Everyone, RequireOwnership = false)]
	private void SwingRpc(float startAngle, float endAngle, float duration, bool clockwise, CardinalDirection direction, ulong clientSenderId)
	{
		if (clientSenderId != OwnerClientId) return;
		
		if(OwnerClientId == NetworkManager.LocalClientId)
		{
			if(_isSwinging)
			{
				return;
			}
		}
		
		SetPivotPosition(direction);
		ApplyPreset(_meleeSwingPreset);
		
		StartCoroutine(SwingCoroutine(startAngle, endAngle, duration, clockwise, direction));
	}

	private IEnumerator SwingCoroutine(float startAngle, float endAngle, float duration, bool clockwise, CardinalDirection direction)
	{
		ShowArm();
		
		OnSwingStart?.Invoke(this, new CardinalDirectionEventArgs { Direction = direction });

		SoundManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerMeleeSwing, Player.LocalClientInstance.transform.position);
		
		_thisPlayer.IsPerformingSwing = true;
		_isSwinging = true;

		startAngle = NormalizeAngle(startAngle);
		endAngle = NormalizeAngle(endAngle);

		if (clockwise && endAngle > startAngle) startAngle += 360f;
		else if (!clockwise && startAngle > endAngle) endAngle += 360f;

		Quaternion startRotation = Quaternion.Euler(0, 0, startAngle);
		Quaternion endRotation = Quaternion.Euler(0, 0, endAngle);

		float elapsedTime = 0f;
		while (elapsedTime < duration)
		{
			_armPivotGO.transform.rotation = Quaternion.Lerp(startRotation, endRotation, elapsedTime / duration);
			elapsedTime += Time.deltaTime;

			if (_stoppingSwing)
			{
				HandleSwingStop(direction, duration, endRotation);
				yield break;
			}

			yield return null;
		}

		HandleSwingStop(direction, duration, endRotation);
	}

	private void HandleSwingStop(CardinalDirection direction, float duration, Quaternion endRotation)
	{
		if (_heldItem is WandItemSO) ShowArm();
		else HideArm();

		OnSwingEnd?.Invoke(this, new CardinalDirectionEventArgs { Direction = direction });

		StartCoroutine(FinishSwing(duration, endRotation));
	}

	private IEnumerator FinishSwing(float duration, Quaternion endRotation)
	{
		yield return new WaitForSeconds(duration * 0.3f);
		_armPivotGO.transform.rotation = endRotation;

		_isSwinging = false;
		_thisPlayer.IsPerformingSwing = false;
		_stoppingSwing = false;
	}

	#endregion

	#region Helpers

	public Vector3 GetDirectionNormalized()
	{
		// Ensure ActionManager.MouseWorldPosition is defined and accessible
		Vector3 direction = (Vector3)ActionManager.MouseWorldPosition - ProjectileSpawnTransform.position;
		return direction.normalized;
	}

	private void ApplyPreset(PlayerArmVisualPreset preset)
	{
		CopyTransformValues(preset.GetArmTransform(), _armGO.transform);
		CopyTransformValues(preset.GetItemInHandTransform(), _itemHeldSR.gameObject.transform);
	}
	
	private void CopyTransformValues(Transform source, Transform target)
	{
		if (source == null || target == null)
		{
			Debug.LogError("Source or target Transform is null.");
			return;
		}

		target.position = source.position;
		target.rotation = source.rotation;
		target.localScale = source.localScale;
	}

	private void SetPivotPosition(CardinalDirection direction)
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

	private CardinalDirection DetermineCardinalDirection(float angle)
	{
		if (angle < 45 || angle > 315) return CardinalDirection.East;
		if (angle < 135) return CardinalDirection.North;
		if (angle < 225) return CardinalDirection.West;
		return CardinalDirection.South;
	}

	private float NormalizeAngle(float angle) => (angle % 360 + 360) % 360;

	private void ShowArm()
	{
		_armGO.SetActive(true);
	}

	private void HideArm()
	{
		_armGO.SetActive(false);
	}

	public bool IsArmShown() => _armGO.activeInHierarchy;
	public bool IsSwinging() => _isSwinging;
	public void StopSwing() => _stoppingSwing = true;

	#endregion

	#region Swing Direction Methods

	private void SwingEast(float duration) => SwingRpc(60, 300, duration, true, CardinalDirection.East, OwnerClientId);
	private void SwingWest(float duration) => SwingRpc(120, 240, duration, false, CardinalDirection.West, OwnerClientId);
	private void SwingNorth(float duration) => SwingRpc(150, 30, duration, true, CardinalDirection.North, OwnerClientId);
	private void SwingSouth(float duration) => SwingRpc(330, 210, duration, false, CardinalDirection.South, OwnerClientId);

	#endregion
}