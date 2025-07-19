using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Shop : NetworkBehaviour
{
    [field: SerializeField] public float PlayerDetectionRange { get; private set; } = 10f;
    [field: SerializeField] public WorldInput WorldInput { get; private set; }
    [field: SerializeField] public List<ItemSO> ItemsToSell { get; private set; }
    
    [HideInInspector] public NetworkList<ulong> PlayersUsingShop { get; private set; }= new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private BasicNpcStateMachine _chaseAI;

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            _chaseAI = GetComponent<BasicNpcStateMachine>();
            PlayersUsingShop.OnListChanged += HandleMovement;
        }
    }

    private void HandleMovement(NetworkListEvent<ulong> changeEvent)
    {
        Debug.Log($"Players using shop: {PlayersUsingShop.Count}");
        if(PlayersUsingShop.Count > 0)
        {
            Debug.Log($"AI not moving");
            // _chaseAI.CanMove = false;
        }
        else
        {
            Debug.Log($"AI can move again");
            // _chaseAI.CanMove = true;
        }
    }

    private void Start()
    {
        GameInput.Instance.OnSecondaryActionStarted += GameInput_OnSecondaryActionStarted;
    }

    private void Update()
    {
        if (!IsServer || _chaseAI == null || PlayersUsingShop.Count > 0) return;

        Player closestPlayer = MultiplayerManager.Instance.GetClosestPlayer(transform.position, GetComponent<NpcNetworkVisibility>().NpcBiomeType);
        if (closestPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, closestPlayer.transform.position);
            // _chaseAI.CanMove = distanceToPlayer <= PlayerDetectionRange;
        }
        else
        {
            // _chaseAI.CanMove = false;
        }
    }

    private void GameInput_OnSecondaryActionStarted(object sender, EventArgs e)
    {
        var centerPosition = new Vector2(transform.position.x, transform.position.y + 0.5f);
        var playerInRange = Vector2.Distance(Player.Instance.transform.position, centerPosition) <= WorldObject.InteractDistance;

        if (WorldInput.IsMouseOverIndputDetector() && playerInRange && !PlayersUsingShop.Contains(NetworkManager.LocalClientId))
        {
            Debug.Log("Opening Shop");
            AddPlayerToThoseUsingShopServerRpc(NetworkManager.LocalClientId);
            InGameMenu.Instance.OpenNpcMenu(ItemsToSell, gameObject);
            InGameMenu.Instance.OnMenuClose += UnRegisterPlayer;
        }
    }

    private void UnRegisterPlayer(object sender, EventArgs e)
    {
        InGameMenu.Instance.OnMenuClose -= UnRegisterPlayer;
       
        RemovePlayerFromThoseUsingShopServerRpc(NetworkManager.LocalClientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RemovePlayerFromThoseUsingShopServerRpc(ulong playerId)
    {
        Debug.Log($"Unregistering player {playerId}");
        PlayersUsingShop.Remove(playerId);
        foreach (var item in PlayersUsingShop)
        {
            Debug.Log(item);
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void AddPlayerToThoseUsingShopServerRpc(ulong playerId)
    {
        Debug.Log($"Registering player {playerId}");
        
        PlayersUsingShop.Add(playerId);
        foreach (var item in PlayersUsingShop)
        {
            Debug.Log(item);
        }
    }

    public override void OnDestroy()
    {
        if (IsServer)
        {
            PlayersUsingShop.OnListChanged -= HandleMovement;
        }

        GameInput.Instance.OnSecondaryActionStarted -= GameInput_OnSecondaryActionStarted;
        base.OnDestroy();
    }
}
