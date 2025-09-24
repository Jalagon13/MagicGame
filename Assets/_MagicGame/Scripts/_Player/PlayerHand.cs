using System;
using System.Collections;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using UnityEditor;

public class PlayerHand : NetworkBehaviour
{
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

	private Player _thisPlayer;
	private ItemDataSO _heldItem;
    private bool _isHoldingWandOrSpell;

	public NetworkVariable<float> AngleToMouse { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<CardinalDirection> AimDirection { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<CardinalDirection> CastingDirection { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<CardinalDirection> SwingDirection { get; private set; } = new(CardinalDirection.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	private NetworkVariable<ushort> _armorEquippedId = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	
	private Sprite _originalArmSprite;

	private void Awake()
	{
		HideArm();
		
		_originalArmSprite = _armGO.GetComponent<SpriteRenderer>().sprite;
		_thisPlayer = transform.root.GetComponent<Player>();
		_thisPlayer.SelectedItemId.OnValueChanged += OnItemIdChanged;

		if (_thisPlayer.OwnerClientId == NetworkManager.LocalClientId)
		{
			ArmorSlotUI.OnArmorUpdated += OnArmorUpdated;
		}

		_armorEquippedId.OnValueChanged += OnArmorEquippedIdChanged;
	}

	public override void OnDestroy()
	{
		_thisPlayer.SelectedItemId.OnValueChanged -= OnItemIdChanged;

		if (_thisPlayer.OwnerClientId == NetworkManager.LocalClientId)
		{
			ArmorSlotUI.OnArmorUpdated -= OnArmorUpdated;
		}

		_armorEquippedId.OnValueChanged -= OnArmorEquippedIdChanged;
	}

	private void OnArmorUpdated(object sender, ArmorSlotUI.ArmorEquipDataEventArgs e)
	{
		if (e.ArmorType != ArmorType.Chest) return;

		_armorEquippedId.Value = e.ArmorItemData != null
			? GameDataRegistry.Instance.GetItemIdFromItemData(e.ArmorItemData)
			: GameDataRegistry.INVALID_ID;
	}

	private void OnArmorEquippedIdChanged(ushort previousValue, ushort newValue)
	{
		SpriteRenderer armSR = _armGO.GetComponent<SpriteRenderer>();

		if (newValue == GameDataRegistry.INVALID_ID)
		{
			armSR.sprite = _originalArmSprite;
			return;
		}
	
		ArmorItemSO armorItem = GameDataRegistry.Instance.GetItemDataFromItemId(newValue) as ArmorItemSO;
		UnityEngine.Object[] data = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(armorItem.ArmorSprites.ArmSprites));
		
		foreach (UnityEngine.Object obj in data)
		{
			if (obj is Sprite sprite)
			{
				if(sprite.name == armSR.sprite.name)
				{
					armSR.sprite = sprite;
					break;
				}
			}
		}
	}

	private void Update()
	{
		if (IsOwner)
		{
			Vector3 direction = ActionManager.MouseWorldPosition - (Vector2)transform.position;
			AngleToMouse.Value = NormalizeAngle(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
			AimDirection.Value = DetermineCardinalDirection(AngleToMouse.Value);
			
			if(_isHoldingWandOrSpell && !IsSwinging)
			{
				CastingDirection.Value = DetermineCardinalDirection(AngleToMouse.Value);
			}
		}
		
		if((_heldItem is WandItemSO || _heldItem is SpellItemSO || _heldItem is MiningSpellItemSO) && !IsSwinging && CastingDirection.Value != CardinalDirection.None)
		{
			_armPivotGO.transform.rotation = Quaternion.AngleAxis(AngleToMouse.Value, Vector3.forward);
			SetPivotPosition(CastingDirection.Value);
		}
	}

	private void OnItemIdChanged(ushort previousValue, ushort newValue)
    {
		_heldItem = GameDataRegistry.Instance.GetItemDataFromItemId(newValue);

		_isHoldingWandOrSpell = _heldItem is WandItemSO || _heldItem is SpellItemSO || _heldItem is MiningSpellItemSO;
		if(_isHoldingWandOrSpell)
		{
			float originalY = Mathf.Abs(SpellSpawnTransform.localPosition.y);
			float newY = _heldItem is WandItemSO ? -originalY : originalY;
			SpellSpawnTransform.localPosition = new Vector3(SpellSpawnTransform.localPosition.x, newY, SpellSpawnTransform.localPosition.z);
			ShowArm();
		}
		else
		{
			HideArm();
			if (IsOwner)
			{
				CastingDirection.Value = CardinalDirection.None;
				if (_thisPlayer.ServerCharacter.MovementState.Value == MovementState.Idle)
				{
					_thisPlayer.ServerCharacter.CardinalDirection.Value = CastingDirection.Value;
				}
			}
		}
		
		if(!IsSwinging)
		{
			RefreshItemSprite();
		}
	}
	
	private void RefreshItemSprite()
	{
		_itemHeldSR.flipX = _heldItem is WandItemSO || _heldItem is SwordItemSO;
		_itemHeldSR.sprite = _heldItem switch
		{
			WandItemSO wand => wand.UiDisplay,
			SwordItemSO tool => tool.UiDisplay,
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

	[Rpc(SendTo.ClientsAndHost)]
	public void PerformSwingClientRpc(Quaternion startRotation, Quaternion endRotation, float duration, CardinalDirection direction)
	{
		SetPivotPosition(direction);
		ShowArm();

		IsSwinging = true;
		float buildUpDuration = duration / 2f;
		_armPivotGO.transform.rotation = startRotation;
		_itemHeldSR.transform.localScale = Vector3.zero;

		_itemHeldSR.transform.DOScale(Vector3.one, buildUpDuration).SetEase(Ease.OutSine).OnComplete(() =>
		{
			// On swing done
			SoundManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerMeleeSwing, transform.root.transform.position);
			_armPivotGO.transform.DORotateQuaternion(endRotation, duration).SetEase(Ease.OutSine).OnComplete(() =>
			{
				if (_heldItem is WandItemSO)
				{
					if (IsOwner)
					{
						SwingDirection.Value = CardinalDirection.None;
						CastingDirection.Value = DetermineCardinalDirection(AngleToMouse.Value);
						if(_thisPlayer.ServerCharacter.MovementState.Value == MovementState.Idle)
						{
							_thisPlayer.ServerCharacter.CardinalDirection.Value = CastingDirection.Value;
						}
					}
					RefreshItemSprite();
					ShowArm();
				}
				else
				{
					SwingDirection.Value = CardinalDirection.None;
					HideArm();
				}
				
				MeleeCollider.EndSwing();
				IsSwinging = false;
				_armPivotGO.transform.rotation = endRotation;
			});
		});
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
}