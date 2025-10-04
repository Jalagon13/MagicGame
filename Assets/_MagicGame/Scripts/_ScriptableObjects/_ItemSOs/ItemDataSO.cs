using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ProjectWizard
{
    [Serializable]
    public abstract class ItemDataSO : ScriptableObject
    {
        [field: SerializeField] public string InGameName { get; private set; }
        [field: SerializeField] public string StringID { get; private set; }
        [field: SerializeField] public Sprite UiDisplay { get; private set; }
        [field: SerializeField] public int GoldValue { get; private set; }
        [field: SerializeField] public bool Stackable { get; private set; } = true;
        [field: TextArea]
        [field: SerializeField] public string Description { get; private set; }

        protected float _baseActionCooldown = 0.25f;

        public abstract InventoryItem CreateInventoryItem(int quantity);
        public abstract float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand);
        public abstract string GetDescription();

        // Returns description with line breaks
        protected string GetDescriptionBreak()
        {
            string description = "";
            if (!string.IsNullOrWhiteSpace(Description))
                description += $"{Description}<br>";

            return description;
        }
    }
}