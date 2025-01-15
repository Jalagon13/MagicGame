using System;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerArmSortingLayer : MonoBehaviour
{
	[SerializeField] private bool _isMainHand;
	[SerializeField] private PlayerHand _mainHand;
	[SerializeField] private PlayerHand _offHand;
	[SerializeField] private GameObject _armPivotGO;
	
	private SortingGroup _sortingGroup;
	
	private void Awake()
	{
		_sortingGroup = GetComponent<SortingGroup>();
	}
	
	private void Start()
	{
		if(_isMainHand)
		{
			_mainHand.OnSwingStart += MainHandOnSwingStart;
			_mainHand.OnHoldingWandStart += MainHandOnHoldingWandStart;
			_mainHand.OnCastingArmDirectionChanged += MainHandOnCastingArmDirectionChanged;
		}
		else
		{
			_offHand.OnSwingStart += OffHandOnSwingStart;
			_offHand.OnHoldingWandStart += OffHandOnHoldingWandStart;
			_offHand.OnCastingArmDirectionChanged += OffHandOnCastingArmDirectionChanged;
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

	private void MainHandOnSwingStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
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

	private void OnDestroy()
	{
		if(_isMainHand)
		{
			_mainHand.OnSwingStart -= MainHandOnSwingStart;
			_mainHand.OnHoldingWandStart -= MainHandOnHoldingWandStart;
			_mainHand.OnCastingArmDirectionChanged -= MainHandOnCastingArmDirectionChanged;
		}
		else
		{
			_offHand.OnSwingStart -= MainHandOnSwingStart;
			_offHand.OnHoldingWandStart -= MainHandOnHoldingWandStart;
			_offHand.OnCastingArmDirectionChanged -= OffHandOnCastingArmDirectionChanged;
		}
	}
}
