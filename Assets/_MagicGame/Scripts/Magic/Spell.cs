using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Spell : NetworkBehaviour
{
	public event EventHandler OnSpellEnd;
	[HideInInspector] public Vector2 Velocity;
	[HideInInspector] public NetworkVariable<SyncSpellData> SpellData = new NetworkVariable<SyncSpellData>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	[HideInInspector] public NetworkVariable<bool> ShowVisuals = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	[HideInInspector] public NetworkVariable<bool> Started = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public int CollisionMask { get; private set; }
	public int NpcLayer { get; private set; }
	public int WallMask { get; private set; }
	public List<Npc> HitTargets { get; private set; } = new();
	public Timer SpellLifeTimer { get; private set; }

	protected GameObject _spellGameObject;
	protected CircleCollider2D _spellCollider;
	protected Vector2 _finalDirection;
	protected Vector2 _lastPosition;
	protected Transform _spellModifierTf;
	protected bool _isDead;
	protected int _bounces;
	protected bool _passingThroughWall;
	protected float _totalPassThroughDistance;

	private SyncSpellData _spellData;
	private Transform _visualizationTf;

	protected virtual void Awake()
    {
		_spellGameObject = transform.GetChild(0).gameObject;
		_spellCollider = GetComponent<CircleCollider2D>();
		_spellModifierTf = transform.GetChild(0).GetChild(0);
		_visualizationTf = transform.GetChild(0).GetChild(1);
		
		Hide();
	}

	public override void OnNetworkSpawn()
	{
		ShowVisuals.OnValueChanged += HandleVisuals;
		
		if (IsOwner)
		{
			SpellData.Value = _spellData;
			CollisionMask = LayerMask.GetMask(new[] { "PathfindingWall", "Npc" });
			WallMask = LayerMask.NameToLayer("PathfindingWall");
			NpcLayer = LayerMask.NameToLayer("Npc");
			ShowVisuals.Value = false;
		}

		foreach (int modifierIndex in SpellData.Value.ModifierArray)
		{
			SpellModItemSO modifier = GameManager.Instance.GetItemSOFromItemId(modifierIndex) as SpellModItemSO;
			var go = Instantiate(modifier.SpellModifierPrefab, _spellModifierTf);

			if (IsOwner)
			{
				SpellData.Value = go.GetComponent<ISpellModifier>().ModifiySpellData(SpellData.Value, this);
			}
		}
	}

	protected virtual void FixedUpdate()
	{
		if (!Started.Value || !IsOwner) return; //don't do anything before OnNetworkSpawn has run.

		SpellLifeTimer.Tick(Time.fixedDeltaTime);

		if (!_isDead)
		{
			PassThroughWall();
			DetectCollisions();
		}
	}

	public virtual void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
	{
		if(IsOwner)
		{
			Started.Value = true;
			ShowVisuals.Value = true;
			transform.position = spawnPoint;
			_lastPosition = spawnPoint;
			_finalDirection = finalDirection;
			_isDead = false;
			
			SpellLifeTimer = new Timer(SpellData.Value.Lifetime);
			SpellLifeTimer.OnTimerEnd += DestroySpell;
		}
	}

    public void CancelSpell()
	{
		OnSpellEnd?.Invoke(this, EventArgs.Empty);

		if (IsOwner)
		{
			NetworkObject.Despawn();
		}
		
		Destroy(gameObject);
	}
	
	public void SetSpellData(SyncSpellData spellData)
	{
		_spellData = spellData;
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
		_visualizationTf.gameObject.SetActive(true);
	}

	private void Hide()
	{
		_visualizationTf.gameObject.SetActive(false);
	}

	private void DestroySpell(object sender, EventArgs e)
    {
		Started.Value = false;
		OnSpellEnd?.Invoke(this, EventArgs.Empty);

		if (IsOwner)
		{
			NetworkObject.Despawn();
		}
		Debug.Log($"Spell Destroyed");
		Destroy(gameObject);
	}
	
	private void PassThroughWall()
	{
		float distanceThisFrame = Vector2.Distance(transform.position, _lastPosition);
		if (_passingThroughWall)
		{
			_totalPassThroughDistance += distanceThisFrame;
			if (_totalPassThroughDistance >= SpellData.Value.GhostDistance)
			{
				TerminateSpell();
				return;
			}
		}

		_passingThroughWall = false;
		_lastPosition = transform.position;
	}

	protected void TerminateSpell()
	{
		_isDead = true;
		SpellLifeTimer.Tick(Mathf.Infinity);
	}

	private void DetectCollisions()
    {
		if(_spellCollider == null) return;
    
		Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, _spellCollider.radius, CollisionMask);
		for (int i = 0; i < collisions.Length; i++)
		{
			int layerTest = 1 << collisions[i].gameObject.layer;
			if((layerTest & CollisionMask) != 0)
			{
			    if(collisions[i].gameObject.layer == WallMask)
			    {
					if (collisions[i].TryGetComponent(out PathfindingWallTm pfWall))
			        {
						if (pfWall.BiomeSameAs(SpellData.Value.SpawnBiome))
						{
							if(SpellData.Value.GhostDistance > 0)
							{
							    _passingThroughWall = true;
							}
							else
							{
								if (_bounces >= SpellData.Value.Bounces)
								{
									TerminateSpell();
									return;
								}
								else
								{
									// Bounce if not at max bounces
									RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Velocity.normalized, _spellCollider.radius, CollisionMask);
									foreach (var hit in hits)
									{
										if (hit.collider == collisions[i])
										{
											Vector2 hitNormal = hit.normal;
											float speed = Velocity.magnitude;
											Velocity = Vector2.Reflect(Velocity.normalized, hitNormal) * speed;
											_bounces++;
											break;
										}
									}
								}
							}
						}
					}
			    }
			    
			    if(collisions[i].gameObject.layer == NpcLayer)
			    {
			    	if(collisions[i].TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(SpellData.Value.SpawnBiome))
			    	{
						Npc npc = npcNet.gameObject.GetComponent<Npc>();

						// Overlapping with an NPC in the same biome
						if (!HitTargets.Contains(npc))
			    		{
							HitTargets.Add(npc);
							npc.ApplyDamage(SpellData.Value.Damage, NetworkManager.ConnectedClients[SpellData.Value.OwnerPlayerId].PlayerObject.transform.position, SpellData.Value.Knockback);
							
							if(HitTargets.Count >= SpellData.Value.Pierces)
							{
								TerminateSpell();
								return;
							}
						}
			    	}
			    }
			}
		}
	}
}