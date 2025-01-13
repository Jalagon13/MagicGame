using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerArmSprite : NetworkBehaviour
{
	[SerializeField] private PlayerHand _mainHand;
	[SerializeField] private PlayerHand _offHand;
	
	private SpriteRenderer _sr;
	private Player _thisPlayer;
	
	private void Awake()
	{
		_thisPlayer = transform.root.GetComponent<Player>();
		_sr = GetComponent<SpriteRenderer>();
		
		_mainHand.OnSwingStart += OnSwingStart;
		_mainHand.OnSwingEnd += OnSwingEnd;
		_mainHand.OnHoldingWandEnd += OnHoldingWandEnd;
		_mainHand.OnHoldingWandStart += OnHoldingWandStart;
		
		_offHand.OnSwingStart += OnSwingStart;
		_offHand.OnSwingEnd += OnSwingEnd;
		_offHand.OnHoldingWandEnd += OnHoldingWandEnd;
		_offHand.OnHoldingWandStart += OnHoldingWandStart;
	}

	public override void OnNetworkSpawn()
	{
		if(_thisPlayer.IsHoldingAWand())
		{
			Hide();
		}
		else
		{
			Show();
		}
	
		base.OnNetworkSpawn();
	}
	
	private void OnHoldingWandStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		Hide();
	}

	private void OnHoldingWandEnd(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		Show();
	}

	private void OnSwingEnd(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		Show();
	}

	private void OnSwingStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		Hide();
	}
	
	public void Show()
	{
		_sr.enabled = true;
	}
	
	public void Hide()
	{
		_sr.enabled = false;
	}
	
	public override void OnDestroy()
	{
		_mainHand.OnSwingStart -= OnSwingStart;
		_mainHand.OnSwingEnd -= OnSwingEnd;
		_mainHand.OnHoldingWandEnd -= OnHoldingWandEnd;
		_mainHand.OnHoldingWandStart -= OnHoldingWandStart;
		
		_offHand.OnSwingStart -= OnSwingStart;
		_offHand.OnSwingEnd -= OnSwingEnd;
		_offHand.OnHoldingWandEnd -= OnHoldingWandEnd;
		_offHand.OnHoldingWandStart -= OnHoldingWandStart;
		base.OnDestroy();
	}
}
