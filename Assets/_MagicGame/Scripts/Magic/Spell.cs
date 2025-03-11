using System;
using System.Collections.Generic;
using MoreMountains.Tools;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Spell : NetworkBehaviour
{
	public NetworkVariable<SyncSpellData> SpellDataNV = new NetworkVariable<SyncSpellData>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	[HideInInspector]
	public Vector2 Velocity;
	public bool Started { get; private set; }
	public int CollisionMask { get; private set; }
	public int NpcLayer { get; private set; }
	public int WallMask { get; private set; }
	public List<GameObject> HitTargets { get; private set; } = new();

	protected GameObject _spellGameObject;
	protected CircleCollider2D _spellCollider;
	protected bool _isDead;
	protected Vector2 _finalDirection;
	protected MiningSpellMod _miningSpellMod;
	
	private SyncSpellData _spellData;
	private Transform _spellModifierTf;
	private Timer _spellLifeTimer;
	private int _bounces;
	private float _totalPassThroughDistance;
	private bool _passingThroughWall;
	private Vector2 _lastPosition;

	protected virtual void Awake()
    {
		_spellGameObject = transform.GetChild(0).gameObject;
		_spellCollider = GetComponent<CircleCollider2D>();
		_spellModifierTf = transform.GetChild(0).GetChild(0);
	}
	
	public virtual void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
	{
		if(IsServer)
		{
			Started = true;
			transform.position = spawnPoint;
			_lastPosition = spawnPoint;
			_finalDirection = finalDirection;
			_isDead = false;

			_spellLifeTimer = new Timer(_spellData.Lifetime);
			_spellLifeTimer.OnTimerEnd += DestroySpell;
		}
	}
	
	public void CancelSpell()
	{
		if (IsServer)
		{
			NetworkObject.Despawn();
		}
		
		Destroy(gameObject);
	}
	
	public void SetSpellData(SyncSpellData spellData)
	{
		_spellData = spellData;
		GetComponent<SpellNetworkComponent>().InitializeSpellNetwork(spellData);
	}
	
	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			SpellDataNV.Value = _spellData;
			CollisionMask = LayerMask.GetMask(new[] { "PathfindingWall", "Npc" });
			WallMask = LayerMask.NameToLayer("PathfindingWall");
			NpcLayer = LayerMask.NameToLayer("Npc");
		}

		foreach (int modifierIndex in SpellDataNV.Value.ModifierArray)
		{
			SpellModItemSO modifier = GameManager.Instance.GetItemSOFromItemId(modifierIndex) as SpellModItemSO;
			var go = Instantiate(modifier.SpellModifierPrefab, _spellModifierTf);
			
			if(IsServer)
			{
				SpellDataNV.Value = go.GetComponent<ISpellModifier>().ModifiySpellData(SpellDataNV.Value, this);
				
				if(modifier is MiningFocusItemSO)
				{
				    _miningSpellMod = go.GetComponent<MiningSpellMod>();
				    Debug.Log($"Found mining spell modifier");
				}
			}
		}
	}
	
    private void DestroySpell(object sender, EventArgs e)
    {
		Started = false;

		if (IsServer)
		{
			NetworkObject.Despawn();
		}
		Debug.Log($"Spell Destroyed");
		Destroy(gameObject);
	}

	protected virtual void FixedUpdate()
	{
		if(!Started || !IsServer) return; //don't do anything before OnNetworkSpawn has run.

		_spellLifeTimer.Tick(Time.fixedDeltaTime);

		if (!_isDead)
		{
			PassThroughWall();
			DetectCollisions();
		}
	}
	
	private void PassThroughWall()
	{
		float distanceThisFrame = Vector2.Distance(transform.position, _lastPosition);
		if (_passingThroughWall)
		{
			_totalPassThroughDistance += distanceThisFrame;
			if (_totalPassThroughDistance >= SpellDataNV.Value.GhostDistance)
			{
				TerminateSpell();
				return;
			}
		}

		_passingThroughWall = false;
		_lastPosition = transform.position;
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
						if (pfWall.BiomeSameAs(SpellDataNV.Value.SpawnBiome))
						{
							if(SpellDataNV.Value.GhostDistance > 0)
							{
							    _passingThroughWall = true;
							}
							else
							{
								if (_bounces >= SpellDataNV.Value.Bounces)
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
			    	if(collisions[i].TryGetComponent(out NpcNetworkComponent npcNet) && npcNet.SameBiomeAs(SpellDataNV.Value.SpawnBiome))
			    	{
						// Overlapping with an NPC in the same biome
						if (!HitTargets.Contains(collisions[i].gameObject))
			    		{
							HitTargets.Add(collisions[i].gameObject);
							
							Npc npc = npcNet.gameObject.GetComponent<Npc>();
							npc.ApplyDamage(SpellDataNV.Value.Damage, NetworkManager.ConnectedClients[SpellDataNV.Value.SpawnPlayerId].PlayerObject.transform.position, SpellDataNV.Value.Knockback);
							
							if(HitTargets.Count >= SpellDataNV.Value.Pierces)
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
	
	protected void TerminateSpell()
	{
		_isDead = true;
		_spellLifeTimer.Tick(Mathf.Infinity);
	}
}