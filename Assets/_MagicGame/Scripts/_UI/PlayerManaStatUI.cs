using MoreMountains.Tools;
using UnityEngine;
using TMPro;

public class PlayerManaStatUI : MonoBehaviour
{
	public float MaxMana { get; private set; }

	[SerializeField] private MMProgressBar _manaBar;
	[SerializeField] private RectTransform _border; // Set width to max mana dynamically
	[SerializeField] private TextMeshProUGUI _amountText;

	private void Awake()
	{
		Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
	}

	private void OnDestroy()
	{
		Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
		if (Player.Instance != null)
		{
			Player.Instance.PlayerManaSystem.OnManaChanged -= Player_OnPlayerManaUpdated;
		}
	}

	private void Player_OnAnyPlayerSpawned(object sender, Player.PlayerIdEventArgs e)
	{
		if (Player.Instance != null)
		{
			Player.Instance.PlayerManaSystem.OnManaChanged += Player_OnPlayerManaUpdated;
			UpdateView(
				Mathf.FloorToInt(Player.Instance.ServerCharacter.Stats.MaxMana.GetValue()),
				Mathf.FloorToInt(Player.Instance.ServerCharacter.Stats.MaxMana.GetValue())
			);
		}
	}

	private void Player_OnPlayerManaUpdated(object sender, PointsChangedEventArgs e)
	{
		UpdateView(e.CurrentPoints, e.MaxPoints);
	}

	private void UpdateView(int currentAmount, int maxAmount)
	{
		// Debug.Log($"Updating Mana UI: Current={currentAmount}, Max={maxAmount}");
		_manaBar.UpdateBar(currentAmount, 0, maxAmount);
		// _border.sizeDelta = new Vector2(maxAmount * 2, _border.sizeDelta.y);
		_amountText.text = $"{currentAmount}/{maxAmount}";
	}
}
