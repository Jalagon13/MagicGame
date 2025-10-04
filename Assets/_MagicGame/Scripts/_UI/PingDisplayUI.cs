using Unity.Netcode;
using UnityEngine;
using TMPro; // If you're using TextMeshPro for UI

namespace ProjectWizard
{
    public class PingDisplay : MonoBehaviour
    {
        public TextMeshProUGUI _pingText; // Assign this in the Inspector

        void Update()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && Player.Instance != null)
            {
                _pingText.text = $"{Mathf.RoundToInt(NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId))} ms";
            }
            else
            {
                _pingText.text = "Not Connected";
            }
        }
    }
}