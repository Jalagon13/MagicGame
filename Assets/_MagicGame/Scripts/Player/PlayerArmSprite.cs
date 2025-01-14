using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerArmSprite : NetworkBehaviour
{
	[SerializeField] private PlayerHand _mainHand;
	[SerializeField] private PlayerHand _offHand;
	[SerializeField] private SpriteMask _rightSideSpriteMask;
	[SerializeField] private SpriteMask _leftSideSpriteMask;
	
	private Player _thisPlayer;
	
	private void Awake()
	{
		_thisPlayer = transform.root.GetComponent<Player>();
		
		_mainHand.OnSwingStart += MainHandOnSwingStart;
		_mainHand.OnSwingEnd += MainHandOnSwingEnd;
		_mainHand.OnHoldingWandEnd += MainHandOnHoldingWandEnd;
		_mainHand.OnHoldingWandStart += MainHandOnHoldingWandStart;
		_mainHand.OnCastingArmDirectionChanged += MainHandOnCastingArmDirectionChanged;
		
		_offHand.OnSwingStart += OffHandOnSwingStart;
		_offHand.OnSwingEnd += OffHandOnSwingEnd;
		_offHand.OnHoldingWandEnd += OffHandOnHoldingWandEnd;
		_offHand.OnHoldingWandStart += OffHandOnHoldingWandStart;
		_offHand.OnCastingArmDirectionChanged += OffHandOnCastingArmDirectionChanged;
	}

	public override void OnNetworkSpawn()
	{
		if(_thisPlayer.IsHoldingAWand())
		{
			
		}
		else
		{
			
		}
	
		base.OnNetworkSpawn();
	}
	
	
	private void OffHandOnCastingArmDirectionChanged(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				ShowLeftSide(false);
				break;
			case CardinalDirection.South:
				ShowRightSide(false);
				break;
			case CardinalDirection.West:
				ShowLeftSide(false);
				break;
			case CardinalDirection.East:
				ShowRightSide(false);
				break;
		}
		
		if(!_mainHand.IsArmShown())
		{
			switch (e.Direction)
			{
				case CardinalDirection.North:
					ShowRightSide(true);
					break;
				case CardinalDirection.South:
					ShowLeftSide(true);
					break;
				case CardinalDirection.West:
					ShowRightSide(true);
					break;
				case CardinalDirection.East:
					ShowLeftSide(true);
					break;
			}
		}
	}

	private void OffHandOnHoldingWandStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				ShowLeftSide(false);
				break;
			case CardinalDirection.South:
				ShowRightSide(false);
				break;
			case CardinalDirection.West:
				ShowLeftSide(false);
				break;
			case CardinalDirection.East:
				ShowRightSide(false);
				break;
		}
	}

	private void OffHandOnHoldingWandEnd(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				ShowLeftSide(true);
				break;
			case CardinalDirection.South:
				ShowRightSide(true);
				break;
			case CardinalDirection.West:
				ShowLeftSide(true);
				break;
			case CardinalDirection.East:
				ShowRightSide(true);
				break;
		}
	}

	private void OffHandOnSwingEnd(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				ShowLeftSide(true);
				break;
			case CardinalDirection.South:
				ShowRightSide(true);
				break;
			case CardinalDirection.West:
				ShowLeftSide(true);
				break;
			case CardinalDirection.East:
				ShowRightSide(true);
				break;
		}
	}

	private void OffHandOnSwingStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				ShowLeftSide(false);
				break;
			case CardinalDirection.South:
				ShowRightSide(false);
				break;
			case CardinalDirection.West:
				ShowLeftSide(false);
				break;
			case CardinalDirection.East:
				ShowRightSide(false);
				break;
		}
	}
	
	
	
	
	private void MainHandOnCastingArmDirectionChanged(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				ShowRightSide(false);
				break;
			case CardinalDirection.South:
				ShowLeftSide(false);
				break;
			case CardinalDirection.West:
				ShowRightSide(false);
				break;
			case CardinalDirection.East:
				ShowLeftSide(false);
				break;
		}
		
		if(!_offHand.IsArmShown())
		{
			switch (e.Direction)
			{
				case CardinalDirection.North:
					ShowLeftSide(true);
					break;
				case CardinalDirection.South:
					ShowRightSide(true);
					break;
				case CardinalDirection.West:
					ShowLeftSide(true);
					break;
				case CardinalDirection.East:
					ShowRightSide(true);
					break;
			}
		}
	}
	
	private void MainHandOnHoldingWandStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				ShowRightSide(false);
				break;
			case CardinalDirection.South:
				ShowLeftSide(false);
				break;
			case CardinalDirection.West:
				ShowRightSide(false);
				break;
			case CardinalDirection.East:
				ShowLeftSide(false);
				break;
		}
	}

	private void MainHandOnHoldingWandEnd(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				ShowRightSide(true);
				break;
			case CardinalDirection.South:
				ShowLeftSide(true);
				break;
			case CardinalDirection.West:
				ShowRightSide(true);
				break;
			case CardinalDirection.East:
				ShowLeftSide(true);
				break;
		}
	}

	private void MainHandOnSwingEnd(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				ShowRightSide(true);
				break;
			case CardinalDirection.South:
				ShowLeftSide(true);
				break;
			case CardinalDirection.West:
				ShowRightSide(true);
				break;
			case CardinalDirection.East:
				ShowLeftSide(true);
				break;
		}
	}

	private void MainHandOnSwingStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				ShowRightSide(false);
				break;
			case CardinalDirection.South:
				ShowLeftSide(false);
				break;
			case CardinalDirection.West:
				ShowRightSide(false);
				break;
			case CardinalDirection.East:
				ShowLeftSide(false);
				break;
		}
	}
	
	private void ShowRightSide(bool show)
	{
		_rightSideSpriteMask.enabled = !show;
	}
	
	private void ShowLeftSide(bool show)
	{
		_leftSideSpriteMask.enabled = !show;
	}
	
	public override void OnDestroy()
	{
		_mainHand.OnSwingStart -= MainHandOnSwingStart;
		_mainHand.OnSwingEnd -= MainHandOnSwingEnd;
		_mainHand.OnHoldingWandEnd -= MainHandOnHoldingWandEnd;
		_mainHand.OnHoldingWandStart -= MainHandOnHoldingWandStart;
		_mainHand.OnCastingArmDirectionChanged -= MainHandOnCastingArmDirectionChanged;
		
		_offHand.OnSwingStart -= MainHandOnSwingStart;
		_offHand.OnSwingEnd -= MainHandOnSwingEnd;
		_offHand.OnHoldingWandEnd -= MainHandOnHoldingWandEnd;
		_offHand.OnHoldingWandStart -= MainHandOnHoldingWandStart;
		_offHand.OnCastingArmDirectionChanged -= OffHandOnCastingArmDirectionChanged;
		base.OnDestroy();
	}
}
