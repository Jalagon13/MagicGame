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

	// Inner Class
	public class CardinalDirectionEventArgs : EventArgs
	{
		public CardinalDirection Direction;
	}

	// Serialized Fields
	[SerializeField] private bool _isMainHand;
	[SerializeField] private PlayerHand _oppositeHand;
	[FoldoutGroup("Pivots"), SerializeField] private Transform _northPivot;
	[FoldoutGroup("Pivots"), SerializeField] private Transform _southPivot;
	[FoldoutGroup("Pivots"), SerializeField] private Transform _eastPivot;
	[FoldoutGroup("Pivots"), SerializeField] private Transform _westPivot;
	[SerializeField] private SpriteRenderer _itemHeldSR;
	[SerializeField] private GameObject _armPivotGO;
	[SerializeField] private GameObject _armGO;

	private Player _thisPlayer;
	private ItemSO _heldItem;
	private SortingGroup _sortingGroup;
	private bool _isSwinging;
	private bool _stoppingSwing;

	private NetworkVariable<float> _angleNetworkVariable = new(
		default,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Owner
	);

	// Properties
	public CardinalDirection ArmDirection { get; private set; }

	#region Unity Callbacks

	private void Awake()
	{
		_sortingGroup = GetComponent<SortingGroup>();
	}

	private void Start()
	{
		_thisPlayer = transform.root.GetComponent<Player>();

		if (_isMainHand)
		{
			_thisPlayer.GetMainHandItemIndexNetworkVariable().OnValueChanged += HandleItemIndexChanged;
		}
		else
		{
			_thisPlayer.GetOffHandItemIndexNetworkVariable().OnValueChanged += HandleItemIndexChanged;
		}

		HideArm();
	}

	private void Update()
	{
		if (Player.LocalClientInstance.IsDead() || !Player.LocalClientInstance.IsOwner) return;

		if (IsOwner)
		{
			Vector3 direction = ActionManager.MouseWorldPosition - (Vector2)transform.position;
			_angleNetworkVariable.Value = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		}
		
		float angle = NormalizeAngle(_angleNetworkVariable.Value);
		ArmDirection = DetermineCardinalDirection(angle);
		
		if (_heldItem is MeleeItemSO)
		{
			if (_isSwinging || Pointer.IsOverUI()) return;

			bool hasSwingInput = _isMainHand
				? GameInput.Instance.GetPrimaryHeldDown()
				: GameInput.Instance.GetSecondaryHeldDown();

			if (hasSwingInput)
			{
				Debug.Log($"Executing swing in {ArmDirection}");
				switch (ArmDirection)
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
		else if (_heldItem is WandItemSO && !_isSwinging)
		{
			RotateArmBasedOnAngle();
			
			_sortingGroup.sortingOrder = ArmDirection == CardinalDirection.North ? -1 : 1;
		}
	}

	public override void OnDestroy()
	{
		if (_isMainHand)
		{
			_thisPlayer.GetMainHandItemIndexNetworkVariable().OnValueChanged -= HandleItemIndexChanged;
		}
		else
		{
			_thisPlayer.GetOffHandItemIndexNetworkVariable().OnValueChanged -= HandleItemIndexChanged;
		}

		base.OnDestroy();
	}

	#endregion

	#region Item Handling

	private void HandleItemIndexChanged(int previousValue, int newValue)
	{
		var tempItem = _heldItem;
		_heldItem = GameManager.Instance.GetItemSOFromIndex(newValue);

		if (tempItem is WandItemSO && _heldItem is not WandItemSO && !_isSwinging)
		{
			OnHoldingWandEnd?.Invoke(this, new CardinalDirectionEventArgs { Direction = ArmDirection });
		}
		
		if(_isSwinging)
		{
			_stoppingSwing = true;
		}

		if (_heldItem is WandItemSO)
		{
			ShowArm();
			
			OnHoldingWandStart?.Invoke(this, new CardinalDirectionEventArgs { Direction = ArmDirection });
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
	private void Swing(float startAngle, float endAngle, float duration, bool clockwise, CardinalDirection direction)
	{
		if (_isSwinging) return;
		Debug.Log($"InSwing function for direction {direction}");
		
		SetPivotPosition(direction);
		
		StartCoroutine(SwingCoroutine(startAngle, endAngle, duration, clockwise, direction));
	}

	private IEnumerator SwingCoroutine(float startAngle, float endAngle, float duration, bool clockwise, CardinalDirection direction)
	{
		ShowArm();
		Debug.Log($"In Swing co routine for direction {direction}");
		OnSwingStart?.Invoke(this, new CardinalDirectionEventArgs { Direction = direction });

		_thisPlayer.SetIsPerformingSwing(true);
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
		_thisPlayer.SetIsPerformingSwing(false);
		_stoppingSwing = false;
	}

	#endregion

	#region Helpers

	private void RotateArmBasedOnAngle()
	{
		float angle = NormalizeAngle(_angleNetworkVariable.Value);
		_armPivotGO.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
		
		SetPivotPosition(ArmDirection);

		if (_thisPlayer.GetComponent<PlayerStateMachine>().MovingDirection != ArmDirection)
		{
			OnCastingArmDirectionChanged?.Invoke(this, new CardinalDirectionEventArgs { Direction = ArmDirection });

			if (_oppositeHand.IsSwinging()) _oppositeHand.StopSwing();

			_armPivotGO.transform.localScale = new Vector3(1, ArmDirection == CardinalDirection.West ? -1 : 1, 1);
		}
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

	public bool IsArmShown() => _armPivotGO.activeInHierarchy;
	public bool IsSwinging() => _isSwinging;
	public void StopSwing() => _stoppingSwing = true;

	#endregion

	#region Swing Direction Methods

	private void SwingEast(float duration) => Swing(60, 300, duration, true, CardinalDirection.East);
	private void SwingWest(float duration) => Swing(120, 240, duration, false, CardinalDirection.West);
	private void SwingNorth(float duration) => Swing(150, 30, duration, true, CardinalDirection.North);
	private void SwingSouth(float duration) => Swing(330, 210, duration, false, CardinalDirection.South);

	#endregion
}