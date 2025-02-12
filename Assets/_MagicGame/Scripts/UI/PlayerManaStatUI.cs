using MoreMountains.Tools;
using UnityEngine;
using TMPro;

public class PlayerManaStatUI : MonoBehaviour
{
	public float MaxMana { get; private set; }

	[SerializeField] private MMProgressBar _manaBar;
	[SerializeField] private RectTransform _border; // Set width to max mana dynamically
	[SerializeField] private TextMeshProUGUI _amountText;

	private void Start()
	{
		ActionManager.Instance.OnPlayerManaUpdated += OnPlayerManaUpdated;
		HotbarManager.Instance.OnFocusSlotUpdated += CheckForWand;
	}

	private void CheckForWand(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
		var mainHandItemSO = GameManager.Instance.GetItemSOFromItemId(e.MainHandItemIndex);
		
		if(mainHandItemSO is WandItemSO)
		{
			ShowBar();
		}
		else
		{
			HideBar();
		}
	}

	private void OnPlayerManaUpdated(object sender, ActionManager.OnStatUpdatedEventArgs e)
	{
		UpdateBarFill(e.CurrentAmount, e.MaxAmount);
	}

	public void UpdateBarFill(float currentAmount, float maxAmount)
	{
		_manaBar.UpdateBar(currentAmount, 0, maxAmount);
		_border.sizeDelta = new Vector2(maxAmount, _border.sizeDelta.y);
		_amountText.text = $"{Mathf.RoundToInt(currentAmount)}/{maxAmount}";
		
		MaxMana = maxAmount;
	}
	
	private void ShowBar()
	{
		gameObject.SetActive(true);
	}
	
	private void HideBar()
	{
		gameObject.SetActive(false);
	}
	
	private void OnDestroy()
	{
		ActionManager.Instance.OnPlayerManaUpdated -= OnPlayerManaUpdated;
		HotbarManager.Instance.OnFocusSlotUpdated -= CheckForWand;
	}
}
