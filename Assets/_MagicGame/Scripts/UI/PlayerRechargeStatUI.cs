using MoreMountains.Tools;
using UnityEngine;
using TMPro;


public class PlayerRechargeStatUI : MonoBehaviour
{
	[SerializeField] private MMProgressBar _rechargeBar;
	[SerializeField] private RectTransform _border; // Set width to max mana dynamically
	[SerializeField] private TextMeshProUGUI _amountText;

	private int _maxValue;
	private int _newValue;

	private void Start()
	{
		ActionManager.Instance.OnPlayerManaRechargeUpdated += OnPlayerManaRechargeUpdated;
		HotbarManager.Instance.OnFocusSlotUpdated += CheckForSpellBook;
	}

	private void CheckForSpellBook(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
		var mainHandItemSO = GameManager.Instance.GetItemSOFromItemId(e.MainHandItemIndex);
		
		if(mainHandItemSO is not SpellBookItemSO)
		{
			HideBar();
		}
	}

	private void OnPlayerManaRechargeUpdated(object sender, ActionManager.OnStatUpdatedEventArgs e)
	{
		if(e.NewValue < e.MaxValue)
		{
			ShowBar();
		}
		else
		{
			HideBar();
		}
		
		UpdateBarFill(e.NewValue, e.MaxValue);
	}

	public void UpdateBarFill(int currentAmount, int maxAmount)
	{
		_rechargeBar.UpdateBar(currentAmount, 0, maxAmount);
		_border.sizeDelta = new Vector2(maxAmount, _border.sizeDelta.y);
		_amountText.text = $"{currentAmount}/{maxAmount}";
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
		ActionManager.Instance.OnPlayerManaUpdated -= OnPlayerManaRechargeUpdated;
		HotbarManager.Instance.OnFocusSlotUpdated -= CheckForSpellBook;
	}
}
