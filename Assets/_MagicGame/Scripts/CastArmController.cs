using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.Events;

public class CastArmController : NetworkBehaviour
{
	public event EventHandler OnHoldingWandStart;
	public event EventHandler<OnHoldingWandEndEventArgs> OnHoldingWandEnd;
	public class OnHoldingWandEndEventArgs : EventArgs
	{
		public CardinalDirection WandHeldDirection;
	}
	
	// NTFS: Start with this variable next time when you decide to solve this bug of the client joining the host's server and the server player not updated correctly
	// public NetworkVariable<bool> IsHoldingWandNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	[SerializeField] private SwingController _swingController;
	[SerializeField] private CastArmPivot _castArmPivot;
	[SerializeField] private SpriteRenderer _castArmSpriteRenderer;
	private ItemSO _focusItemSO;
	private Player _thisPlayer;
	
	private void Awake()
	{
		_swingController.OnSwingEnd += SwingController_OnSwingEnd;
		
		if(_thisPlayer == null)
		{
			_thisPlayer = transform.root.GetComponent<Player>();
			_thisPlayer.GetFocusItemIndexNetworkVariable().OnValueChanged += Player_FocusItemIndexNetworkVariable_OnValueChanged;
		}
	}

	public override void OnNetworkSpawn()
	{
		if(_thisPlayer.IsHoldingWand())
		{
			ShowCastArm();
		}
		else
		{
			HideCastArm();
		}
	
		base.OnNetworkSpawn();
	}

	private void SwingController_OnSwingEnd(object sender, EventArgs e)
	{
		if(_focusItemSO != null && _focusItemSO is WandItemSO)
		{
			SetCastingArmHolding();
		}
	}

	private void Player_FocusItemIndexNetworkVariable_OnValueChanged(int previousValue, int newValue)
	{
		_focusItemSO = GameManager.Instance.GetItemSOFromIndex(newValue);
		
		_castArmSpriteRenderer.sprite = _focusItemSO == null ? null : _focusItemSO.UiDisplay;
		
		if(!_thisPlayer.IsSwingGoingOn())
		{
			CastArmUpdate();
		}
	}
	
	public void CastArmUpdate()
	{
		if(_focusItemSO == null)
		{
			HideCastArm();
			return;
		}
	
		if(_focusItemSO is WandItemSO || _focusItemSO is SimpleWandItemSO)
		{
			SetCastingArmHolding();
		}
		else
		{
			HideCastArm();
		}
	}
	
	private void SetCastingArmHolding()
	{
		OnHoldingWandStart?.Invoke(this, EventArgs.Empty);
		ShowCastArm();
	}
	
	private void ShowCastArm()
	{
		_castArmPivot.gameObject.SetActive(true);
	}
	
	private void HideCastArm()
	{
		OnHoldingWandEnd?.Invoke(this, new OnHoldingWandEndEventArgs
		{
			WandHeldDirection = _castArmPivot.CastArmDirection
		});
	
		_castArmPivot.gameObject.SetActive(false);
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
		
		_swingController.OnSwingEnd -= SwingController_OnSwingEnd;
		_thisPlayer.GetFocusItemIndexNetworkVariable().OnValueChanged -= Player_FocusItemIndexNetworkVariable_OnValueChanged;
	}
}
