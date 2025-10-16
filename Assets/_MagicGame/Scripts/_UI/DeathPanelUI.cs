using System;
using System.Collections;
using UnityEngine;

namespace ProjectTinker
{
    public class DeathPanelUI : MonoBehaviour
    {
        [SerializeField] private float _delayBeforeShowingUI = 1.5f;

        private GameObject _uiElements;

        private void Awake()
        {
            _uiElements = transform.GetChild(0).gameObject;
            Player.OnAnyPlayerSpawned += RegisterDeathPanelLogic;
            Hide();
        }

        private void OnDestroy()
        {
            Player.OnAnyPlayerSpawned -= RegisterDeathPanelLogic;
            if (Player.Instance != null)
            {
                Player.Instance.ServerCharacter.NetLifeState.LifeState.OnValueChanged -= OnPlayerLifeStateChanged;
            }
        }

        private void RegisterDeathPanelLogic(object sender, Player.PlayerIdEventArgs e)
        {
            if (Player.Instance != null)
            {
                Player.Instance.ServerCharacter.NetLifeState.LifeState.OnValueChanged += OnPlayerLifeStateChanged;
            }
        }

        private void OnPlayerLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            if (previousValue == LifeState.Alive && newValue == LifeState.Dead)
            {
                StartCoroutine(DeathPanelUIRoutine());
            }
        }

        private IEnumerator DeathPanelUIRoutine()
        {
            yield return new WaitForSeconds(_delayBeforeShowingUI);
            Show();
        }

        public void OnRespawnButtonPressed()
        {
            Hide();
            Player.Instance.Respawn();
        }

        private void Show()
        {
            _uiElements.SetActive(true);
        }

        private void Hide()
        {
            _uiElements.SetActive(false);
        }
    }
}
