using MoreMountains.Tools;
using UnityEngine;
using TMPro;

public class PlayerCastTimeUI : MonoBehaviour
{
    [SerializeField] private PlayerManaStatUI _manaStatUI;
    [SerializeField] private MMProgressBar _castTimeBar;
    [SerializeField] private RectTransform _border; // Set width to max mana dynamically
    [SerializeField] private TextMeshProUGUI _amountText;

    private void Start()
    {
        ActionManager.Instance.OnPlayerSpellChargeUpdated += OnPlayerSpellChargeUpdated;
    }

    private void OnPlayerSpellChargeUpdated(object sender, ActionManager.OnStatUpdatedEventArgs e)
    {
        if(e.CurrentAmount >= e.MaxAmount)
        {
            HideBar();
        }
        else
        {
            ShowBar();
        }
    
        _border.sizeDelta = new Vector2(e.MaxAmount, _border.sizeDelta.y);
        UpdateBarFill(e.CurrentAmount, e.MaxAmount);
    }

    public void UpdateBarFill(float currentAmount, float maxAmount)
    {
        if (currentAmount <= 0) return;
        
        float curr = (currentAmount / maxAmount) * _manaStatUI.MaxMana;
        
        _castTimeBar.UpdateBar(curr, 0, _manaStatUI.MaxMana);
        _border.sizeDelta = new Vector2(_manaStatUI.MaxMana * 2, _border.sizeDelta.y);
        _amountText.text = $"{Mathf.RoundToInt(curr)}/{_manaStatUI.MaxMana}";
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
        ActionManager.Instance.OnPlayerManaUpdated -= OnPlayerSpellChargeUpdated;
    }
}
