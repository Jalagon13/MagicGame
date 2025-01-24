using MoreMountains.Tools;
using UnityEngine;
using TMPro;

public class PlayerManaStatUI : MonoBehaviour
{
	[SerializeField] private MMProgressBar _manaBar;
	[SerializeField] private RectTransform _border; // Set width to max mana dynamically
	[SerializeField] private TextMeshProUGUI _amountText;

	private void Start()
	{
		ActionManager.Instance.OnPlayerManaUpdated += OnPlayerManaUpdated;
		HotbarManager.Instance.OnFocusSlotUpdated += CheckForSpellBook;
	}

	private void CheckForSpellBook(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
		var mainHandItemSO = GameManager.Instance.GetItemSOFromItemId(e.MainHandItemIndex);
		
		if(mainHandItemSO is SpellBookItemSO)
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
		UpdateBarFill(e.NewValue, e.MaxValue);
	}

	public void UpdateBarFill(int currentAmount, int maxAmount)
	{
		_manaBar.UpdateBar(currentAmount, 0, maxAmount);
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
		ActionManager.Instance.OnPlayerManaUpdated -= OnPlayerManaUpdated;
		HotbarManager.Instance.OnFocusSlotUpdated -= CheckForSpellBook;
	}
}
