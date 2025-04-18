using System;
using System.Collections.Generic;
using FMODUnity;
using MoreMountains.Tools;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Spell : NetworkBehaviour
{
	public static bool IsContinuouslyCasting;

	public NetworkVariable<Vector2> Velocity { get; set; } = new NetworkVariable<Vector2>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<SyncSpellData> SpellData { get; set; } = new NetworkVariable<SyncSpellData>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public NetworkVariable<bool> ShowVisuals { get; set; } = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<bool> IsStarted { get; set; } = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public int CollisionMask { get; private set; }
	public int NpcLayer { get; private set; }
	public int WallMask { get; private set; }
	public List<NetworkHealthState> HitTargets { get; private set; } = new();
	public Timer SpellLifeTimer { get; private set; }
	
	protected GameObject _spellGameObject;
	protected Vector2 _finalDirection;

	private Transform _visualizationTf;
	private PositionLerper _positionLerper;
	const float k_LerpTime = 0.05f;

	protected virtual void Awake()
    {
		_spellGameObject = transform.GetChild(0).gameObject;
		_visualizationTf = transform.GetChild(0).GetChild(0);
		
		Hide();
	}

	public override void OnNetworkSpawn()
	{
		ShowVisuals.OnValueChanged += HandleVisuals;
		
		if (IsOwner)
		{
			ShowVisuals.Value = false;
			
			CollisionMask = LayerMask.GetMask(new[] { "PathfindingWall", "Npc" });
			WallMask = LayerMask.NameToLayer("PathfindingWall");
			NpcLayer = LayerMask.NameToLayer("Npc");

			SpellManager.Instance.OnExecuteSpells += ExecuteSpellStart;
			SpellManager.Instance.OnCancelSpells += CancelSpellCharge;
			HotbarManager.Instance.OnFocusSlotUpdated += TryToDespawnIfSlotChanged;
			
			OnOwnerSpellSpawned();
		}
	}

    private void TryToDespawnIfSlotChanged(object sender, HotbarManager.OnFocusItemSetEventArgs e)
    {
        if(SpellData.Value.DespawnIfFocusSlotChanged)
        {
			InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
			
			if(selectedInventoryItem.Id != SpellData.Value.InventorySlotId)
			{
				OnOwnerSpellEnd();
			}
		}
    }

    private void ExecuteSpellStart(object sender, SpellManager.ExecuteSpellsEventArgs e)
	{
		transform.position = e.SpawnPoint;
		_finalDirection = e.Direction;

		IsStarted.Value = true;
		ShowVisuals.Value = true;

		SpellLifeTimer = new Timer(SpellData.Value.Lifetime);
		SpellLifeTimer.OnTimerEnd += OnSpellLifeTimerEnd;

		OnOwnerExecuteSpellStart();
	}

	private void CancelSpellCharge(object sender, EventArgs e)
    {
		OnOwnerSpellCanceled();
	}

    protected virtual void Update()
	{
		if(IsStarted.Value && IsOwner)
		{
			SpellLifeTimer.Tick(Time.deltaTime);
		}
	
		if (IsClient && _visualizationTf.gameObject.activeSelf)
		{
			_visualizationTf.position = _positionLerper.LerpPosition(_visualizationTf.position, transform.position);
		}
		// don't do anything before OnNetworkSpawn has run.
	}

	protected virtual void OnOwnerSpellSpawned() { }
	protected virtual void OnOwnerExecuteSpellStart() { }
	public virtual void OnOwnerSpellEnd()
	{
		IsStarted.Value = false;
		DespawnSpellServerRpc();
	}

	public virtual void OnOwnerSpellCanceled()
	{
		IsStarted.Value = false;
		DespawnSpellServerRpc();
	}

	private void OnSpellLifeTimerEnd(object sender, EventArgs e)
	{
		SpellLifeTimer.OnTimerEnd -= OnSpellLifeTimerEnd;
		OnOwnerSpellEnd();
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void DespawnSpellServerRpc()
	{
		NetworkObject.DontDestroyWithOwner = true;
		NetworkObject.Despawn();
	}

    private void HandleVisuals(bool previousValue, bool newValue)
    {
        if(newValue)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

	private void Show()
	{
		if(Player.LocalClientInstance.OwnerClientId == SpellData.Value.OwnerPlayerId)
		{
			_visualizationTf.position = Player.LocalClientInstance.MainHand.SpellSpawnTransform.position;
		}
		else
		{
			_visualizationTf.position = NetworkManager.Singleton.ConnectedClients[SpellData.Value.OwnerPlayerId].PlayerObject.GetComponent<Player>().MainHand.SpellSpawnTransform.position;
		}

		_positionLerper = new PositionLerper(transform.position, k_LerpTime);

		_visualizationTf.parent = null;
		_visualizationTf.gameObject.SetActive(true);
	}

	private void Hide()
	{
		_visualizationTf.gameObject.SetActive(false);
	}
	
	public override void OnNetworkDespawn()
	{
		if(IsOwner)
		{
			SpellManager.Instance.OnExecuteSpells -= ExecuteSpellStart;
			SpellManager.Instance.OnCancelSpells -= CancelSpellCharge;
			HotbarManager.Instance.OnFocusSlotUpdated -= TryToDespawnIfSlotChanged;
		}
	
		if (IsClient)
		{
			Debug.Log($"Reattaching visualization before despawning");
			_visualizationTf.parent = transform;
		}
	}
}