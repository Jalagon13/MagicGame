using UnityEngine;
using UnityEngine.UI;

public class JoinCodeButtonUI : MonoBehaviour
{
	[SerializeField] private Button _copyJoinCodeButton;
	
	private void Start()
	{
		_copyJoinCodeButton.onClick.AddListener(() =>
		{
			GUIUtility.systemCopyBuffer = Relay.JOIN_CODE.ToString();
		});
	}
}
