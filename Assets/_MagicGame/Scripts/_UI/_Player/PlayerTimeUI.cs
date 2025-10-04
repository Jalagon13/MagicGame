using System;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;

namespace ProjectWizard
{
    public class PlayerTimeUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _amountText;

        private void Start()
        {
            GameWorld.Instance.OnTick += UpdateTimeUI;
        }

        private void UpdateTimeUI(object sender, GameWorld.OnTickEventArgs e)
        {
            _amountText.text = $"Time:<br> {Mathf.RoundToInt(e.CurrentTime)}/{Mathf.RoundToInt(e.DayDuration)}";
        }

        private void OnDestroy()
        {
            GameWorld.Instance.OnTick -= UpdateTimeUI;
        }
    }
}
