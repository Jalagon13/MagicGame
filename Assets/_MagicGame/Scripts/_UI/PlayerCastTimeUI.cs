using MoreMountains.Tools;
using UnityEngine;
using TMPro;
using System;

public class PlayerCastTimeUI : MonoBehaviour
{
    [SerializeField] private PlayerManaStatUI _manaStatUI;
    [SerializeField] private MMProgressBar _castTimeBar;
    // [SerializeField] private RectTransform _border; // Set width to max mana dynamically
    // [SerializeField] private TextMeshProUGUI _amountText;

    private void Start()
    {
        HotbarManager.Instance.OnFocusSlotUpdated += HandleUI;
    }

    private void HandleUI(object sender, HotbarManager.OnFocusItemSetEventArgs e)
    {
        // HideBar();
    }

    private void Update()
    {
        float maxAmount = MagicManager.Instance.CastTimeTimer.Duration;
        float currentAmount = maxAmount - MagicManager.Instance.CastTimeTimer.RemainingSeconds;

        if (currentAmount >= maxAmount)
        {
            HideBar();
        }
        else
        {
            ShowBar();
        }

        // _border.sizeDelta = new Vector2(e.MaxAmount, _border.sizeDelta.y);

        UpdateBarFill(currentAmount, maxAmount);
    }

    public void UpdateBarFill(float currentAmount, float maxAmount)
    {
        if (currentAmount <= 0 || currentAmount >= maxAmount)
        {
            HideBar();
            return;
        }
        
        // Debug.Log($"Current Amount: {currentAmount} Max Amount: {maxAmount}");
        float curr = (currentAmount / maxAmount) * _manaStatUI.MaxMana;
        
        _castTimeBar.UpdateBar(curr, 0, _manaStatUI.MaxMana);
        // _border.sizeDelta = new Vector2(_manaStatUI.MaxMana * 2, _border.sizeDelta.y);
        // _amountText.text = $"{Mathf.RoundToInt(curr)}/{_manaStatUI.MaxMana}";
    }

    private void ShowBar()
    {
        gameObject.transform.GetChild(0).gameObject.SetActive(true);
    }

    private void HideBar()
    {
        gameObject.transform.GetChild(0).gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        HotbarManager.Instance.OnFocusSlotUpdated -= HandleUI;
    }
}
