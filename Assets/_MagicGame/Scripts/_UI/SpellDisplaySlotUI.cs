using System;
using UnityEngine;
using UnityEngine.UI;

public class SpellDisplaySlotUI : MonoBehaviour
{
    [field: SerializeField] public Color SelectedColor { get; private set; }
    [field: SerializeField] public Color DeselectedColor { get; private set; }
    [field: SerializeField] public Image CooldownUI { get; private set; }

    private SpellItemSO _spell;
    private int _spellId;
    private Image _spellIcon;
    private Image _background;
    
    private void Awake()
    {
        _spellIcon = transform.GetChild(0).GetComponent<Image>();
        _background = GetComponent<Image>();
    }
    
    private void Start()
    {
        CooldownUI.enabled = false;
        SpellManager.Instance.OnSpellCooldownTimersUpdated += UpdateCooldownDisplay;
    }

    private void UpdateCooldownDisplay(object sender, EventArgs e)
    {
        if(SpellManager.Instance.SpellCooldownTimers.ContainsKey(_spellId))
        {
            Timer spellCdTimer = SpellManager.Instance.SpellCooldownTimers[_spellId];
            CooldownUI.enabled = true;
            CooldownUI.fillAmount = spellCdTimer.RemainingSeconds / spellCdTimer.Duration;
        }
        else
        {
            CooldownUI.enabled = false;
        }
    }

    public void SetSpell(SpellItemSO spell)
    {
        _spell = spell;
        _spellIcon.sprite = _spell.SpellPortrait;
        _spellId = GameManager.Instance.GetItemIdFromItemSO(_spell);
    }
    
    public void SelectSlot()
    {
        _background.color = SelectedColor;
        Color c = _background.color;
        c.a = 1f;
        _background.color = c;
    }
    
    public void DeselectSlot()
    {
        _background.color = DeselectedColor;
        Color c = _background.color;
        c.a = 0.75f;
        _background.color = c;
    }
    
    private void OnDestroy()
    {
        SpellManager.Instance.OnSpellCooldownTimersUpdated -= UpdateCooldownDisplay;
    }
}
