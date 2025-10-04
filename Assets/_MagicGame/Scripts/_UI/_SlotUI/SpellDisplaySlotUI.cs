using System;
using System.Collections.Generic;
using AdvancedTooltips.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectWizard
{
    public class SpellDisplaySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private Image _cooldownUI;

        [SerializeField]
        private Color _selectedColor = Color.yellow;

        private Color _defaultColor;
        private SpellItemSO _spell;
        private Image _spellIcon;
        private Image _background;
        private bool _hovered;
        private int _spellIndex;

        private void Awake()
        {
            _spellIcon = transform.GetChild(0).GetComponent<Image>();
            _background = GetComponent<Image>();
            _defaultColor = _background.color;

            Player.Instance.SpellCastController.SpellCooldownSystem.OnSpellCooldownTimersUpdated += UpdateCooldownDisplay;
            Player.Instance.SpellCastController.OnSelectedSpellUpdated += OnSelectedSpellUpdated;
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
            _cooldownUI.enabled = false;
        }


        private void OnDestroy()
        {
            Player.Instance.SpellCastController.SpellCooldownSystem.OnSpellCooldownTimersUpdated -= UpdateCooldownDisplay;
            Player.Instance.SpellCastController.OnSelectedSpellUpdated -= OnSelectedSpellUpdated;
        }

        private void OnSelectedSpellUpdated(object sender, SelectedSpellChangedEventArgs e)
        {
            if (e.SelectedSpellIndex == _spellIndex)
            {
                _background.color = _selectedColor;
            }
            else
            {
                _background.color = _defaultColor;
            }
        }

        private void UpdateCooldownDisplay(object sender, EventArgs e)
        {
            if (Player.Instance.SpellCastController.SpellCooldownSystem.SpellCoolDownTimers.ContainsKey(_spell))
            {
                Timer spellCdTimer = Player.Instance.SpellCastController.SpellCooldownSystem.SpellCoolDownTimers[_spell];
                _cooldownUI.enabled = true;
                _cooldownUI.fillAmount = spellCdTimer.RemainingSeconds / spellCdTimer.Duration;
            }
            else
            {
                _cooldownUI.enabled = false;
            }
        }

        public void SetSpell(SpellItemSO spell, int spellIndex)
        {
            _spell = spell;
            _spellIcon.sprite = _spell != null ? _spell.UiDisplay : null;
            _spellIcon.enabled = _spell != null;
            _spellIndex = spellIndex;
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
}
