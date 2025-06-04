using System;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerArmSortingLayer : MonoBehaviour
{
	[SerializeField] private GameObject _armPivotGO;
	
	private PlayerHand _playerHand;
	private SortingGroup _sortingGroup;
	
	private void Awake()
	{
		_sortingGroup = GetComponent<SortingGroup>();
		_playerHand = GetComponent<PlayerHand>();
		_playerHand.SwingDirection.OnValueChanged += OnSwingChanged;
		_playerHand.OnHoldingWandStart += MainHandOnHoldingWandStart;
		_playerHand.OnCastingArmDirectionChanged += MainHandOnCastingArmDirectionChanged;
	}

	private void OnDestroy()
	{
		_playerHand.SwingDirection.OnValueChanged += OnSwingChanged;
		_playerHand.OnHoldingWandStart -= MainHandOnHoldingWandStart;
		_playerHand.OnCastingArmDirectionChanged -= MainHandOnCastingArmDirectionChanged;
	}

    private void OnSwingChanged(CardinalDirection previousValue, CardinalDirection newValue)
    {
		if(newValue == CardinalDirection.None) return;
    
		switch (newValue)
		{
			case CardinalDirection.North:
				PutSpriteBack();
				PivotYToPositive();
				break;
			case CardinalDirection.South:
				PutSpriteFront();
				PivotYToPositive();
				break;
			case CardinalDirection.West:
				PutSpriteFront();
				PivotYToNegative();
				break;
			case CardinalDirection.East:
				PutSpriteFront();
				PivotYToPositive();
				break;
		}
	}

    private void OffHandOnCastingArmDirectionChanged(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				PutSpriteBack();
				PivotYToNegative();
				break;
			case CardinalDirection.South:
				PutSpriteFront();
				PivotYToNegative();
				break;
			case CardinalDirection.West:
				PutSpriteBack();
				PivotYToNegative();
				break;
			case CardinalDirection.East:
				PutSpriteBack();
				PivotYToPositive();
				break;
		}
	}

	private void OffHandOnHoldingWandStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				PutSpriteBack();
				PivotYToNegative();
				break;
			case CardinalDirection.South:
				PutSpriteFront();
				PivotYToNegative();
				break;
			case CardinalDirection.West:
				PutSpriteBack();
				PivotYToNegative();
				break;
			case CardinalDirection.East:
				PutSpriteBack();
				PivotYToPositive();
				break;
		}
	}

	private void OffHandOnSwingStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				PutSpriteBack();
				PivotYToNegative();
				break;
			case CardinalDirection.South:
				PutSpriteFront();
				PivotYToNegative();
				break;
			case CardinalDirection.West:
				PutSpriteBack();
				PivotYToNegative();
				break;
			case CardinalDirection.East:
				PutSpriteBack();
				PivotYToPositive();
				break;
		}
	}

	private void MainHandOnCastingArmDirectionChanged(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				PutSpriteBack();
				PivotYToPositive();
				break;
			case CardinalDirection.South:
				PutSpriteFront();
				PivotYToPositive();
				break;
			case CardinalDirection.West:
				PutSpriteFront();
				PivotYToNegative();
				break;
			case CardinalDirection.East:
				PutSpriteFront();
				PivotYToPositive();
				break;
		}
	}

	private void MainHandOnHoldingWandStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		switch (e.Direction)
		{
			case CardinalDirection.North:
				PutSpriteBack();
				PivotYToPositive();
				break;
			case CardinalDirection.South:
				PutSpriteFront();
				PivotYToPositive();
				break;
			case CardinalDirection.West:
				PutSpriteFront();
				PivotYToNegative();
				break;
			case CardinalDirection.East:
				PutSpriteFront();
				PivotYToPositive();
				break;
		}
	}
	
	private void PutSpriteFront()
	{
		_sortingGroup.sortingOrder = 1;
	}
	
	private void PutSpriteBack()
	{
		_sortingGroup.sortingOrder = -1;
	}
	
	private void PivotYToPositive()
	{
		_armPivotGO.transform.localScale = new Vector3(1, 1, 1);
	}
	
	private void PivotYToNegative()
	{
		_armPivotGO.transform.localScale = new Vector3(1, -1, 1);
	}
}
