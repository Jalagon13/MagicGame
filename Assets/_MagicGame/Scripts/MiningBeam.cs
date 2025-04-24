using Unity.Netcode;
using UnityEngine;

public class MiningBeam : NetworkBehaviour
{
    private NetworkVariable<bool> _miningBeamVisible = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<Vector2> _miningBeamStartPosition = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<Vector2> _miningBeamEndPosition = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private LineRenderer _miningBeamRenderer;
    private GameObject _startVfx;

    private void Awake()
    {
        _miningBeamRenderer = GetComponent<LineRenderer>();
        _startVfx = transform.GetChild(0).gameObject;
    }

    private void FixedUpdate()
    {
        if(IsOwner)
        {
            _miningBeamStartPosition.Value = Player.LocalClientInstance.MainHand.SpellSpawnTransform.position;
            _miningBeamEndPosition.Value = ActionManager.MouseWorldPosition;
            _miningBeamVisible.Value = MiningHandler.Instance.IsMining;
        }
        
        if(IsClient)
        {
            _miningBeamRenderer.enabled = _miningBeamVisible.Value;
            _startVfx.SetActive(_miningBeamVisible.Value);
            
            if(_miningBeamVisible.Value)
            {
                Vector2 direction = _miningBeamEndPosition.Value - _miningBeamStartPosition.Value;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                _startVfx.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
                _startVfx.transform.position = _miningBeamStartPosition.Value;
                
                _miningBeamRenderer.positionCount = 2;
                _miningBeamRenderer.SetPosition(0, _miningBeamStartPosition.Value);
                _miningBeamRenderer.SetPosition(1, _miningBeamEndPosition.Value);
            }
        }
    }
}
