using System;
using TMPro;
using UnityEngine;

namespace ProjectTinker
{
    public class GoldUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _goldText;

        private void Start()
        {
            UpdateText(GoldManager.Instance.StartingGold);
            GoldManager.Instance.OnGoldChanged += UpdateGoldUI;
        }

        private void UpdateGoldUI(object sender, GoldManager.GoldEventArgs e)
        {
            UpdateText(e.CurrentGold);
        }

        private void UpdateText(int goldAmount)
        {
            _goldText.text = $"{goldAmount}";
        }

        private void OnDestroy()
        {
            GoldManager.Instance.OnGoldChanged -= UpdateGoldUI;
        }
    }
}
