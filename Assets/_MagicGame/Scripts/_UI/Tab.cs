using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using AdvancedTooltips.Core;

namespace ProjectTinker
{
    [RequireComponent(typeof(Image))]
    public class Tab : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string _tabDisplayName;
        [SerializeField] private TabGroup _tabGroup;
        [SerializeField] private UnityEvent _onTabSelected;
        [SerializeField] private UnityEvent _onTabDeselected;

        private Image _background;
        private bool _hovered;

        public Image Background => _background;

        private void Awake()
        {
            _background = GetComponent<Image>();
            _tabGroup.Subscribe(this);
        }

        private void OnDisable()
        {
            if (_hovered)
            {
                Tooltip.HideUI();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _tabGroup.OnTabSelected(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _tabGroup.OnTabEnter(this);
            Tooltip.ShowNew();
            Tooltip.JustText(_tabDisplayName, Color.white, fontSize: 12f);
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _tabGroup.OnTabExit(this);
            Tooltip.HideUI();
            _hovered = false;
        }

        public void Select()
        {
            _onTabSelected?.Invoke();
        }

        public void Deselect()
        {
            _onTabDeselected?.Invoke();
        }
    }
}
