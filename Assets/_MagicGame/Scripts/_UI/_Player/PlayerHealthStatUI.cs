using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthStatUI : MonoBehaviour
{
	[SerializeField] private MMProgressBar _healthBar;
	[SerializeField] private RectTransform _border; // Set width to max health dynamically
	[SerializeField] private TextMeshProUGUI _amountText;
	
	private void Awake()
	{
		Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
	}

	private void OnDestroy()
	{
		Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
		if(Player.Instance != null)
		{
			Player.Instance.ServerCharacter.NetHealthState.OnHitPointsChanged -= Player_OnPlayerHealthUpdated;
		}
	}
	
	private void Player_OnAnyPlayerSpawned(object sender, Player.PlayerIdEventArgs e)
	{
		if(Player.Instance != null)
		{
			Player.Instance.ServerCharacter.NetHealthState.OnHitPointsChanged += Player_OnPlayerHealthUpdated;
			UpdateView(Player.Instance.ServerCharacter.Stats.MaxHealth.AsIntValue, Player.Instance.ServerCharacter.Stats.MaxHealth.AsIntValue);
		}
	}

    private void Player_OnPlayerHealthUpdated(object sender, PointsChangedEventArgs e)
    {
		UpdateView(e.CurrentPoints, e.MaxPoints);
    }

	private void UpdateView(int currentAmount, int maxAmount)
	{
		_healthBar.UpdateBar(currentAmount, 0, maxAmount);
		// _border.sizeDelta = new Vector2(maxAmount * 2, _border.sizeDelta.y);
		_amountText.text = $"{currentAmount}/{maxAmount}";
	}
}
