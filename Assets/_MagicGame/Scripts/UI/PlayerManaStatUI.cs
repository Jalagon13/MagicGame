using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System;

public class PlayerManaStatUI : MonoBehaviour
{
	[SerializeField] private MMProgressBar _manaBar;
	[SerializeField] private RectTransform _border; // Set width to max mana dynamically
	[SerializeField] private TextMeshProUGUI _amountText;
	
	private Vector3 _originalPosition;
	private RectTransform _rTransform;
	private Canvas _canvas;
	
	private void Awake()
	{
		_rTransform = GetComponent<RectTransform>();
		_originalPosition = _rTransform.anchoredPosition;
		_canvas = transform.parent.GetComponent<Canvas>();
		
		Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
	}
	
	private void Player_OnAnyPlayerSpawned(object sender, Player.PlayerIdEventArgs e)
	{
		if(Player.LocalClientInstance != null)
		{
			Player.LocalClientInstance.OnPlayerManaUpdated += Player_OnPlayerManaUpdated;
		}
	}

	private void Player_OnPlayerManaUpdated(object sender, Player.OnStatUpdatedEventArgs e)
	{
		UpdateBarFill(e.NewValue, e.MaxValue);
	}

	public void UpdateBarFill(int currentAmount, int maxAmount)
	{
		_manaBar.UpdateBar(currentAmount, 0, maxAmount);
		_border.sizeDelta = new Vector2(maxAmount, _border.sizeDelta.y);
		_amountText.text = $"{currentAmount}/{maxAmount}";
	}
	
	public void AnchorToWorldPoint(Vector3 worldPoint, float tweenDuration)
	{
		// Convert world space to screen space
		Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPoint);
		
		// Convert screen space to local point in the canvas RectTransform
		Vector2 localPoint;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			(RectTransform)_canvas.transform, 
			screenPosition, 
			_canvas.worldCamera, 
			out localPoint);
		
		// Set the UI element's position and rotation to the local point in the canvas
		_rTransform.SetLocalPositionAndRotation(localPoint, Quaternion.identity);
		
		// Set the scale to 0 and lerp it into the original local scale
		Vector3 localScale = _rTransform.localScale;
		_rTransform.localScale = Vector2.zero;
		_rTransform.DOScale(localScale, tweenDuration).SetEase(Ease.InOutQuad);
	}
	
	public void CancelAnchor()
	{
		DOTween.ClearCachedTweens();
		_rTransform.anchoredPosition = _originalPosition;
	}
	
	private void OnDestroy()
	{
		Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
	
		if(Player.LocalClientInstance != null)
		{
			Player.LocalClientInstance.OnPlayerManaUpdated -= Player_OnPlayerManaUpdated;
		}
	}
}
