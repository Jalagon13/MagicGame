using System;
using System.Collections;
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
	
	[field: SerializeField] public GameObject Visualization { get; private set; }
	[field: SerializeField] public float DespawnDelay { get; private set; }

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
	private bool _despawning;

	protected virtual void Awake()
    {
		_spellGameObject = transform.GetChild(0).gameObject;
		Visualization.SetActive(false);
	}

	public override void OnNetworkSpawn()
	{
		if(IsClient)
		{
			ShowVisuals.OnValueChanged += HandleVisuals;
			IsStarted.OnValueChanged += HandleStarted;
		}
		
		if (IsOwner)
		{
			CollisionMask = LayerMask.GetMask(new[] { "LocalWall", "Npc" });
			WallMask = LayerMask.NameToLayer("LocalWall");
			NpcLayer = LayerMask.NameToLayer("Npc");
			
			Visualization.SetActive(false);	

			SpellManager.Instance.OnExecuteSpells += ExecuteSpellStart;
			SpellManager.Instance.OnCancelSpells += CancelSpellCharge;
			HotbarManager.Instance.OnFocusSlotUpdated += TryToDespawnIfSlotChanged;
			
			OnOwnerSpellSpawned();
		}
	}

    private void HandleStarted(bool previousValue, bool newValue)
    {
		if(!newValue)
		{
			OnStopped();
		}
	}
    
    protected virtual void OnStopped() { }

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
		if(IsStarted.Value || _despawning) return;

		SpellManager.Instance.OnCancelSpells -= CancelSpellCharge;

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
		if(IsOwner && SpellLifeTimer != null && SpellLifeTimer.RemainingSeconds > 0)
		{
			SpellLifeTimer.Tick(Time.deltaTime);
		}
	}

	protected virtual void OnOwnerSpellSpawned() { }
	protected virtual void OnOwnerExecuteSpellStart() { }

	public virtual void OnOwnerSpellCanceled()
	{
		IsStarted.Value = false;
		DespawnSpellServerRpc();
	}
	
	public virtual void OnOwnerSpellEnd()
	{
		IsStarted.Value = false;
		StartCoroutine(WaitToDespawnRoutine());
	}

	private IEnumerator WaitToDespawnRoutine()
	{
		_despawning = true;
		
		if (DespawnDelay > 0)
		{
			yield return new WaitForSeconds(DespawnDelay);
		}
	
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
		Visualization.SetActive(newValue);
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
			Visualization.transform.parent = transform;
		}
	}
}