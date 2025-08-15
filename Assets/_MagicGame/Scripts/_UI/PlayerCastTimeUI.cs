using MoreMountains.Tools;
using TMPro;
using UnityEngine;

public class PlayerCastTimeUI : MonoBehaviour
{
    [SerializeField] private MMProgressBar _castTimeBar;
    [SerializeField] private RectTransform _border; // Set width to max mana dynamically
    [SerializeField] private TextMeshProUGUI _amountText;
    
    private void Update()
    {
        if(Player.Instance == null) return;
        
        if(Player.Instance.SpellCaster.CastTimer.IsRunning && Player.Instance.SpellCaster.IsCasting.Value)
        {
            Show();
            UpdateView(Player.Instance.SpellCaster.CastTimer.Duration - Player.Instance.SpellCaster.CastTimer.RemainingSeconds, Player.Instance.SpellCaster.CastTimer.Duration);
        }
        else
        {
            Hide();
        }
    }

    private void UpdateView(float currentAmount, float maxAmount)
    {
        if (!_castTimeBar || !_amountText) return;

        _castTimeBar.UpdateBar(currentAmount, 0, maxAmount);
        // _amountText.text = $"{currentAmount}/{maxAmount}";
    }
    
    private void Show()
    {
        transform.GetChild(0).gameObject.SetActive(true);
    }
    
    private void Hide()
    {
        transform.GetChild(0).gameObject.SetActive(false);
    }
}
