using System;
using System.Collections.Generic;
using AdvancedTooltips.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellDisplaySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [field: SerializeField] public Image CooldownUI { get; private set; }
    [field: SerializeField] public TextMeshProUGUI ControlText { get; private set; }

    private SpellItemSO _spell;
    private int _spellId;
    private Image _spellIcon;
    private Image _background;
    private bool _hovered;

    private Dictionary<int, string> _controlTexts = new Dictionary<int, string>
    {
        { 0, $"Left<br>Click" },
        { 1, $"Right<br>Click" },
        { 2, "Shift" },
        { 3, "Space" }
    };
    
    private void Awake()
    {
        _spellIcon = transform.GetChild(0).GetComponent<Image>();
        _background = GetComponent<Image>();
    }

    private void OnDisable()
    {
        if (_hovered)
        {
            Tooltip.HideUI();
        }
    }

    private void Start()
    {
        CooldownUI.enabled = false;
        Player.LocalClientInstance.SpellCaster.OnSpellCooldownTimersUpdated += UpdateCooldownDisplay;
    }


    private void OnDestroy()
    {
        Player.LocalClientInstance.SpellCaster.OnSpellCooldownTimersUpdated -= UpdateCooldownDisplay;
    }

    private void UpdateCooldownDisplay(object sender, EventArgs e)
    {
        // if(Player.LocalClientInstance.SpellCaster.SpellCoolDownTimers.ContainsKey(_spellId))
        // {
        //     Timer spellCdTimer = Player.LocalClientInstance.SpellCaster.SpellCoolDownTimers[_spellId];
        //     CooldownUI.enabled = true;
        //     CooldownUI.fillAmount = spellCdTimer.RemainingSeconds / spellCdTimer.Duration;
        // }
        // else
        // {
        //     CooldownUI.enabled = false;
        // }
    }

    public void SetSpell(SpellItemSO spell, int controlIndex)
    {
        _spell = spell;
        _spellIcon.sprite = _spell != null ? _spell.SpellUIDisplaySprite : null;
        _spellIcon.enabled = _spell != null;
        _spellId = GameManager.Instance.GetItemIdFromItemSO(_spell);
        ControlText.text = _controlTexts.ContainsKey(controlIndex) ? _controlTexts[controlIndex] : "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;

        Tooltip.ShowNew();
        Tooltip.SpellDisplay(_spell, fontSize: 12f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Tooltip.HideUI();
    }
}
