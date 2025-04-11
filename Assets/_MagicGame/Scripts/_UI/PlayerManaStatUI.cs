using MoreMountains.Tools;
using UnityEngine;
using TMPro;

public class PlayerManaStatUI : MonoBehaviour
{
	public float MaxMana { get; private set; }

	[SerializeField] private MMProgressBar _manaBar;
	[SerializeField] private RectTransform _border; // Set width to max mana dynamically
	[SerializeField] private TextMeshProUGUI _amountText;

    private void Update()
    {
		if(PlayerStats.Instance == null) return;
    
		_border.sizeDelta = new Vector2(PlayerStats.Instance.BaseMana, _border.sizeDelta.y);
		_manaBar.UpdateBar(PlayerStats.Instance.CurrentMana, 0, PlayerStats.Instance.BaseMana);
		_border.sizeDelta = new Vector2(PlayerStats.Instance.BaseMana * 2, _border.sizeDelta.y);
		_amountText.text = $"{Mathf.RoundToInt(PlayerStats.Instance.CurrentMana)}/{PlayerStats.Instance.BaseMana}";

		MaxMana = PlayerStats.Instance.BaseMana;
	}

	public void UpdateBarFill(float currentAmount, float maxAmount)
	{
		
	}
	
	private void ShowBar()
	{
		gameObject.SetActive(true);
	}
	
	private void HideBar()
	{
		gameObject.SetActive(false);
	}
}
