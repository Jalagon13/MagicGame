using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerArmSprite : NetworkBehaviour
{
	[SerializeField] private PlayerHand _playerHand;
	[SerializeField] private SpriteMask _rightSideSpriteMask;
	[SerializeField] private SpriteMask _leftSideSpriteMask;
	
	private Player _thisPlayer;
	
	private void Awake()
	{
		_thisPlayer = transform.root.GetComponent<Player>();
		_playerHand.SwingDirection.OnValueChanged += OnSwingDirectionChanged;
		_playerHand.OnHoldingWandEnd += MainHandOnHoldingWandEnd;
		_playerHand.OnHoldingWandStart += MainHandOnHoldingWandStart;
		_playerHand.OnCastingArmDirectionChanged += MainHandOnCastingArmDirectionChanged;

	}

	public override void OnDestroy()
	{
		_playerHand.SwingDirection.OnValueChanged -= OnSwingDirectionChanged;
		_playerHand.OnHoldingWandEnd -= MainHandOnHoldingWandEnd;
		_playerHand.OnHoldingWandStart -= MainHandOnHoldingWandStart;
		_playerHand.OnCastingArmDirectionChanged -= MainHandOnCastingArmDirectionChanged;
	}

    private void OnSwingDirectionChanged(CardinalDirection previousValue, CardinalDirection newValue)
    {
		if(newValue == CardinalDirection.None)
		{
			// Swing done
			switch (previousValue)
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
		else
		{
			// Swing started
			switch (newValue)
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

	private void ShowRightSide(bool show)
	{
		_rightSideSpriteMask.enabled = !show;
		Debug.Log($"Showing right side: {show}");
	}
	
	private void ShowLeftSide(bool show)
	{
		_leftSideSpriteMask.enabled = !show;
		Debug.Log($"Showing left side: {show}");
	}
}
