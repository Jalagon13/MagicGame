using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
	[SerializeField] private Button _hostButton;
	[SerializeField] private Button _joinButton;
	[SerializeField] private Button _quitButton;
	[SerializeField] private TMP_InputField _joinInput;
	[SerializeField] private Relay _relay;
	
	private void Awake()
	{
		_hostButton.onClick.AddListener(() => 
		{
			_relay.CreateRelay();
		});
		
		_joinButton.onClick.AddListener(() => 
		{
			_relay.JoinRelay(_joinInput.text);
		});
		
		_quitButton.onClick.AddListener(() => 
		{
			Application.Quit();
		});
		
		Time.timeScale = 1f;
	}
}
