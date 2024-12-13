using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerArmSprite : NetworkBehaviour
{
    [SerializeField] private CastArmController _castArmController;
    [SerializeField] private SwingController _swingController;
	
    private SpriteRenderer _sr;
    private Player _thisPlayer;
	
    private void Awake()
    {
        _thisPlayer = transform.root.GetComponent<Player>();
        _sr = GetComponent<SpriteRenderer>();
		
        _castArmController.OnHoldingWandStart += CastArm_OnHoldingWandStart;
        _castArmController.OnHoldingWandEnd += CastArm_OnHoldingWandEnd;
        _swingController.OnSwingStart += Swing_OnStart;
        _swingController.OnSwingEnd += Swing_OnEnd;
    }

    public override void OnNetworkSpawn()
    {
        if(_thisPlayer.IsHoldingWand())
        {
            Hide();
        }
        else
        {
            Show();
        }
	
        base.OnNetworkSpawn();
    }

    private void Swing_OnStart(object sender, SwingController.SwingEventArgs e)
    {
        Hide();
    }

    private void Swing_OnEnd(object sender, SwingController.SwingEventArgs e)
    {
        Show();
    }

    private void CastArm_OnHoldingWandStart(object sender, EventArgs e)
    {
        Hide();
    }

    private void CastArm_OnHoldingWandEnd(object sender, CastArmController.OnHoldingWandEndEventArgs e)
    {
        Show();
    }
	
    public void Show()
    {
        _sr.enabled = true;
    }
	
    public void Hide()
    {
        _sr.enabled = false;
    }
	
    public override void OnDestroy()
    {
        _castArmController.OnHoldingWandStart -= CastArm_OnHoldingWandStart;
        _castArmController.OnHoldingWandEnd -= CastArm_OnHoldingWandEnd;
        _swingController.OnSwingStart -= Swing_OnStart;
        _swingController.OnSwingEnd -= Swing_OnEnd;
        base.OnDestroy();
    }
}
