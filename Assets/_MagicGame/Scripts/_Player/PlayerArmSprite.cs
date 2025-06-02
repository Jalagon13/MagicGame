using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerArmSprite : NetworkBehaviour
{
	[SerializeField] private PlayerHand _mainHand;
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
	}

	
	private void MainHandOnCastingArmDirectionChanged(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				ShowRightSide(false);
				ShowLeftSide(true);
				break;
			case CardinalDirection.South:
				ShowLeftSide(false);
				ShowRightSide(true);
				break;
			case CardinalDirection.West:
				ShowRightSide(false);
				ShowLeftSide(true);
				break;
			case CardinalDirection.East:
				ShowLeftSide(false);
				ShowRightSide(true);
				break;
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
		base.OnDestroy();
	}
}
