using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTinker
{
    // This script manages a group of tabs and their associated content.
    public class TabGroup : MonoBehaviour
    {
        [SerializeField] private AudioClip _tabClickSound; // Sound played when a tab is clicked.
        [SerializeField] private Color _tabIdleColor; // Color of the tab when it's not selected or hovered over.
        [SerializeField] private Color _tabHoverColor; // Color of the tab when the mouse hovers over it.
        [SerializeField] private Color _tabActiveColor; // Color of the tab when it's selected.
        [SerializeField] private List<GameObject> _objectsToSwap; // List of game objects to swap visibility based on the selected tab.

        private List<Tab> _tabButtons; // List of tab buttons associated with this group.
        private Tab _selectedTab; // Reference to the currently selected tab.

        private void Start()
        {
            // Select first tab on start of the game (might delete later)
            OnTabSelected(_tabButtons[1]);
        }

        // Subscribe a tab button to this tab group.
        public void Subscribe(Tab button)
        {
            if (_tabButtons == null)
            {
                _tabButtons = new List<Tab>();
            }
            _tabButtons.Add(button);
        }

        // Called when the mouse hovers over a tab button.
        public void OnTabEnter(Tab button)
        {
            ResetTabs();
            if (_selectedTab == null || button != _selectedTab)
            {
                button.Background.color = _tabHoverColor;
            }
        }

        // Called when the mouse exits a tab button.
        public void OnTabExit(Tab button)
        {
            ResetTabs();
        }

        // Called when a tab button is selected.
        public void OnTabSelected(Tab button)
        {
            if (_selectedTab != null)
            {
                _selectedTab.Deselect();
            }

            _selectedTab = button;
            _selectedTab.Select();

            MMSoundManagerSoundPlayEvent.Trigger(_tabClickSound, MMSoundManager.MMSoundManagerTracks.Sfx, default, pitch: Random.Range(0.95f, 1.1f), volume: 0.7f);

            ResetTabs();
            button.Background.color = _tabActiveColor;
            int index = button.transform.GetSiblingIndex();
            for (int i = 0; i < _objectsToSwap.Count; i++)
            {
                _objectsToSwap[i].SetActive(i == index);
            }
        }

        // Reset the colors of all tab buttons to the idle color.
        public void ResetTabs()
        {
            foreach (Tab tab in _tabButtons)
            {
                if (_selectedTab != null && tab == _selectedTab) continue;
                tab.Background.color = _tabIdleColor;
            }
        }
    }
}