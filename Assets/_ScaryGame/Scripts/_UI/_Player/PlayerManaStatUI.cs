using MoreMountains.Tools;
using UnityEngine;
using TMPro;
using System;

public class PlayerManaStatUI : MonoBehaviour
{
	[SerializeField] private MMProgressBar _manaBar;
	[SerializeField] private RectTransform _border; // Set width to max mana dynamically
	[SerializeField] private TextMeshProUGUI _amountText;

	private void Awake()
	{
		Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
	}

	private void OnDestroy()
	{
		if (Player.Instance != null)
		{
			Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
			Player.Instance.SpellCastController.PlayerManaSystem.OnManaChanged -= Player_OnPlayerManaUpdated;
		}
	}

    private void Player_OnAnyPlayerSpawned(object sender, Player.PlayerIdEventArgs e)
	{
		if (Player.Instance != null && e.PlayerId == Player.Instance.OwnerClientId)
		{
			Player.Instance.SpellCastController.PlayerManaSystem.OnManaChanged += Player_OnPlayerManaUpdated;
		}
	}

    private void Player_OnPlayerManaUpdated(object sender, PlayerManaSystem.ManaChangedEventArgs e)
    {
        UpdateView(e.CurrentMana, e.MaxMana);
    }

	private void UpdateView(int currentAmount, int maxAmount)
	{
	    if (!_manaBar || !_amountText) return;

	    _manaBar.UpdateBar(currentAmount, 0, maxAmount);
	    _amountText.text = $"{currentAmount}/{maxAmount}";
	}
}
