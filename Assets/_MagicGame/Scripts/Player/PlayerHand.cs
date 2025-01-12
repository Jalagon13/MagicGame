using System;
using System.Collections;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerHand : NetworkBehaviour
{
	// Event for casting arm direction changes
	public event EventHandler<OnCastingArmDirectionChangedEventArgs> OnCastingArmDirectionChanged;
	public class OnCastingArmDirectionChangedEventArgs : EventArgs
	{
		public CardinalDirection Direction;
	}

	[SerializeField] private bool _isMainHand;
	[FoldoutGroup("Pivots"), SerializeField] private Transform _rightFacePivot;
	[FoldoutGroup("Pivots"), SerializeField] private Transform _backFacePivot;
	[FoldoutGroup("Pivots"), SerializeField] private Transform _leftFacePivot;
	[FoldoutGroup("Pivots"), SerializeField] private Transform _frontFacePivot;

	private SpriteRenderer _itemSpriteRenderer;
	private GameObject _armGameObject;
	private Player _player;
	private ItemSO _heldItem;
	private SortingGroup _sortingGroup;
	private bool _isSwinging;

	private NetworkVariable<float> _angleNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public CardinalDirection CastArmDirection { get; private set; }

	private void Awake()
	{
		_sortingGroup = GetComponent<SortingGroup>();
		_armGameObject = transform.GetChild(0).gameObject;
		_itemSpriteRenderer = _armGameObject.transform.GetChild(0).GetComponent<SpriteRenderer>();
	}

	private void Start()
	{
		_player = transform.root.GetComponent<Player>();

		if (_isMainHand)
			_player.GetMainHandItemIndexNetworkVariable().OnValueChanged += HandleItemIndexChanged;
		else
			_player.GetOffHandItemIndexNetworkVariable().OnValueChanged += HandleItemIndexChanged;

		HideArm();
	}

	private void Update()
	{
		if (Player.LocalClientInstance.IsDead() || !Player.LocalClientInstance.IsOwner || _heldItem == null) return;

		if (_heldItem is MeleeItemSO)
		{
			HandleMeleeSwing();
		}
		else if (_heldItem is WandItemSO)
		{
			HandleWandActions();
		}
	}

	private void HandleMeleeSwing()
	{
		if (_isSwinging || Pointer.IsOverUI()) return;

		bool isSwinging = _isMainHand
			? GameInput.Instance.GetPrimaryHeldDown()
			: GameInput.Instance.GetSecondaryHeldDown();

		if (isSwinging)
		{
			float angle = CalculateMouseAngle();
			SwingBasedOnAngle(angle);
		}
	}

	private void HandleWandActions()
	{
		if (IsOwner)
		{
			Vector3 direction = ActionManager.MouseWorldPosition - (Vector2)transform.position;
			_angleNetworkVariable.Value = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		}

		RotateArmBasedOnAngle();
		UpdateSortingOrder();
	}

	private void SwingBasedOnAngle(float angle)
	{
		if (angle < 45 || angle > 315) SwingEast(0.35f);
		else if (angle < 135) SwingNorth(0.35f);
		else if (angle < 225) SwingWest(0.35f);
		else SwingSouth(0.35f);
	}

	private void RotateArmBasedOnAngle()
	{
		float angle = NormalizeAngle(_angleNetworkVariable.Value);

		CastArmDirection = DetermineCardinalDirection(angle);
		transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

		if (_player.GetComponent<PlayerStateMachine>().MovingDirection != CastArmDirection)
		{
			OnCastingArmDirectionChanged?.Invoke(this, new OnCastingArmDirectionChangedEventArgs { Direction = CastArmDirection });
			transform.localScale = new Vector3(1, CastArmDirection == CardinalDirection.West ? -1 : 1, 1);
		}
	}

	private void UpdateSortingOrder()
	{
		float angle = NormalizeAngle(transform.eulerAngles.z);
		CastArmDirection = DetermineCardinalDirection(angle);

		_sortingGroup.sortingOrder = CastArmDirection == CardinalDirection.North ? -1 : 1;
	}

	private float CalculateMouseAngle()
	{
		Vector2 direction = ActionManager.MouseWorldPosition - (Vector2)transform.position;
		return NormalizeAngle(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
	}

	private CardinalDirection DetermineCardinalDirection(float angle)
	{
		if (angle < 45 || angle > 315) return CardinalDirection.East;
		if (angle < 135) return CardinalDirection.North;
		if (angle < 225) return CardinalDirection.West;
		return CardinalDirection.South;
	}

	private void HandleItemIndexChanged(int previousValue, int newValue)
	{
		HideArm();
		
		_heldItem = GameManager.Instance.GetItemSOFromIndex(newValue);

		if (_heldItem is WandItemSO)
		{
			ShowArm();
		}
		else if (_heldItem is MeleeItemSO)
		{
			HideArm();
		}
		else
		{
			_heldItem = null;
		}

		_itemSpriteRenderer.sprite = _heldItem?.UiDisplay;
	}

	private void SwingEast(float duration) => Swing(60, 300, duration, true);
	private void SwingWest(float duration) => Swing(120, 240, duration, false);
	private void SwingNorth(float duration) => Swing(150, 30, duration, true);
	private void SwingSouth(float duration) => Swing(330, 210, duration, false);

	private void Swing(float startAngle, float endAngle, float duration, bool clockwise)
	{
		if (_isSwinging) return;
		StartCoroutine(SwingCoroutine(startAngle, endAngle, duration, clockwise));
	}

	private IEnumerator SwingCoroutine(float startAngle, float endAngle, float duration, bool clockwise)
	{
		ShowArm();
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
			transform.rotation = Quaternion.Lerp(startRotation, endRotation, elapsedTime / duration);
			elapsedTime += Time.deltaTime;
			yield return null;
		}

		transform.rotation = endRotation;
		HideArm();
		_isSwinging = false;
	}

	private float NormalizeAngle(float angle) => (angle % 360 + 360) % 360;

	private void ShowArm() => _armGameObject.SetActive(true);
	private void HideArm() => _armGameObject.SetActive(false);

	public override void OnDestroy()
	{
		if (_isMainHand)
			_player.GetMainHandItemIndexNetworkVariable().OnValueChanged -= HandleItemIndexChanged;
		else
			_player.GetOffHandItemIndexNetworkVariable().OnValueChanged -= HandleItemIndexChanged;

		base.OnDestroy();
	}
}